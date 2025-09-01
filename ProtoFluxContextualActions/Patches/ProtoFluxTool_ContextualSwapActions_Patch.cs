using System;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.ProtoFlux;

using ProtoFluxContextualActions.Attributes;
using HarmonyLib;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Transform;
using ProtoFlux.Core;
using System.Linq;
using FrooxEngine.Undo;
using ProtoFlux.Runtimes.Execution.Nodes;
using ProtoFlux.Runtimes.Execution.Nodes.Math.Easing;
using ProtoFlux.Runtimes.Execution.Nodes.Operators;
using ProtoFlux.Runtimes.Execution.Nodes.Math;
using ProtoFlux.Runtimes.Execution.Nodes.TimeAndDate;
using System.Collections;
using ProtoFlux.Runtimes.Execution;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Variables;
using ProtoFluxContextualActions.Extensions;
using ProtoFluxContextualActions.Utils;
using System.Diagnostics.CodeAnalysis;
using ProtoFluxContextualActions.Utils.ProtoFlux;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Users;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Users.Roots;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Time;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Operators;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Audio;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Slots;

namespace ProtoFluxContextualActions.Patches;

[HarmonyPatch(typeof(ProtoFluxTool), nameof(ProtoFluxTool.OnSecondaryPress))]
[HarmonyPatchCategory("ProtoFluxTool Contextual Swap Actions"), TweakCategory("Adds 'Contextual Swapping Actions' to the ProtoFlux Tool. Double pressing secondary pointing at a node with protoflux tool will be open a context menu of actions to swap the node for another node.", defaultValue: true)] // unstable, disable by default
internal static class ProtoFluxTool_ContextualSwapActions_Patch
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

  static readonly HashSet<Type> GetDirectionGroup = [
    typeof(GetForward),
    typeof(GetBackward),
    typeof(GetUp),
    typeof(GetDown),
    typeof(GetLeft),
    typeof(GetRight)
  ];

  // todo: async
  static readonly HashSet<Type> ForLoopGroup = [
    typeof(For),
    typeof(RangeLoopInt),
  ];

  // todo: currently there's too many, page support or custom uix menus are needed
  static readonly HashSet<Type> EasingGroupFloat = [
    typeof(EaseInBounceFloat),
    typeof(EaseInCircularFloat),
    typeof(EaseInCubicFloat),
    typeof(EaseInElasticFloat),
    typeof(EaseInExponentialFloat),
    typeof(EaseInOutBounceFloat),
    typeof(EaseInOutCircularFloat),
    typeof(EaseInOutCubicFloat),
    typeof(EaseInOutElasticFloat),
    typeof(EaseInOutExponentialFloat),
    typeof(EaseInOutQuadraticFloat),
    typeof(EaseInOutQuarticFloat),
    typeof(EaseInOutQuinticFloat),
    typeof(EaseInOutReboundFloat),
    typeof(EaseInOutSineFloat),
    typeof(EaseInQuadraticFloat),
    typeof(EaseInQuarticFloat),
    typeof(EaseInQuinticFloat),
    typeof(EaseInReboundFloat),
    typeof(EaseInSineFloat),
    typeof(EaseOutBounceFloat),
    typeof(EaseOutCircularFloat),
    typeof(EaseOutCubicFloat),
    typeof(EaseOutElasticFloat),
    typeof(EaseOutExponentialFloat),
    typeof(EaseOutQuadraticFloat),
    typeof(EaseOutQuarticFloat),
    typeof(EaseOutQuinticFloat),
    typeof(EaseOutReboundFloat),
    typeof(EaseOutSineFloat),
  ];

  static readonly HashSet<Type> EasingGroupDouble = [
    typeof(EaseInBounceDouble),
    typeof(EaseInCircularDouble),
    typeof(EaseInCubicDouble),
    typeof(EaseInElasticDouble),
    typeof(EaseInExponentialDouble),
    typeof(EaseInOutBounceDouble),
    typeof(EaseInOutCircularDouble),
    typeof(EaseInOutCubicDouble),
    typeof(EaseInOutElasticDouble),
    typeof(EaseInOutExponentialDouble),
    typeof(EaseInOutQuadraticDouble),
    typeof(EaseInOutQuarticDouble),
    typeof(EaseInOutQuinticDouble),
    typeof(EaseInOutReboundDouble),
    typeof(EaseInOutSineDouble),
    typeof(EaseInQuadraticDouble),
    typeof(EaseInQuarticDouble),
    typeof(EaseInQuinticDouble),
    typeof(EaseInReboundDouble),
    typeof(EaseInSineDouble),
    typeof(EaseOutBounceDouble),
    typeof(EaseOutCircularDouble),
    typeof(EaseOutCubicDouble),
    typeof(EaseOutElasticDouble),
    typeof(EaseOutExponentialDouble),
    typeof(EaseOutQuadraticDouble),
    typeof(EaseOutQuarticDouble),
    typeof(EaseOutQuinticDouble),
    typeof(EaseOutReboundDouble),
    typeof(EaseOutSineDouble),
  ];

  static readonly HashSet<Type> PlayOneShotGroup = [
    typeof(PlayOneShot),
    typeof(PlayOneShotAndWait),
  ];


  static readonly BiDictionary<Type, Type> MultiInputMappingGroup = new()
  {
    {typeof(ValueAdd<>), typeof(ValueAddMulti<>)},
    {typeof(ValueSub<>), typeof(ValueSubMulti<>)},
    {typeof(ValueMul<>), typeof(ValueMulMulti<>)},
    {typeof(ValueDiv<>), typeof(ValueDivMulti<>)},
    {typeof(ValueMin<>), typeof(ValueMinMulti<>)},
    {typeof(ValueMax<>), typeof(ValueMaxMulti<>)},
  };

  static readonly HashSet<Type> MinMaxGroup = [
    typeof(ValueMin<>),
    typeof(ValueMax<>),
  ];

  static readonly HashSet<Type> MinMaxMultiGroup = [
    typeof(ValueMinMulti<>),
    typeof(ValueMaxMulti<>),
  ];

  static readonly HashSet<Type> TimespanInstanceGroup = [
    typeof(TimeSpanFromDays),
    typeof(TimeSpanFromHours),
    typeof(TimeSpanFromMilliseconds),
    typeof(TimeSpanFromMinutes),
    typeof(TimeSpanFromSeconds),
    typeof(TimeSpanFromTicks),
  ];

  static readonly HashSet<Type> ArithmeticBinaryOperatorGroup = [
    typeof(ValueAdd<>),
    typeof(ValueSub<>),
    typeof(ValueMul<>),
    typeof(ValueDiv<>),
    typeof(ValueMod<>),
  ];

  static readonly HashSet<Type> ArithmeticMultiOperatorGroup = [
    typeof(ValueAddMulti<>),
    typeof(ValueSubMulti<>),
    typeof(ValueMulMulti<>),
    typeof(ValueDivMulti<>),
  ];

  static readonly HashSet<Type> ArithmeticRepeatGroup = [
    typeof(ValueMod<>),
    typeof(ValueRepeat<>),
  ];

  static readonly HashSet<Type> ArithmeticNegateGroup = [
    typeof(ValueNegate<>),
    typeof(ValuePlusMinus<>),
  ];

  static readonly HashSet<Type> ComparisonBinaryOperatorGroup = [
    typeof(ValueLessThan<>),
    typeof(ValueLessOrEqual<>),
    typeof(ValueGreaterThan<>),
    typeof(ValueGreaterOrEqual<>),
    typeof(ValueEquals<>),
    typeof(ValueNotEquals<>),
  ];

  static readonly HashSet<Type> ValueRelayGroup = [
    typeof(ValueRelay<>),
    typeof(ContinuouslyChangingValueRelay<>)
  ];

  static readonly HashSet<Type> ObjectRelayGroup = [
    typeof(ObjectRelay<>),
    typeof(ContinuouslyChangingObjectRelay<>)
  ];

  static readonly HashSet<Type> SlotMetaGroup = [
    typeof(GetSlotName),
    typeof(GetTag),
  ];

  static readonly HashSet<Type> FindSlotGroup = [
    typeof(FindChildByName),
    typeof(FindChildByTag),
  ];

  static readonly HashSet<Type> SetSlotTranformGlobalOperationGroup = [
    typeof(SetGlobalPosition),
    typeof(SetGlobalPositionRotation),
    typeof(SetGlobalRotation),
    typeof(SetGlobalScale),
    typeof(SetGlobalTransform),
  ];

  static readonly HashSet<Type> SetSlotTranformLocalOperationGroup = [
    typeof(SetLocalPosition),
    typeof(SetLocalPositionRotation),
    typeof(SetLocalRotation),
    typeof(SetLocalScale),
    typeof(SetLocalTransform),
  ];

  public static readonly HashSet<Type> DeltaTimeGroup = [
    typeof(DeltaTime),
    typeof(SmoothDeltaTime),
    typeof(InvertedDeltaTime),
    typeof(InvertedSmoothDeltaTime),
  ];

  public static readonly HashSet<Type> DeltaTimeOperationGroup = [
    typeof(MulDeltaTime<>),
    typeof(DivDeltaTime<>),
  ];

  static readonly HashSet<Type> UserBoolCheck = [
    typeof(IsLocalUser),
    typeof(IsUserHost),
  ];

  static readonly HashSet<Type> UserInfoGroup = [
    typeof(UserVR_Active),
    typeof(UserFPS),
    typeof(UserTime),
    typeof(UserVoiceMode),
    typeof(UserHeadOutputDevice),
    typeof(UserActiveViewTargettingController),
    typeof(UserPrimaryHand),
  ];

  static readonly HashSet<Type> UserRootSlotGroup = [
    typeof(HeadSlot),
    typeof(HandSlot),
    typeof(ControllerSlot),
  ];

  static readonly HashSet<Type> UserRootPositionGroup = [
    typeof(HeadPosition),
    typeof(HipsPosition),
    typeof(LeftHandPosition),
    typeof(RightHandPosition),
    typeof(FeetPosition),
  ];

  static readonly HashSet<Type> UserRootRotationGroup = [
    typeof(HeadRotation),
    typeof(HipsRotation),
    typeof(LeftHandRotation),
    typeof(RightHandRotation),
    typeof(FeetRotation),
  ];

  static readonly HashSet<Type> UserRootHeadRotationGroup = [
    typeof(HeadRotation),
    typeof(HeadFacingRotation),
    typeof(HeadFacingDirection),
  ];

  static readonly HashSet<Type> SetUserRootPositionGroup = [
    typeof(SetHeadPosition),
    typeof(SetHipsPosition),
    typeof(SetFeetPosition),
  ];

  static readonly HashSet<Type> SetUserRootRotationGroup = [
    typeof(SetHeadRotation),
    typeof(SetHipsRotation),
    typeof(SetFeetRotation),
  ];

  static readonly HashSet<Type> SetUserRootHeadRotationGroup = [
    typeof(SetHeadRotation),
    typeof(SetHeadFacingRotation),
    typeof(SetHeadFacingDirection),
  ];

  static readonly BiDictionary<Type, Type> GetUserRootSwapGroup =
    UserRootPositionGroup.Zip(UserRootRotationGroup).ToBiDictionary();

  static readonly BiDictionary<Type, Type> UserRootPositionSwapGroup =
    UserRootPositionGroup.Zip(SetUserRootPositionGroup).ToBiDictionary();

  static readonly BiDictionary<Type, Type> UserRootRotationSwapGroup =
    UserRootRotationGroup.Zip(SetUserRootRotationGroup).ToBiDictionary();

  static readonly BiDictionary<Type, Type> SetUserRootSwapGroup =
    SetUserRootPositionGroup.Zip(SetUserRootRotationGroup).ToBiDictionary();

  static readonly BiDictionary<Type, Type> HeadRotationSwapGroup =
    UserRootHeadRotationGroup.Zip(SetUserRootHeadRotationGroup).ToBiDictionary();

  static readonly BiDictionary<Type, Type> GetGlobalLocalEquivilents = new()
  {
    {typeof(GlobalTransform), typeof(LocalTransform)}
  };

  static readonly BiDictionary<Type, Type> SetGlobalLocalEquivilents =
    SetSlotTranformGlobalOperationGroup.Zip(SetSlotTranformLocalOperationGroup).ToBiDictionary();

  static readonly HashSet<Type> VariableStoreNodesGroup = [
    typeof(LocalValue<>),
    typeof(LocalObject<>),
    typeof(StoredValue<>),
    typeof(StoredObject<>),
    typeof(DataModelUserRefStore),
    typeof(DataModelTypeStore),
    typeof(DataModelObjectAssetRefStore<>),
    typeof(DataModelObjectAssetRefStore<>),
    typeof(DataModelValueFieldStore<>),
    typeof(DataModelObjectRefStore<>),
    typeof(DataModelObjectFieldStore<>),
  ];

  private static Type GetIVariableValueType(Type type)
  {
    if (TypeUtils.MatchInterface(type, typeof(IVariable<,>), out var varType))
    {
      return varType.GenericTypeArguments[1];
    }
    throw new Exception($"Unable to find IVariable node for type '{type}'");
  }

  static readonly Dictionary<Type, Type> protoFluxBindingMapping =
    Traverse.Create(typeof(ProtoFluxHelper)).Field<Dictionary<Type, Type>>("protoFluxToBindingMapping").Value.ToDictionary(a => a.Value, a => a.Key);

  internal static IEnumerable<MenuItem> GetMenuItems(ProtoFluxTool __instance, ProtoFluxNode nodeComponent)
  {
    var node = nodeComponent.NodeInstance;
    var nodeType = node.GetType();
    var componentType = nodeComponent.GetType();

    IEnumerable<IEnumerable<(Type Node, IEnumerable<Type> Types)>> binaryOperations = [
      [.. MapPsuedoGenericsToGenericTypes(__instance.World, "AND_")],
      [.. MapPsuedoGenericsToGenericTypes(__instance.World, "OR_")],
      [.. MapPsuedoGenericsToGenericTypes(__instance.World, "NAND_")],
      [.. MapPsuedoGenericsToGenericTypes(__instance.World, "NOR_")],
      [.. MapPsuedoGenericsToGenericTypes(__instance.World, "XNOR_")],
      [.. MapPsuedoGenericsToGenericTypes(__instance.World, "XOR_")]
    ];

    IEnumerable<IEnumerable<(Type Node, IEnumerable<Type> Types)>> binaryOperationsMulti = [
      [.. MapPsuedoGenericsToGenericTypes(__instance.World, "AND_Multi_")],
      [.. MapPsuedoGenericsToGenericTypes(__instance.World, "OR_Multi_")],
      [.. MapPsuedoGenericsToGenericTypes(__instance.World, "NAND_Multi_")],
      [.. MapPsuedoGenericsToGenericTypes(__instance.World, "NOR_Multi_")],
      [.. MapPsuedoGenericsToGenericTypes(__instance.World, "XNOR_Multi_")],
      [.. MapPsuedoGenericsToGenericTypes(__instance.World, "XOR_Multi_")]
    ];

    // todo: cache per-world?
    // realistically with current resonite it doesn't matter and only needs to be done once.
    var binaryOperationsGroup = binaryOperations
        .SelectMany(a => a)
        .ToDictionary((a) => protoFluxBindingMapping[a.Node], (a) => a.Types.ToArray());

    var binaryOperationsMultiGroup = binaryOperationsMulti
        .SelectMany(a => a)
        .ToDictionary((a) => protoFluxBindingMapping[a.Node], (a) => a.Types.ToArray());

    var binaryOperationsMultiSwapMap = binaryOperationsGroup.Keys.Zip(binaryOperationsMultiGroup.Keys).ToBiDictionary();

    var numericLogGroup = MapPsuedoGenericsToGenericTypes(__instance.World, "Log_")
      .Concat(MapPsuedoGenericsToGenericTypes(__instance.World, "Log10_"))
      .Concat(MapPsuedoGenericsToGenericTypes(__instance.World, "LogN_"))
      .ToDictionary((a) => protoFluxBindingMapping[a.Node], (a) => a.Types.ToArray());

    var avgGroup = MapPsuedoGenericsToGenericTypes(__instance.World, "Avg_")
      .Concat(MapPsuedoGenericsToGenericTypes(__instance.World, "AvgMulti_"))
      .ToDictionary((a) => protoFluxBindingMapping[a.Node], (a) => a.Types);

    var approximatelyGroup = MapPsuedoGenericsToGenericTypes(__instance.World, "Approximately_")
      .Concat(MapPsuedoGenericsToGenericTypes(__instance.World, "ApproximatelyNot_"))
      .ToDictionary((a) => protoFluxBindingMapping[a.Node], (a) => a.Types);


    if (GetDirectionGroup.Contains(nodeType))
    {
      foreach (var match in GetDirectionGroup)
      {
        yield return new MenuItem(match);
      }
    }

    if (ForLoopGroup.Contains(nodeType))
    {
      foreach (var match in ForLoopGroup)
      {
        yield return new MenuItem(match, connectionTransferType: ConnectionTransferType.ByMappingsLossy);
      }
    }

    if (EasingGroupFloat.Contains(nodeType))
    {
      foreach (var match in EasingGroupFloat)
      {
        yield return new MenuItem(match);
      }
    }

    if (EasingGroupDouble.Contains(nodeType))
    {
      foreach (var match in EasingGroupDouble)
      {
        yield return new MenuItem(match);
      }
    }

    if (TimespanInstanceGroup.Contains(nodeType))
    {
      foreach (var match in TimespanInstanceGroup)
      {
        yield return new MenuItem(match);
      }
    }

    if (SetSlotTranformGlobalOperationGroup.Contains(nodeType))
    {
      foreach (var match in SetSlotTranformGlobalOperationGroup)
      {
        yield return new MenuItem(match);
      }
    }

    if (SetSlotTranformLocalOperationGroup.Contains(nodeType))
    {
      foreach (var match in SetSlotTranformLocalOperationGroup)
      {
        yield return new MenuItem(match);
      }
    }


    if (UserInfoGroup.Contains(nodeType))
    {
      foreach (var match in UserInfoGroup)
      {
        yield return new MenuItem(match);
      }
    }

    if (DeltaTimeGroup.Contains(nodeType))
    {
      foreach (var match in DeltaTimeGroup)
      {
        yield return new MenuItem(match);
      }
    }

    if (UserBoolCheck.Contains(nodeType))
    {
      foreach (var match in UserBoolCheck)
      {
        yield return new MenuItem(match);
      }
    }

    if (PlayOneShotGroup.Contains(nodeType))
    {
      foreach (var match in PlayOneShotGroup)
      {
        yield return new MenuItem(match);
      }
    }


    if (FindSlotGroup.Contains(nodeType))
    {
      foreach (var match in FindSlotGroup)
      {
        yield return new MenuItem(match);
      }
    }

    if (SlotMetaGroup.Contains(nodeType))
    {
      foreach (var match in SlotMetaGroup)
      {
        yield return new MenuItem(match);
      }
    }

    #region User Root
    {
      if (UserRootSlotGroup.Contains(nodeType))
      {
        foreach (var match in UserRootSlotGroup)
        {
          yield return new MenuItem(match);
        }
      }

      if (UserRootPositionGroup.Contains(nodeType))
      {
        foreach (var match in UserRootPositionGroup)
        {
          yield return new MenuItem(match);
        }
      }

      if (UserRootRotationGroup.Contains(nodeType))
      {
        foreach (var match in UserRootRotationGroup)
        {
          yield return new MenuItem(match);
        }
      }

      if (SetUserRootPositionGroup.Contains(nodeType))
      {
        foreach (var match in SetUserRootPositionGroup)
        {
          yield return new MenuItem(match);
        }
      }

      if (SetUserRootRotationGroup.Contains(nodeType))
      {
        foreach (var match in SetUserRootRotationGroup)
        {
          yield return new MenuItem(match);
        }
      }

      if (UserRootHeadRotationGroup.Contains(nodeType))
      {
        foreach (var match in UserRootHeadRotationGroup)
        {
          yield return new MenuItem(match);
        }
      }

      if (SetUserRootHeadRotationGroup.Contains(nodeType))
      {
        foreach (var match in SetUserRootHeadRotationGroup)
        {
          yield return new MenuItem(match);
        }
      }

      {
        if (TryGetSwap(GetUserRootSwapGroup, nodeType, out Type match)) yield return new(match);
        if (TryGetSwap(UserRootPositionSwapGroup, nodeType, out match)) yield return new(match);
        if (TryGetSwap(UserRootRotationSwapGroup, nodeType, out match)) yield return new(match);
        if (TryGetSwap(SetUserRootSwapGroup, nodeType, out match)) yield return new(match);
        if (TryGetSwap(HeadRotationSwapGroup, nodeType, out match)) yield return new(match);
      }
    }
    #endregion

    {
      if (TryGetSwap(SetGlobalLocalEquivilents, nodeType, out var match)) yield return new(match);
      if (TryGetSwap(GetGlobalLocalEquivilents, nodeType, out match)) yield return new(match);
    }

    {
      if (binaryOperationsGroup.TryGetValue(nodeType, out var genericTypes))
      {
        var matchingNodes = binaryOperationsGroup.Where(a => genericTypes.SequenceEqual(a.Value)).Select(a => a.Key);
        foreach (var match in matchingNodes)
        {
          yield return new MenuItem(match);
        }
      }
    }

    {
      if (binaryOperationsMultiGroup.TryGetValue(nodeType, out var genericTypes))
      {
        var matchingNodes = binaryOperationsMultiGroup.Where(a => genericTypes.SequenceEqual(a.Value)).Select(a => a.Key);
        foreach (var match in matchingNodes)
        {
          yield return new MenuItem(
            node: match,
            name: $"{NodeMetadataHelper.GetMetadata(match).Name} (Multi)",
            connectionTransferType: ConnectionTransferType.ByIndexLossy
          );
        }
      }
    }

    {
      if (binaryOperationsMultiSwapMap.TryGetFirst(nodeType, out var matched))
      {
        yield return new MenuItem(
          node: matched,
          connectionTransferType: ConnectionTransferType.ByIndexLossy
        );
      }
      else if (binaryOperationsMultiSwapMap.TryGetSecond(nodeType, out matched))
      {
        yield return new MenuItem(
          node: matched,
          name: $"{NodeMetadataHelper.GetMetadata(matched).Name} (Multi)",
          connectionTransferType: ConnectionTransferType.ByIndexLossy
        );
      }
    }

    {
      if (numericLogGroup.TryGetValue(nodeType, out var genericTypes))
      {
        var matchingNodes = numericLogGroup.Where(a => genericTypes.SequenceEqual(a.Value)).Select(a => a.Key);
        foreach (var match in matchingNodes)
        {
          yield return new MenuItem(match);
        }
      }
    }

    {
      if (approximatelyGroup.TryGetValue(nodeType, out var genericTypes))
      {
        var matchingNodes = approximatelyGroup.Where(a => genericTypes.SequenceEqual(a.Value)).Select(a => a.Key);
        foreach (var match in matchingNodes)
        {
          yield return new MenuItem(match);
        }
      }
    }


    {
      if (avgGroup.TryGetValue(nodeType, out var genericTypes))
      {
        var matchingNodes = avgGroup.Where(a => genericTypes.SequenceEqual(a.Value)).Select(a => a.Key);
        foreach (var match in matchingNodes)
        {
          yield return new MenuItem(
            node: match,
            name: match.GetNiceTypeName().Contains("Multi_") ? $"{NodeMetadataHelper.GetMetadata(match).Name} (Multi)" : null,
            connectionTransferType: ConnectionTransferType.ByIndexLossy
          );
        }
        if (nodeType.GetNiceTypeName().Contains("Multi_"))
        {
          foreach (var match in MinMaxMultiGroup)
          {
            yield return new MenuItem(
              node: match.MakeGenericType([.. genericTypes]),
              name: $"{NodeMetadataHelper.GetMetadata(match).Name} (Multi)",
              connectionTransferType: ConnectionTransferType.ByIndexLossy
            );
          }
        }
        else
        {
          foreach (var match in MinMaxGroup)
          {
            yield return new MenuItem(
              node: match.MakeGenericType([.. genericTypes]),
              connectionTransferType: ConnectionTransferType.ByIndexLossy
            );
          }
        }
      }
    }

    if (VariableStoreNodesGroup.Any(t => nodeType.IsGenericType ? t == nodeType.GetGenericTypeDefinition() : t == nodeType))
    {
      var storageType = GetIVariableValueType(nodeType);
      yield return new MenuItem(protoFluxBindingMapping[ProtoFluxHelper.GetLocalNode(storageType).GetGenericTypeDefinition()].MakeGenericType(storageType));
      yield return new MenuItem(protoFluxBindingMapping[ProtoFluxHelper.GetStoreNode(storageType).GetGenericTypeDefinition()].MakeGenericType(storageType));

      var dataModelStore = ProtoFluxHelper.GetDataModelStoreNode(storageType);
      if (dataModelStore.IsGenericType)
      {
        yield return new MenuItem(protoFluxBindingMapping[dataModelStore.GetGenericTypeDefinition()].MakeGenericType(dataModelStore.GenericTypeArguments));
      }
      else
      {
        yield return new MenuItem(protoFluxBindingMapping[dataModelStore]);
      }
    }

    #region  Generics Handling
    if (nodeType.TryGetGenericTypeDefinition(out var genericType))
    {
      if (ValueRelayGroup.Contains(genericType))
      {
        foreach (var match in ValueRelayGroup)
        {
          yield return new MenuItem(match.MakeGenericType(nodeType.GenericTypeArguments[0]));
        }
      }

      if (ObjectRelayGroup.Contains(genericType))
      {
        foreach (var match in ObjectRelayGroup)
        {
          yield return new MenuItem(match.MakeGenericType(nodeType.GenericTypeArguments[0]));
        }
      }

      if (ComparisonBinaryOperatorGroup.Contains(genericType))
      {
        foreach (var match in ComparisonBinaryOperatorGroup)
        {
          yield return new MenuItem(match.MakeGenericType(nodeType.GenericTypeArguments[0]));
        }
      }

      if (DeltaTimeOperationGroup.Contains(genericType))
      {
        foreach (var match in DeltaTimeOperationGroup)
        {
          yield return new MenuItem(match.MakeGenericType(nodeType.GenericTypeArguments[0]));
        }
      }

      {
        if (MinMaxGroup.Contains(genericType))
        {
          var innerType = nodeType.GenericTypeArguments[0];
          foreach (var match in MinMaxGroup)
          {
            yield return new MenuItem(match.MakeGenericType(innerType));
          }

          var matchingNodes = avgGroup
            .Where(a => a.Value.FirstOrDefault() == innerType)
            .Select(a => a.Key)
            .Where(a => !a.GetNiceTypeName().Contains("Multi_"));

          foreach (var match in matchingNodes)
          {
            yield return new MenuItem(
              node: match,
              connectionTransferType: ConnectionTransferType.ByIndexLossy
            );
          }
        }

        if (MinMaxMultiGroup.Contains(genericType))
        {
          var innerType = nodeType.GenericTypeArguments[0];
          foreach (var match in MinMaxMultiGroup)
          {
            yield return new MenuItem(match.MakeGenericType(innerType));
          }

          var matchingNodes = avgGroup
            .Where(a => a.Value.FirstOrDefault() == innerType)
            .Select(a => a.Key)
            .Where(a => a.GetNiceTypeName().Contains("Multi_"));

          foreach (var match in matchingNodes)
          {
            yield return new MenuItem(
              node: match,
              name: $"{NodeMetadataHelper.GetMetadata(match).Name} (Multi)",
              connectionTransferType: ConnectionTransferType.ByIndexLossy
            );
          }
        }
      }

      if (ArithmeticBinaryOperatorGroup.Contains(genericType))
      {
        var opType = nodeType.GenericTypeArguments[0];
        var coder = Traverse.Create(typeof(Coder<>).MakeGenericType(opType));

        if (coder.Property<bool>("SupportsAddSub").Value)
        {
          yield return new MenuItem(typeof(ValueAdd<>).MakeGenericType(opType));
          yield return new MenuItem(typeof(ValueSub<>).MakeGenericType(opType));
        }

        if (coder.Property<bool>("SupportsMul").Value)
        {
          yield return new MenuItem(typeof(ValueMul<>).MakeGenericType(opType));
        }

        if (coder.Property<bool>("SupportsDiv").Value)
        {
          yield return new MenuItem(typeof(ValueDiv<>).MakeGenericType(opType));
        }

        if (coder.Property<bool>("SupportsMod").Value)
        {
          yield return new MenuItem(typeof(ValueMod<>).MakeGenericType(opType));
        }
      }

      if (ArithmeticMultiOperatorGroup.Contains(genericType))
      {
        var opType = nodeType.GenericTypeArguments[0];
        var coder = Traverse.Create(typeof(Coder<>).MakeGenericType(opType));

        static MenuItem MultiMenuItem(Type nodeType) => new(
          node: nodeType,
          name: nodeType.GetNiceTypeName(),
          connectionTransferType: ConnectionTransferType.ByIndexLossy
        );

        if (coder.Property<bool>("SupportsAddSub").Value)
        {
          yield return MultiMenuItem(typeof(ValueAddMulti<>).MakeGenericType(opType));
          yield return MultiMenuItem(typeof(ValueSubMulti<>).MakeGenericType(opType));
        }

        if (coder.Property<bool>("SupportsMul").Value)
        {
          yield return MultiMenuItem(typeof(ValueMulMulti<>).MakeGenericType(opType));
        }

        if (coder.Property<bool>("SupportsDiv").Value)
        {
          yield return MultiMenuItem(typeof(ValueDivMulti<>).MakeGenericType(opType));
        }
      }

      if (ArithmeticRepeatGroup.Contains(genericType))
      {
        var opType = nodeType.GenericTypeArguments[0];
        var coder = Traverse.Create(typeof(Coder<>).MakeGenericType(opType));

        static MenuItem RepeatItem(Type nodeType) => new(
          node: nodeType,
          connectionTransferType: ConnectionTransferType.ByIndexLossy
        );

        if (coder.Property<bool>("SupportsRepeat").Value)
        {
          yield return RepeatItem(typeof(ValueRepeat<>).MakeGenericType(opType));
        }

        if (coder.Property<bool>("SupportsMod").Value)
        {
          yield return RepeatItem(typeof(ValueMod<>).MakeGenericType(opType));
        }
      }

      if (ArithmeticNegateGroup.Contains(genericType))
      {
        var opType = nodeType.GenericTypeArguments[0];
        var coder = Traverse.Create(typeof(Coder<>).MakeGenericType(opType));

        if (coder.Property<bool>("SupportsNegate").Value)
        {
          yield return new(typeof(ValueNegate<>).MakeGenericType(opType));
        }

        if (coder.Property<bool>("SupportsAddSub").Value)
        {
          yield return new(typeof(ValuePlusMinus<>).MakeGenericType(opType));
        }
      }

      if (MultiInputMappingGroup.TryGetSecond(genericType, out var mapped))
      {
        var binopType = nodeType.GenericTypeArguments[0];
        yield return new MenuItem(
          node: mapped.MakeGenericType(binopType),
          name: mapped.GetNiceTypeName(),
          connectionTransferType: ConnectionTransferType.ByIndexLossy
        );
      }
      else if (MultiInputMappingGroup.TryGetFirst(genericType, out mapped))
      {
        var binopType = nodeType.GenericTypeArguments[0];
        yield return new MenuItem(mapped.MakeGenericType(binopType), connectionTransferType: ConnectionTransferType.ByIndexLossy);
      }
    }
  }

  private static bool TryGetSwap(BiDictionary<Type, Type> swaps, Type nodeType, out Type match) =>
    swaps.TryGetSecond(nodeType, out match) || swaps.TryGetFirst(nodeType, out match);

  #endregion

  #region Utils
  static bool TryGetGenericTypeDefinition(this Type type, [NotNullWhen(true)] out Type? genericTypeDefinition)
  {
    if (type.IsGenericType)
    {
      genericTypeDefinition = type.GetGenericTypeDefinition();
      return true;
    }
    genericTypeDefinition = null;
    return false;
  }

  public static IEnumerable<(Type Node, IEnumerable<Type> Types)> MapPsuedoGenericsToGenericTypes(World world, string startingWith)
  {
    var protoFluxNodes = Traverse.Create(typeof(ProtoFluxHelper)).Field<Dictionary<string, Type>>("protoFluxNodes").Value;
    return protoFluxNodes.Values
      .Select(t => (name: t.GetNiceTypeName(), type: t))
      .Where(a => a.name.StartsWith(startingWith) && !a.type.IsGenericType)
      .Select(a => (a.type, ParseUnderscoreGenerics(world, a.name[startingWith.Length..])))
      // skip non matching
      .Where(a => a.Item2.All(t => t != null));
  }

  class ArrayComparer<T> : EqualityComparer<T[]>
  {
    public override bool Equals(T[]? x, T[]? y) =>
      StructuralComparisons.StructuralEqualityComparer.Equals(x, y);

    public override int GetHashCode(T[] obj) =>
      StructuralComparisons.StructuralEqualityComparer.GetHashCode(obj);
  }

  static IEnumerable<Type> ParseUnderscoreGenerics(World world, string generics) =>
    generics.Split('_').Select(name => world.Types.DecodeType(name.ToLower()) ?? world.Types.DecodeType(name));


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

  // [HarmonyReversePatch]
  // [HarmonyPatch(typeof(ProtoFluxNode), "ReverseMapElements")]
  // [MethodImpl(MethodImplOptions.NoInlining)]
  // internal static void ReverseMapElements(ProtoFluxNode instance, Dictionary<INode, ProtoFluxNode> nodeMapping, bool undoable) => throw new NotImplementedException();
}

// todo: optimization pass with static dictionary
// var nodesWithGenericArguments = new Dictionary<Type[], List<Type>>(binaryOperations.Count, new ArrayComparer<Type>());
// foreach (var (psuedoGenericNode, types) in binaryOperations) nodesWithGenericArguments.Add(types, psuedoGenericNode);

// if (nodesWithGenericArguments.TryGetValue(genericTypes, out var matchingNodes))
// {
//   foreach (var match in matchingNodes)
//   {
//     yield return new MenuItem(match);
//   }
// }