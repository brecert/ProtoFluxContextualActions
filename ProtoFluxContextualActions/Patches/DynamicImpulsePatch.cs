using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ProtoFluxContextualActions.Attributes;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.ProtoFlux;
using FrooxEngine.ProtoFlux.Runtimes.Execution.Nodes;
using FrooxEngine.ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Variables;
using HarmonyLib;
using ProtoFluxContextualActions.Utils;
using FrooxEngine.ProtoFlux.Runtimes.Execution.Nodes.Actions;

namespace ProtoFluxContextualActions.Patches;

[HarmonyPatchCategory("ProtoFluxTool DynamicImpulse Creation"), TweakCategory("Adds a context menu item to create DynamicImpulse when holding a button dynamic impulse component with the ProtoFlux tool.")]
[HarmonyPatch(typeof(ProtoFluxTool), nameof(ProtoFluxTool.GenerateMenuItems))]
internal static class DynamicImpulsePatch
{
  static readonly Uri Icon_Color_Output = new("resdb:///e0a4e5f5dd6c0fc7e2b089b873455f908a8ede7de4fd37a3430ef71917a543ec.png");

  internal static void Postfix(ProtoFluxTool __instance, InteractionHandler tool, ContextMenu menu)
  {
    var grabbedReference = __instance.GetGrabbedReference();

    if (grabbedReference == null) return;

    var componentType = grabbedReference.GetType();

    if (grabbedReference is ButtonDynamicImpulseTrigger buttonTrigger)
    {
      void CreateForTag(string name, string tag)
      {
        if (!string.IsNullOrEmpty(tag))
        {
          menu.AddMenuItem(name, RadiantUI_Constants.Hero.ORANGE, () =>
          {
            __instance.SpawnNode(typeof(DynamicImpulseReceiver), n =>
              {
                var globalValue = n.Slot.AttachComponent<GlobalValue<string>>();
                globalValue.SetValue(tag);
                n.GetGlobalRef(0).Target = globalValue;
                __instance.ActiveHandler.CloseContextMenu();
              });
          }, Icon_Color_Output);
        }
      }

      CreateForTag("Pressed", buttonTrigger.PressedTag.Value);
      CreateForTag("Pressing", buttonTrigger.PressingTag.Value);
      CreateForTag("Released", buttonTrigger.ReleasedTag.Value);
      CreateForTag("Hover Enter", buttonTrigger.HoverEnterTag.Value);
      CreateForTag("Hover Stay", buttonTrigger.HoverStayTag.Value);
      CreateForTag("Hover Leave", buttonTrigger.HoverLeaveTag.Value);
    }

    if (!componentType.IsGenericType) return;

    var baseType = componentType.GetGenericTypeDefinition();
    var innerType = componentType.GenericTypeArguments[0];

    if (baseType == typeof(ButtonDynamicImpulseTriggerWithValue<>))
    {
      void CreateForTag(string name, string tag)
      {
        if (!string.IsNullOrEmpty(tag))
        {
          menu.AddMenuItem(name, RadiantUI_Constants.Hero.ORANGE, () =>
          {
            var variableRead = GetNodeForType(innerType, [
              new NodeTypeRecord(typeof(DynamicImpulseReceiverWithValue<>), null, null),
              new NodeTypeRecord(typeof(DynamicImpulseReceiverWithObject<>), null, null),
            ]);
            __instance.SpawnNode(variableRead, n =>
              {
                var globalValue = n.Slot.AttachComponent<GlobalValue<string>>();
                globalValue.SetValue(tag);
                n.GetGlobalRef(0).Target = globalValue;
                __instance.ActiveHandler.CloseContextMenu();
              });
          }, Icon_Color_Output);
        }
      }

      var traverse = Traverse.Create(grabbedReference);

      CreateForTag("Pressed", traverse.Field("PressedData").Field<Sync<string>>("Tag").Value);
      CreateForTag("Pressing", traverse.Field("PressingData").Field<Sync<string>>("Tag").Value);
      CreateForTag("Released", traverse.Field("ReleasedData").Field<Sync<string>>("Tag").Value);
      CreateForTag("Hover Enter", traverse.Field("HoverEnterData").Field<Sync<string>>("Tag").Value);
      CreateForTag("Hover Stay", traverse.Field("HoverStayData").Field<Sync<string>>("Tag").Value);
      CreateForTag("Hover Leave", traverse.Field("HoverLeaveData").Field<Sync<string>>("Tag").Value);
    }

    if (baseType == typeof(ButtonDynamicImpulseTriggerWithReference<>))
    {
      void CreateForTag(string name, string tag)
      {
        if (!string.IsNullOrEmpty(tag))
        {
          menu.AddMenuItem(name, RadiantUI_Constants.Hero.ORANGE, () =>
          {
            __instance.SpawnNode(typeof(DynamicImpulseReceiverWithObject<>).MakeGenericType(innerType), n =>
              {
                var globalValue = n.Slot.AttachComponent<GlobalValue<string>>();
                globalValue.SetValue(tag);
                n.GetGlobalRef(0).Target = globalValue;
                __instance.ActiveHandler.CloseContextMenu();
              });
          }, Icon_Color_Output);
        }
      }

      var traverse = Traverse.Create(grabbedReference);

      CreateForTag("Pressed", traverse.Field("PressedData").Field<Sync<string>>("Tag").Value);
      CreateForTag("Pressing", traverse.Field("PressingData").Field<Sync<string>>("Tag").Value);
      CreateForTag("Released", traverse.Field("ReleasedData").Field<Sync<string>>("Tag").Value);
      CreateForTag("Hover Enter", traverse.Field("HoverEnterData").Field<Sync<string>>("Tag").Value);
      CreateForTag("Hover Stay", traverse.Field("HoverStayData").Field<Sync<string>>("Tag").Value);
      CreateForTag("Hover Leave", traverse.Field("HoverLeaveData").Field<Sync<string>>("Tag").Value);
    }
  }

  [HarmonyReversePatch]
  [HarmonyPatch(typeof(ProtoFluxHelper), "GetNodeForType")]
  [MethodImpl(MethodImplOptions.NoInlining)]
  internal static Type GetNodeForType(Type type, List<NodeTypeRecord> list) => throw new NotImplementedException();
}