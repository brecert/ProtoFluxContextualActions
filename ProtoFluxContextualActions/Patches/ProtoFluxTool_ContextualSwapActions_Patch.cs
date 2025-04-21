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
using System.Runtime.InteropServices;
using ProtoFlux.Runtimes.Execution.Nodes.Math;

namespace ProtoFluxContextualActions.Patches;

[HarmonyPatchCategory("ProtoFluxTool Contextual Swap Actions"), TweakCategory("Adds 'Contextual Swapping Actions' to the ProtoFlux Tool. Double pressing secondary pointing at a node with protoflux tool will be open a context menu of actions to swap the node for another node.")]
[HarmonyPatch(typeof(ProtoFluxTool), nameof(ProtoFluxTool.OnSecondaryPress))]
internal static class ProtoFluxTool_ContextualSwapActions_Patch
{
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

  internal readonly struct MenuItem(Type node, Type? binding = null, string? name = null, ConnectionTransferType? connectionTransferType = ConnectionTransferType.ByNameLossy, Action<ProtoFluxNode>? onInit = null)
  {
    internal readonly Type node = node;
    internal readonly Type? binding = binding;
    internal readonly string? name = name;

    internal readonly ConnectionTransferType? connectionTransferType = connectionTransferType;

    internal readonly string DisplayName => name ?? NodeMetadataHelper.GetMetadata(node).Name ?? node.GetNiceTypeName();

    internal readonly Action<ProtoFluxNode>? onInit = onInit;
  }

  internal class ProtoFluxNodeData
  {
    internal DateTime? lastSecondaryPress;
    internal ProtoFluxNode? lastSecondaryPressNode;

    internal double SecondsSinceLastSecondaryPress() => (DateTime.Now - lastSecondaryPress.GetValueOrDefault()).TotalSeconds;
  }

  const double DoublePressTime = 1;

  private static readonly ConditionalWeakTable<ProtoFluxTool, ProtoFluxNodeData> additionalData = new();

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
            // skip rest
            return false;
          }
          else
          {
            data.lastSecondaryPressNode = hitNode;
            data.lastSecondaryPress = DateTime.Now;
            // skip null
            return true;
          }
        }
      }

      data.lastSecondaryPressNode = null;
      data.lastSecondaryPress = null;
    }

    return true;
  }

  // internal static readonly HashSet<Type>[] SelectionGroups = [
  //   [typeof(GetForward), typeof(GetBackward), typeof(GetUp), typeof(GetDown), typeof(GetLeft), typeof(GetRight)],
  //   [typeof(ValueAdd<>), typeof(ValueSub<>), typeof(ValueMul<>), typeof(ValueDiv<>)],
  //   [typeof(For), typeof(AsyncFor)],
  //   [typeof(ValueInc<>), typeof(ValueDec<>)],
  // ];

  // TODO: find alternative to this, even generating it at runtime will feel bad.
  //       alternatives include manual conversions, 
  internal static readonly Dictionary<(Type, Type), Dictionary<string, string>> CompiledInputMappings = new() {
    {(typeof(For), typeof(RangeLoopInt)), new () {
      {"Count", "End"}
    }},
    {(typeof(RangeLoopInt), typeof(For)), new () {
      {"End", "Count"}
    }},
  };

  internal static readonly Dictionary<(Type, Type), Dictionary<string, string>> CompiledOutputMappings = new() {
    {(typeof(For), typeof(RangeLoopInt)), new () {
      {"Iteration", "Current"}
    }},
    {(typeof(RangeLoopInt), typeof(For)), new () {
      {"Current", "Iteration"}
    }},
  };

  // todo: pages
  private static void CreateMenu(ProtoFluxTool __instance, ProtoFluxNode hitNode)
  {
    __instance.StartTask(async () =>
    {
      var menu = await __instance.LocalUser.OpenContextMenu(__instance, __instance.Slot);
      var items = GetMenuItems(__instance, hitNode).Take(10);
      // TODO: pages / menus

      foreach (var menuItem in items)
      {
        AddMenuItem(__instance, menu, colorX.White, menuItem, () =>
        {
          var runtime = hitNode.NodeInstance.Runtime;
          var oldNode = hitNode.NodeInstance;
          var binding = ProtoFluxHelper.GetBindingForNode(menuItem.node);

          var undoBatch = __instance.World.BeginUndoBatch($"Swap {hitNode.Name} to {menuItem.DisplayName}");

          var newNode = __instance.SpawnNode(binding, n =>
          {
            // this will be so buggy, I need to make my own protoflux helper library so I'm not constantly fighting all of the edge cases of protoflux
            var sharedInputListsByOrder = hitNode.NodeInputLists.Zip(n.NodeInputLists, (a, b) => (a, b)).Where((z) => z.b.GetType().IsAssignableFrom(z.a.GetType()));
            var elementsEnsured = false;
            foreach (var (oldList, newList) in sharedInputListsByOrder)
            {
              if (oldList.Count > newList.Count)
              {
                elementsEnsured = true;
                var missingElements = oldList.Count - newList.Count;
                for (int i = 0; i < missingElements; i++)
                {
                  newList.AddElement();
                }
              }
            }
            if (!elementsEnsured)
            {
              n.EnsureElementsInDynamicLists();
            }
          });
          newNode.Slot.TRS = hitNode.Slot.TRS;
          menuItem.onInit?.Invoke(newNode);

          var query = new NodeQueryAcceleration(runtime.Group);
          var evaluatingNodes = query.GetEvaluatingNodes(oldNode);
          var impulsingNodes = query.GetImpulsingNodes(oldNode);
          var nodeReferences = hitNode.Group.Nodes.ToDictionary(n => n.NodeInstance);
          var remappedNodes = new Dictionary<INode, INode>() {
            {oldNode, newNode.NodeInstance}
          };
          var oldMeta = NodeMetadataHelper.GetMetadata(oldNode.GetType());
          var newMeta = NodeMetadataHelper.GetMetadata(newNode.NodeType);

          // outputs
          switch (menuItem.connectionTransferType)
          {
            case ConnectionTransferType.ByIndexLossy:
              goto case ConnectionTransferType.ByNameLossy;
            case ConnectionTransferType.ByMappingsLossy:
              {
                var nodeOutputMappings = CompiledOutputMappings.GetValueSafe((oldNode.GetType(), menuItem.node));

                var evaluatingInputs = evaluatingNodes.SelectMany(n => nodeReferences[n].AllInputs)
                  .Where(i => ((INodeOutput)i.Target)?.MappedOutput?.OwnerNode == oldNode);

                foreach (var input in evaluatingInputs)
                {
                  var oldName = oldNode.GetOutputName(((INodeOutput)input.Target).MappedOutput.FindLinearOutputIndex());
                  if (nodeOutputMappings.TryGetValue(oldName, out var newName))
                  {
                    var newOutputIndex = newMeta.GetOutputByName(newName).Index;
                    input.TrySet(newNode.GetOutput(newOutputIndex));
                  }
                }

                goto case ConnectionTransferType.ByNameLossy;
              }
            case ConnectionTransferType.ByNameLossy:
              {
                var evaluatingInputs = evaluatingNodes.SelectMany(n => nodeReferences[n].AllInputs)
                  .Where(i => ((INodeOutput)i.Target)?.MappedOutput?.OwnerNode == oldNode);

                foreach (var input in evaluatingInputs)
                {
                  var oldName = oldNode.GetOutputName(((INodeOutput)input.Target).MappedOutput.FindLinearOutputIndex());
                  var newOutputIndex = newMeta.GetOutputByName(oldName).Index;
                  input.TrySet(newNode.GetOutput(newOutputIndex));
                }

                break;
              }
          }

          // inputs
          switch (menuItem.connectionTransferType)
          {
            case ConnectionTransferType.ByIndexLossy:
              var validInputs = hitNode.AllInputs.Zip(newNode.AllInputs, (a, b) => (a, b)).Where(z => z.a.TargetType == z.b.TargetType);
              foreach (var (oldInput, newInput) in validInputs)
              {
                newInput.TrySet(oldInput.Target);
              }
              goto case ConnectionTransferType.ByNameLossy;
            case ConnectionTransferType.ByMappingsLossy:
              {
                var nodeInputMappings = CompiledInputMappings.GetValueSafe((oldNode.GetType(), menuItem.node));

                foreach (var oldInput in hitNode.AllInputs)
                {
                  if (nodeInputMappings?.TryGetValue(oldInput.Name, out var mappedName) ?? false)
                  {
                    var input = newNode.AllInputs.FirstOrDefault(i => i.Name == mappedName);
                    input.TrySet(oldInput.Target);
                  }
                }
                goto case ConnectionTransferType.ByNameLossy;
              }
            case ConnectionTransferType.ByNameLossy:
              {
                foreach (var newInput in newNode.AllInputs)
                {
                  foreach (var oldInput in hitNode.AllInputs)
                  {
                    if (newInput.Name == oldInput.Name)
                    {
                      newInput.TrySet(oldInput.Target);
                    }
                  }
                }
                break;
              }
          }


          // output impulses
          foreach (var oldImpulse in hitNode.AllImpulses)
          {
            foreach (var newImpulse in newNode.AllImpulses)
            {
              if (oldImpulse.Name == newImpulse.Name)
              {
                newNode.TryConnectImpulse(newImpulse, (INodeOperation)oldImpulse.Target, undoable: true);
              }
            }
          }

          // input impulses
          foreach (var impulsingNode in impulsingNodes)
          {
            foreach (var impulseRef in nodeReferences[impulsingNode].AllImpulses)
            {
              foreach (var operation in hitNode.NodeOperations)
              {
                if (impulseRef.Target == operation)
                {
                  var index = operation.MappedOperation.FindLinearOperationIndex();
                  nodeReferences[impulsingNode].TryConnectImpulse(impulseRef, newNode.GetOperation(index), undoable: true);
                }
              }
            }
          }

          // delay for "seamless" transition
          // without this there will be an update where there are no nodes connectedm
          // that is unacceptable as it would cause unintented behavior with protoflux that can lead to destructive actions
          __instance.StartTask(async () =>
          {
            await new Updates(1);
            hitNode.Slot.Destroy();
          });

          __instance.World.EndUndoBatch();
        });
      }
    });
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

  static readonly HashSet<Type> ForLoopGroup = [
    typeof(For),
    typeof(RangeLoopInt),
  ];

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

  static readonly HashSet<Type> BinaryOperationGroup = [
    typeof(ValueAdd<>),
    typeof(ValueSub<>),
    typeof(ValueMul<>),
    typeof(ValueDiv<>),
    typeof(ValueMod<>),
  ];

  static readonly HashSet<Type> BinaryOperationGroupMulti = [
    typeof(ValueAddMulti<>),
    typeof(ValueSubMulti<>),
    typeof(ValueMulMulti<>),
    typeof(ValueDivMulti<>),
  ];

  static readonly BiDictionary<Type, Type> MultiInputMappingGroup = new() {
    {typeof(ValueAdd<>), typeof(ValueAddMulti<>)},
    {typeof(ValueSub<>), typeof(ValueSubMulti<>)},
    {typeof(ValueMul<>), typeof(ValueMulMulti<>)},
    {typeof(ValueDiv<>), typeof(ValueDivMulti<>)},
    {typeof(ValueMin<>), typeof(ValueMinMulti<>)},
    {typeof(ValueMax<>), typeof(ValueMaxMulti<>)},
  };

  internal static IEnumerable<MenuItem> GetMenuItems(ProtoFluxTool __instance, ProtoFluxNode nodeComponent)
  {
    var node = nodeComponent.NodeInstance;
    var nodeType = node.GetType();

    if (GetDirectionGroup.Contains(nodeType))
    {
      foreach (var match in GetDirectionGroup)
      {
        if (match == nodeType) continue;
        yield return new MenuItem(match);
      }
    }

    if (ForLoopGroup.Contains(nodeType))
    {
      foreach (var match in ForLoopGroup)
      {
        if (match == nodeType) continue;
        yield return new MenuItem(match, connectionTransferType: ConnectionTransferType.ByMappingsLossy);
      }
    }

    if (EasingGroupFloat.Contains(nodeType))
    {
      foreach (var match in EasingGroupFloat)
      {
        if (match == nodeType) continue;
        yield return new MenuItem(match);
      }
    }

    if (EasingGroupDouble.Contains(nodeType))
    {
      foreach (var match in EasingGroupDouble)
      {
        if (match == nodeType) continue;
        yield return new MenuItem(match);
      }
    }

    if (nodeType.TryGetGenericTypeDefinition(out var genericType))
    {
      if (BinaryOperationGroup.Contains(genericType))
      {
        var binopType = nodeType.GenericTypeArguments[0];
        foreach (var match in BinaryOperationGroup)
        {
          var matchedNodeType = match.MakeGenericType(binopType);
          if (matchedNodeType == nodeType) continue;
          yield return new MenuItem(matchedNodeType);
        }
      }

      if (BinaryOperationGroupMulti.Contains(genericType))
      {
        var binopType = nodeType.GenericTypeArguments[0];
        foreach (var match in BinaryOperationGroupMulti)
        {
          var matchedNodeType = match.MakeGenericType(binopType);
          if (matchedNodeType == nodeType) continue;
          yield return new MenuItem(
            node: matchedNodeType,
            name: matchedNodeType.GetNiceTypeName(),
            connectionTransferType: ConnectionTransferType.ByIndexLossy
          );
        }
      }

      // MultiInputMappingGroup
      {
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

  }

  // Utils
  static bool TryGetGenericTypeDefinition(this Type type, out Type? genericTypeDefinition)
  {
    if (type.IsGenericType)
    {
      genericTypeDefinition = type.GetGenericTypeDefinition();
      return true;
    }
    genericTypeDefinition = null;
    return false;
  }


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
  [HarmonyPatch(typeof(ProtoFluxNode), "ReverseMapElements")]
  [MethodImpl(MethodImplOptions.NoInlining)]
  internal static void ReverseMapElements(ProtoFluxNode instance, Dictionary<INode, ProtoFluxNode> nodeMapping, bool undoable) => throw new NotImplementedException();
}
