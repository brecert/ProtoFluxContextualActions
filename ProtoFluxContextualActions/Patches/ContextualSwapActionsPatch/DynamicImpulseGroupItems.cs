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
        if (field.FieldExists())
        {
          var globalField = field.GetValue<SyncRef<IGlobalValueProxy<string>>>().Target;
          if (globalField != null) receiverTag = globalField.Value;
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
        .OrderBy(kv => !kv.Key.x == IsAsync)
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
        if (receiverTag == null) return;

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
          casted.Value.Value = receiverTag;
          casted.Slot.Parent = newNodeSlot.Parent;
          casted.Slot.CopyTransform(newNodeSlot);
          // Dynamic Impulses with data are slightly taller, so increase the vertical offset by that amount
          casted.Slot.LocalPosition += newNodeSlot.Left * 0.18f + newNodeSlot.Up * (newBaseType.GetNiceTypeName().Contains("With") ? 0.03f : 0.015f);
        });
      }

      foreach (var imp in sortedImpulses)
      {
        if (imp != null) yield return new(imp, name: NodeNameSelector(imp), onSpawn: (node) => OnNodeSpawn(imp, node));
      }
    }
  }
}