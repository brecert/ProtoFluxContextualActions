using System;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.ProtoFlux;

using ProtoFluxContextualActions.Attributes;
using HarmonyLib;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using ProtoFlux.Core;
using System.Linq;
using FrooxEngine.Undo;
using ProtoFlux.Runtimes.Execution;
using ProtoFluxContextualActions.Extensions;
using ProtoFluxContextualActions.Utils.ProtoFlux;

namespace ProtoFluxContextualActions.Patches;

[HarmonyPatch(typeof(ProtoFluxTool), nameof(ProtoFluxTool.OnSecondaryPress))]
[HarmonyPatchCategory("ProtoFluxTool Contextual Swap Actions"), TweakCategory("Adds 'Contextual Swapping Actions' to the ProtoFlux Tool. Double pressing secondary pointing at a node with protoflux tool will be open a context menu of actions to swap the node for another node.", defaultValue: true)] // unstable, disable by default
internal static partial class ContextualSwapActionsPatch
{
  // TODO: This can be replaced in the future with flags or a combination of the three automatically.
  //       progress has already been made.
  internal enum ConnectionTransferType
  {
    /// <summary>
    /// Transfers the connections by name, connections that are not found and are not of the same type will be lost.
    /// </summary>
    ByNameLossy,
    /// <summary>
    /// Uses names too :)
    /// Transfers the connections by a manually made set of mappings. Unmapped connections will be lost.
    /// </summary>
    ByMappingsLossy,
    /// <summary>
    /// Uses names too :)
    /// Attempts to match inputs of the same type 
    /// </summary>
    ByIndexLossy
  }

  internal readonly struct MenuItem(Type node, string? name = null, ConnectionTransferType? connectionTransferType = ConnectionTransferType.ByNameLossy)
  {
    internal readonly Type node = node;

    internal readonly string? name = name;

    internal readonly ConnectionTransferType? connectionTransferType = connectionTransferType;

    internal readonly string DisplayName => name ?? NodeMetadataHelper.GetMetadata(node).Name ?? node.GetNiceTypeName();
  }

  internal record ContextualContext(Type NodeType, World World);

  // additional data we store for the protoflux tool
  internal class ProtoFluxToolData
  {
    internal DateTime? lastSecondaryPress;
    internal ProtoFluxNode? lastSecondaryPressNode;
    internal Type? lastSpawnNodeType;

    internal double SecondsSinceLastSecondaryPress() => (DateTime.Now - lastSecondaryPress.GetValueOrDefault()).TotalSeconds;
  }

  // TODO: configurable
  const double DoublePressTime = 0.45;

  private static readonly ConditionalWeakTable<ProtoFluxTool, ProtoFluxToolData> additionalData = [];

  internal static bool Prefix(ProtoFluxTool __instance, SyncRef<ProtoFluxElementProxy> ____currentProxy)
  {
    var data = additionalData.GetOrCreateValue(__instance);
    var elementProxy = ____currentProxy.Target;

    if (elementProxy is null)
    {
      var hit = GetHit(__instance);
      if (hit is { Collider.Slot: var hitSlot })
      {
        var hitNode = hitSlot.GetComponentInParents<ProtoFluxNode>();
        if (hitNode != null)
        {
          if (data.SecondsSinceLastSecondaryPress() < DoublePressTime && data.lastSecondaryPressNode != null && !data.lastSecondaryPressNode.IsRemoved && data.lastSecondaryPressNode == hitNode)
          {
            CreateMenu(__instance, hitNode);
            data.lastSecondaryPressNode = null;
            data.lastSecondaryPressNode = null;
            data.lastSpawnNodeType = null;
            // skip rest
            return false;
          }
          else
          {
            data.lastSpawnNodeType = __instance.SpawnNodeType;
            data.lastSecondaryPressNode = hitNode;
            data.lastSecondaryPress = DateTime.Now;
            // skip null
            return true;
          }
        }
      }

      data.lastSecondaryPressNode = null;
      data.lastSecondaryPress = null;
      data.lastSpawnNodeType = null;
    }

    return true;
  }

  private static void CreateMenu(ProtoFluxTool __instance, ProtoFluxNode hitNode)
  {
    __instance.StartTask(async () =>
    {
      var items = GetMenuItems(__instance, hitNode).Where(m => m.node != hitNode.NodeType).Take(10).ToArray();

      var query = new NodeQueryAcceleration(hitNode.NodeInstance.Runtime.Group);

      if (items.Length > 0)
      {
        // restore previous spawn node
        __instance.SpawnNodeType.Value = additionalData.GetOrCreateValue(__instance).lastSpawnNodeType;

        var menu = await __instance.LocalUser.OpenContextMenu(__instance, __instance.Slot);
        // TODO: pages / custom menus

        foreach (var menuItem in items)
        {
          AddMenuItem(__instance, menu, colorX.White, menuItem, () =>
          {
            try
            {
              SwapHitForNode(__instance, hitNode, menuItem);
            }
            finally
            {
              // if there's somehow an error I do not want evil dangling references that world crash silently.
              if (hitNode != null && !hitNode.IsRemoved)
              {
                hitNode.UndoableDestroy();
              }
            }
          });
        }
      }
    });
  }

  private static void SwapHitForNode(ProtoFluxTool __instance, ProtoFluxNode hitNode, MenuItem menuItem)
  {
    var undoBatch = __instance.World.BeginUndoBatch($"Swap {hitNode.Name} to {menuItem.DisplayName}");

    var ensureVisualMethodInfo = AccessTools.Method(typeof(ProtoFluxNodeGroup), "EnsureVisualOnRestore", [typeof(Worker)]);
    var ensureVisualDelegate = AccessTools.MethodDelegate<Action<Worker>>(ensureVisualMethodInfo);

    var runtime = hitNode.NodeInstance.Runtime;
    var oldNode = hitNode.NodeInstance;
    var binding = ProtoFluxHelper.GetBindingForNode(menuItem.node);
    var query = new NodeQueryAcceleration(oldNode.Runtime.Group);
    var executionRuntime = Traverse.Create(hitNode.Group).Field<ExecutionRuntime<FrooxEngineContext>>("executionRuntime").Value;

    {
      var newNodeInstance = runtime.AddNode(menuItem.node);
      var tryByIndex = menuItem.connectionTransferType == ConnectionTransferType.ByIndexLossy;
      var results = SwapHelper.TransferElements(oldNode, newNodeInstance, query, executionRuntime, tryByIndex, overload: true);
      var nodeMap = hitNode.Group.Nodes.ToDictionary(a => a.NodeInstance, a => a);
      var swappedNodes = results.Where(r => r.overload?.OverloadedAnyNodes == true).SelectMany(r => r.overload?.SwappedNodes).Append(new(oldNode, newNodeInstance)).ToList();

      foreach (var (fromNode, intoNode) in swappedNodes)
      {
        var intoType = intoNode.GetType();
        var swappedNode = (ProtoFluxNode)nodeMap[fromNode].Slot.AttachComponent(ProtoFluxHelper.GetBindingForNode(intoType));
        nodeMap[intoNode] = swappedNode;
        AssociateInstance(swappedNode, nodeMap[fromNode].Group, intoNode);
      }

      foreach (var (_, intoNode) in swappedNodes)
      {
        intoNode.MapElements(nodeMap[intoNode], nodeMap, undoable: true);
      }

      foreach (var (fromNode, _) in swappedNodes)
      {
        var oldFromNode = nodeMap[fromNode];
        var oldVisualSlot = oldFromNode.GetVisualSlot();
        oldVisualSlot?.Destroy();
        oldVisualSlot?.Parent.GetComponent<Grabbable>()?.Destroy();
        oldFromNode.ClearGroupAndInstance();
        oldFromNode.UndoableDestroy(oldVisualSlot != null ? ensureVisualDelegate : null);
        runtime.RemoveNode(fromNode);
      }

      foreach (var (_, intoNode) in swappedNodes)
      {
        nodeMap[intoNode].EnsureVisual();
      }

      var newNode = nodeMap[newNodeInstance];
      var dynamicLists = newNode.NodeInputLists
        .Concat(newNode.NodeOutputLists)
        .Concat(newNode.NodeImpulseLists)
        .Concat(newNode.NodeOperationLists);

      foreach (var list in dynamicLists) list.EnsureElementCount(2);

      newNode.EnsureVisual();

      foreach (var (_, intoNode) in swappedNodes)
      {
        var node = nodeMap[intoNode];
        node.CreateSpawnUndoPoint(node.HasActiveVisual() ? ensureVisualDelegate : null);
      }
    }

    __instance.World.EndUndoBatch();
  }

  private static void AddMenuItem(ProtoFluxTool __instance, ContextMenu menu, colorX color, MenuItem item, Action setup)
  {
    var nodeMetadata = NodeMetadataHelper.GetMetadata(item.node);
    var label = (LocaleString)item.DisplayName;
    var menuItem = menu.AddItem(in label, (Uri?)null, color);
    menuItem.Button.LocalPressed += (button, data) =>
    {
      setup();
      __instance.LocalUser.CloseContextMenu(__instance);
    };
  }

  internal static IEnumerable<MenuItem> GetMenuItems(ProtoFluxTool __instance, ProtoFluxNode nodeComponent)
  {
    var node = nodeComponent.NodeInstance;
    var nodeType = node.GetType();
    var context = new ContextualContext(nodeType, __instance.World);

    IEnumerable<MenuItem> menuItems = [
      .. UserRootSwapGroups(nodeType),
      .. GlobalLocalEquivilentSwapGroups(nodeType),
      .. GetDirectionGroupItems(context),
      .. ForLoopGroupItems(context),
      .. EasingOfSameKindFloatItems(context),
      .. EasingOfSameKindDoubleItems(context),
      .. TimespanInstanceGroupItems(context),
      .. SetSlotTranformGlobalOperationGroupItems(context),
      .. SetSlotTranformLocalOperationGroupItems(context),
      .. UserInfoGroupItems(context),
      .. DeltaTimeGroupItems(context),
      .. UserBoolCheckGroupItems(context),
      .. PlayOneShotGroupItems(context),
      .. ScreenPointGroupItems(context),
      .. MousePositionGroupItems(context),
      .. FindSlotGroupItems(context),
      .. SlotMetaGroupItems(context),
      .. UserRootSlotGroupItems(context),
      .. UserRootPositionGroupItems(context),
      .. UserRootRotationGroupItems(context),
      .. SetUserRootPositionGroupItems(context),
      .. SetUserRootRotationGroupItems(context),
      .. UserRootHeadRotationGroupItems(context),
      .. SetUserRootHeadRotationGroupItems(context),
      .. BinaryOperationsGroupItems(context),
      .. BinaryOperationsMultiGroupItems(context),
      .. BinaryOperationsMultiSwapMapItems(context),
      .. NumericLogGroupItems(context),
      .. ApproximatelyGroupItems(context),
      .. AverageGroupItems(context),
      .. VariableStoreNodesGroupItems(context),
      .. ValueRelayGroupItems(context),
      .. ObjectRelayGroupItems(context),
      .. ComparisonBinaryOperatorGroupItems(context),
      .. DeltaTimeOperationGroupItems(context),
      .. EnumShiftGroupItems(context),
      .. NullCoalesceGroupItems(context),
      .. MinMaxGroupItems(context),
      .. MinMaxMultiGroupItems(context),
      .. ArithmeticBinaryOperatorGroupItems(context),
      .. ArithmeticMultiOperatorGroupItems(context),
      .. ArithmeticRepeatGroupItems(context),
      .. ArithmeticNegateGroupItems(context),
      .. ArithmeticOneGroupItems(context),
      .. EnumToNumberGroupItems(context),
      .. NumberToEnumGroupItems(context),
      .. MultiInputMappingGroupItems(context),
      .. ApproximatelyNodesGroupItems(context),
      .. GrabbableValuePropertyGroupItems(context),
      .. SinCosSwapGroup(context),
      .. SampleSpatialVariableGroupItems(context),
    ];

    foreach (var menuItem in menuItems)
    {
      yield return menuItem;
    }
  }

  #region Utils
  internal static string FormatMultiName(Type match) =>
    $"{NodeMetadataHelper.GetMetadata(match).Name} (Multi)";

  internal static bool TryGetSwap(BiDictionary<Type, Type> swaps, Type nodeType, out Type match) =>
    swaps.TryGetSecond(nodeType, out match) || swaps.TryGetFirst(nodeType, out match);

  [HarmonyReversePatch]
  [HarmonyPatch(typeof(ProtoFluxTool), "CleanupDraggedWire")]
  [MethodImpl(MethodImplOptions.NoInlining)]
  internal static void CleanupDraggedWire(ProtoFluxTool instance) => throw new NotImplementedException();

  [HarmonyReversePatch]
  [HarmonyPatch(typeof(ProtoFluxTool), "OnSecondaryPress")]
  [MethodImpl(MethodImplOptions.NoInlining)]
  internal static void OnSecondaryPress(ProtoFluxTool instance) => throw new NotImplementedException();


  [HarmonyReversePatch]
  [HarmonyPatch(typeof(ProtoFluxHelper), "GetNodeForType")]
  [MethodImpl(MethodImplOptions.NoInlining)]
  internal static Type GetNodeForType(Type type, List<NodeTypeRecord> list) => throw new NotImplementedException();

  [HarmonyReversePatch]
  [HarmonyPatch(typeof(Tool), "GetHit")]
  [MethodImpl(MethodImplOptions.NoInlining)]
  internal static RaycastHit? GetHit(Tool instance) => throw new NotImplementedException();

  [HarmonyReversePatch]
  [HarmonyPatch(typeof(ProtoFluxNodeGroup), "MapCastsAndOverloads")]
  [MethodImpl(MethodImplOptions.NoInlining)]
  internal static void MapCastsAndOverloads(ProtoFluxNodeGroup instance, ProtoFluxNode sourceNode, ProtoFluxNode targetNode, ConnectionResult result, bool undoable) => throw new NotImplementedException();

  [HarmonyReversePatch]
  [HarmonyPatch(typeof(ProtoFluxNode), "AssociateInstance")]
  [MethodImpl(MethodImplOptions.NoInlining)]
  internal static void AssociateInstance(ProtoFluxNode instance, ProtoFluxNodeGroup group, INode node) => throw new NotImplementedException();

  [HarmonyReversePatch]
  [HarmonyPatch(typeof(ProtoFluxNode), "ClearGroupAndInstance")]
  [MethodImpl(MethodImplOptions.NoInlining)]
  internal static void ClearGroupAndInstance(this ProtoFluxNode instance) => throw new NotImplementedException();
  #endregion
}