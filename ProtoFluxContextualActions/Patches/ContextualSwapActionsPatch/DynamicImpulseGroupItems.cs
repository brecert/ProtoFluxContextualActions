using Elements.Core;
using FrooxEngine;
using FrooxEngine.ProtoFlux;
using HarmonyLib;
using ProtoFlux.Core;
using ProtoFlux.Runtimes.Execution.Nodes.Actions;
using ProtoFluxContextualActions.Extensions;
using ProtoFluxContextualActions.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> DynamicImpulseTriggerGroup = [
    typeof(DynamicImpulseTrigger),
    typeof(DynamicImpulseTriggerWithValue<>),
    typeof(DynamicImpulseTriggerWithObject<>),
  ];

  static readonly HashSet<Type> AsyncDynamicImpulseTriggerGroup = [
    typeof(AsyncDynamicImpulseTrigger),
    typeof(AsyncDynamicImpulseTriggerWithValue<>),
    typeof(AsyncDynamicImpulseTriggerWithObject<>),
  ];

  static readonly BiDictionary<Type, Type> AsyncDynamicImpulseTriggerMap =
    DynamicImpulseTriggerGroup.Zip(AsyncDynamicImpulseTriggerGroup).ToBiDictionary();

  static readonly HashSet<Type> DynamicImpulseReceiverGroup = [
    typeof(DynamicImpulseReceiver),
    typeof(DynamicImpulseReceiverWithValue<>),
    typeof(DynamicImpulseReceiverWithObject<>),
    typeof(DynamicImpulseTriggerWithValue<>),
    typeof(DynamicImpulseTriggerWithObject<>)
  ];

  static readonly HashSet<Type> AsyncDynamicImpulseReceiverGroup = [
    typeof(AsyncDynamicImpulseReceiver),
    typeof(AsyncDynamicImpulseReceiverWithValue<>),
    typeof(AsyncDynamicImpulseReceiverWithObject<>),
  ];

  static readonly BiDictionary<Type, Type> AsyncDynamicImpulseReceiverMap =
    DynamicImpulseReceiverGroup.Zip(AsyncDynamicImpulseReceiverGroup).ToBiDictionary();

  static readonly BiDictionary<Type, Type> DynamicImpulseTriggerRecieverMap =
    DynamicImpulseTriggerGroup.Zip(DynamicImpulseReceiverGroup)
    .Concat(AsyncDynamicImpulseTriggerGroup.Zip(AsyncDynamicImpulseReceiverGroup))
    .ToBiDictionary();

  static readonly HashSet<HashSet<Type>> DynamicImpulseGroups = [
    DynamicImpulseTriggerGroup,
    DynamicImpulseReceiverGroup,
    AsyncDynamicImpulseTriggerGroup,
    AsyncDynamicImpulseReceiverGroup,
  ];

  static readonly HashSet<Type> DynamicImpulseGroup = [
    ..DynamicImpulseTriggerGroup,
    ..DynamicImpulseReceiverGroup,
    ..AsyncDynamicImpulseTriggerGroup,
    ..AsyncDynamicImpulseReceiverGroup,
  ];

  static readonly HashSet<Type> AsyncGroup = [
    ..AsyncDynamicImpulseTriggerGroup,
    ..AsyncDynamicImpulseReceiverGroup,
  ];

  internal static IEnumerable<MenuItem> DynamicImpulseGroupItems(ContextualContext context)
  {
    var node = context.hitNode;
    var nodeType = node.NodeType;

    if (DynamicImpulseGroup.Contains(nodeType) || node.NodeType.TryGetGenericTypeDefinition(out nodeType) && DynamicImpulseGroup.Contains(nodeType))
    {
      var tag = FindImpulseTag(node);

      if (context.proxy is { } proxy)
      {
        if (proxy is ProtoFluxInputProxy or ProtoFluxOutputProxy)
        {
          var elementType = proxy.ElementContentType;

          var filledNodes = DynamicImpulseGroups
            .Where(g => g.Contains(nodeType))
            .Select(g => g.Where(t => t.IsGenericType).Select(t => new NodeTypeRecord(t, null, null)))
            .Select(g => GetNodeForType(elementType, [.. g]));

          foreach (var filledType in filledNodes)
          {
            yield return new(filledType, onSpawn: node => OnNodeSpawn(filledType, node, tag), name: NiceName(filledType));
          }
        }
      }
      else
      {
        {
          if (DynamicImpulseTriggerRecieverMap.TryGetEither(nodeType, out var foundType) && foundType.TryMakingGenericTypeFrom(node.NodeType) is { } filledType)
          {
            yield return new(filledType, onSpawn: node => OnNodeSpawn(filledType, node, tag), name: NiceName(filledType));
          }
        }
        {
          if (AsyncDynamicImpulseTriggerMap.TryGetEither(nodeType, out var foundType) && foundType.TryMakingGenericTypeFrom(node.NodeType) is { } filledType)
          {
            yield return new(filledType, onSpawn: node => OnNodeSpawn(filledType, node, tag), name: NiceName(filledType));
          }
        }
        {
          if (AsyncDynamicImpulseReceiverMap.TryGetEither(nodeType, out var foundType) && foundType.TryMakingGenericTypeFrom(node.NodeType) is { } filledType)
          {
            yield return new(filledType, onSpawn: node => OnNodeSpawn(filledType, node, tag), name: NiceName(filledType));
          }
        }
      }
    }

    #region utils
    void OnNodeSpawn(Type inputType, ProtoFluxNode newNode, string? tag)
    {
      if (tag == null) return;

      bool isGeneric = context.NodeType.IsGenericType;
      Type oldBaseType = isGeneric ? context.NodeType.GetGenericTypeDefinition() : context.NodeType;
      Type newBaseType = isGeneric ? inputType.GetGenericTypeDefinition() : inputType;
      if (!oldBaseType.GetNiceTypeName().Contains("Receiver")) return;
      if (!newBaseType.GetNiceTypeName().Contains("Trigger")) return;
      context.callingTool.SpawnNode(ProtoFluxHelper.GetInputNode(typeof(string)), inputNode =>
      {
        inputNode.EnsureVisual();
        var casted = (FrooxEngine.ProtoFlux.Runtimes.Execution.Nodes.ValueObjectInput<string>)inputNode;
        newNode.GetInput(0).Target = casted.GetOutput(0);
        Slot newNodeSlot = newNode.Slot;
        casted.Value.Value = tag;
        casted.Slot.Parent = newNodeSlot.Parent;
        casted.Slot.CopyTransform(newNodeSlot);
        // Dynamic Impulses with data are slightly taller, so increase the vertical offset by that amount
        casted.Slot.LocalPosition += newNodeSlot.Left * 0.18f + newNodeSlot.Up * (newBaseType.GetNiceTypeName().Contains("With") ? 0.03f : 0.015f);
      });
    }

    static string NiceName(Type node)
    {
      var type = node.IsGenericType ? node.GetGenericTypeDefinition() : node;
      var isTrigger = DynamicImpulseTriggerRecieverMap.ContainsFirst(type);
      var asyncName = AsyncGroup.Contains(type) ? "(Async) " : "";
      var kindName = isTrigger ? "Trigger" : "Receiver";
      var dataName = node.IsGenericType ? $" with {node.GenericTypeArguments[0].GetNiceName()}" : "";
      return $"{asyncName}{kindName}{dataName}";
    }

    static string? FindImpulseTag(ProtoFluxNode node)
    {
      var tagField = ((dynamic)node).Tag;
      UniLog.Log(tagField);
      switch (tagField)
      {
        case SyncRef<INodeObjectOutput<string>> tagRef:
          {
            var tagInput = (ObjectInput<string>)((dynamic)node.NodeInstance).Tag;
            var tag = node.Group.EvaluateImmediatelly(tagInput, default);
            UniLog.Log(tag);
            return tag;
          }
        case SyncRef<IGlobalValueProxy<string>> globalRef:
          {
            var tag = globalRef.Target?.Value;
            return tag;
          }
        default:
          return null;
      }
    }
    #endregion
  }
}
