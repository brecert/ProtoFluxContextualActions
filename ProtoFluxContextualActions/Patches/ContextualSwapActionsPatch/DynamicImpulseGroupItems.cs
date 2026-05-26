using Elements.Core;
using FrooxEngine;
using FrooxEngine.ProtoFlux;
using HarmonyLib;
using ProtoFlux.Core;
using ProtoFlux.Runtimes.Execution.Nodes.Actions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> DynamicImpulseGroup = [
    typeof(DynamicImpulseReceiver),
    typeof(DynamicImpulseTrigger),
    typeof(DynamicImpulseReceiverWithValue<>),
    typeof(DynamicImpulseReceiverWithObject<>),
    typeof(DynamicImpulseTriggerWithValue<>),
    typeof(DynamicImpulseTriggerWithObject<>),

    typeof(AsyncDynamicImpulseReceiver),
    typeof(AsyncDynamicImpulseTrigger),
    typeof(AsyncDynamicImpulseReceiverWithValue<>),
    typeof(AsyncDynamicImpulseReceiverWithObject<>),
    typeof(AsyncDynamicImpulseTriggerWithValue<>),
    typeof(AsyncDynamicImpulseTriggerWithObject<>),
  ];


  internal static IEnumerable<MenuItem> DynamicImpulseGroupItems(ContextualContext context)
  {
    if (DynamicImpulseGroup.Any(t => context.NodeType.IsGenericType ? t == context.NodeType.GetGenericTypeDefinition() : t == context.NodeType))
    {
      bool isGeneric = context.NodeType.IsGenericType;
      Type baseType = isGeneric ? context.NodeType.GetGenericTypeDefinition() : context.NodeType;
      Type innerType = isGeneric ? context.NodeType.GenericTypeArguments.First() : context.NodeType;

      bool IsTrigger = innerType.GetNiceTypeName().Contains("DynamicImpulseTrigger");
      bool IsAsync = innerType.GetNiceTypeName().StartsWith("Async");

      Type? dynTrigData = null, dynRecData = null, asyncDynTrigData = null, asyncDynRecData = null;

      Type? target = null;
      bool hasProxyHeld = false;
      bool hasDynData = false;

      string? receiverTag = null;

      if (context.proxy is ProtoFluxInputProxy)
      {
        ProtoFluxInputProxy inputType = (ProtoFluxInputProxy)context.proxy;
        Type targetType = inputType.InputType;
        target = targetType;
        hasProxyHeld = true;
      }
      if (context.proxy is ProtoFluxOutputProxy)
      {
        ProtoFluxOutputProxy outputType = (ProtoFluxOutputProxy)context.proxy;
        Type targetType = outputType.OutputType;
        target = targetType;
        hasProxyHeld = true;
      }
      if (context.NodeType.IsGenericType && target == null)
      {
        var opCount = context.NodeType.GenericTypeArguments.Length;
        var opType = context.NodeType.GenericTypeArguments[opCount - 1];
        target = opType;
      }

      if (baseType.GetNiceTypeName().Contains("Receiver"))
      {
        var trav = Traverse.Create(context.hitNode);
        var field = trav.Field("Tag");
        UniLog.Warning($"--- possibly has field");
        if (field.FieldExists())
        {
          UniLog.Warning($"--- Field Exists!!!");
          receiverTag = field.GetValue<SyncRef<IGlobalValueProxy<string>>>().Target.Value;
          UniLog.Warning($"--- Found tag: '{receiverTag}'");
				}
      }


      if (target != null)
      {
        var DynTrigger = GetNodeForType(target, [
          new NodeTypeRecord(typeof(DynamicImpulseTriggerWithValue<>), null, null),
          new NodeTypeRecord(typeof(DynamicImpulseTriggerWithObject<>), null, null),
        ]);
        dynTrigData = DynTrigger;

        var AsyncDynTrigger = GetNodeForType(target, [
          new NodeTypeRecord(typeof(AsyncDynamicImpulseTriggerWithValue<>), null, null),
          new NodeTypeRecord(typeof(AsyncDynamicImpulseTriggerWithObject<>), null, null),
        ]);
        asyncDynTrigData = AsyncDynTrigger;

        var DynReceiver = GetNodeForType(target, [
          new NodeTypeRecord(typeof(DynamicImpulseReceiverWithValue<>), null, null),
          new NodeTypeRecord(typeof(DynamicImpulseReceiverWithObject<>), null, null),
        ]);
        dynRecData = DynReceiver;

        var AsyncDynReceiver = GetNodeForType(target, [
          new NodeTypeRecord(typeof(AsyncDynamicImpulseReceiverWithValue<>), null, null),
          new NodeTypeRecord(typeof(AsyncDynamicImpulseReceiverWithObject<>), null, null),
        ]);
        asyncDynRecData = AsyncDynReceiver;

        hasDynData = true;
      }

      Dictionary<bool3, Type?> keyedImpulses = new()
      {
        { new(false, true, false), typeof(DynamicImpulseTrigger) },
        { new(false, false, false), typeof(DynamicImpulseReceiver) },
        { new(true, true, false), typeof(AsyncDynamicImpulseTrigger) },
        { new(true, false, false), typeof(AsyncDynamicImpulseReceiver) },

        { new(false, true, true), dynTrigData },
        { new(false, false, true), dynRecData },
        { new(true, true, true), asyncDynTrigData },
        { new(true, false, true), asyncDynRecData },
      };

      List<Type?> sortedImpulses = keyedImpulses
        .Where(kv => !kv.Key.x)
        .OrderBy(kv => hasProxyHeld ? !kv.Key.z : false)
        .OrderBy(kv => IsTrigger ? !kv.Key.y : false)
        .OrderBy(kv => hasDynData ? !kv.Key.z : false)
        .Select(kv => kv.Value)
        .ToList();

      List<Type?> sortedAsyncImpulses = keyedImpulses
        .Where(kv => kv.Key.x)
        .OrderBy(kv => hasProxyHeld ? !kv.Key.z : false)
        .OrderBy(kv => IsTrigger ? !kv.Key.y : false)
        .OrderBy(kv => hasDynData ? !kv.Key.z : false)
        .Select(kv => kv.Value)
        .ToList();

      string? NodeNameSelector(Type input)
      {
        string constructedName = "";
        bool isGeneric = input.IsGenericType;
        Type baseType = isGeneric ? input.GetGenericTypeDefinition() : input;
        Type innerType = isGeneric ? input.GenericTypeArguments.First() : input;
        if (isGeneric)
        {
          constructedName += innerType.GetNiceName();
        }
        string niceTypeName = baseType.GetNiceTypeName().ToLower();
        if (niceTypeName.Contains("trigger")) constructedName += " Trigger";
        else constructedName += " Receiver";
        if (niceTypeName.Contains("async")) constructedName += " (Async)";
        return constructedName;
      }

      void OnNodeSpawn(Type inputType, ProtoFluxNode newNode)
      {
        UniLog.Warning($"--- On Node Spawn: checking tag '{receiverTag}'");
        if (receiverTag == null) return;
        UniLog.Warning($"--- On Node Spawn: tag not null!");

        bool isGeneric = context.NodeType.IsGenericType;
        Type baseType = isGeneric ? context.NodeType.GetGenericTypeDefinition() : context.NodeType;
        UniLog.Warning($"--- On Node Spawn: base type is '{baseType.GetNiceTypeName()}'");
        if (!baseType.GetNiceTypeName().Contains("Receiver")) return;
        UniLog.Warning($"--- On Node Spawn: earlier node is a receiver!");
        UniLog.Warning($"--- On Node Spawn: spawning node.");
        context.callingTool.SpawnNode(ProtoFluxHelper.GetInputNode(typeof(string)), inputNode =>
        {
          UniLog.Warning($"--- Input node has spawned!");
          inputNode.EnsureVisual();
          var casted = (FrooxEngine.ProtoFlux.Runtimes.Execution.Nodes.ValueObjectInput<string>)inputNode;
          newNode.GetInput(0).Target = casted.GetOutput(0);
          UniLog.Warning($"--- set input/output");
          Slot newNodeSlot = newNode.Slot;
          casted.Value.Value = receiverTag;
          casted.Slot.Parent = newNodeSlot.Parent;
          casted.Slot.CopyTransform(newNodeSlot);
          casted.Slot.LocalPosition += newNodeSlot.Left * 0.18f + newNodeSlot.Up * (baseType.GetNiceTypeName().Contains("With") ? 0.03f : 0.015f);
          UniLog.Warning($"--- set transforms");
        });
        UniLog.Warning($"--- Main Execution Finished.");
      }

      foreach (var imp in IsAsync ? sortedAsyncImpulses : sortedImpulses)
      {
        if (imp != null) yield return new(imp, name: NodeNameSelector(imp), onSpawn: (node) => OnNodeSpawn(imp, node));
      }
      foreach (var imp in IsAsync ? sortedImpulses : sortedAsyncImpulses)
      {
        if (imp != null) yield return new(imp, name: NodeNameSelector(imp), onSpawn: (node) => OnNodeSpawn(imp, node));
      }
    }
  }
}