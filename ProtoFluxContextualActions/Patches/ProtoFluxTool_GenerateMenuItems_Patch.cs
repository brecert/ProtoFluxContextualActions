using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ProtoFluxContextualActions.Attributes;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.ProtoFlux;
using FrooxEngine.ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Variables;
using HarmonyLib;
using ProtoFluxContextualActions.Utils;

namespace ProtoFluxContextualActions.Patches;

[HarmonyPatchCategory("ProtoFluxTool DynamicVariableInput Creation"), TweakCategory("Adds a context menu item to create DynamicVariableInputs when holding a dynamic variable component with the ProtoFlux tool.")]
[HarmonyPatch(typeof(ProtoFluxTool), nameof(ProtoFluxTool.GenerateMenuItems))]
internal static class ProtoFluxTool_GenerateMenuItems_Patch
{
  static readonly Uri Icon_Color_Output = new("resdb:///e0a4e5f5dd6c0fc7e2b089b873455f908a8ede7de4fd37a3430ef71917a543ec.png");

  internal static void Postfix(ProtoFluxTool __instance, InteractionHandler tool, ContextMenu menu)
  {
    var grabbedReference = __instance.GetGrabbedReference();

    if (grabbedReference != null && TypeUtils.MatchInterface(grabbedReference.GetType(), typeof(IDynamicVariable<>), out var matchedType))
    {
      var variableName = ((IDynamicVariable)grabbedReference!).VariableName;
      var variableType = matchedType!.GenericTypeArguments[0];

      var label = (LocaleString)"Input";
      var item = menu.AddItem(in label, Icon_Color_Output, RadiantUI_Constants.Hero.ORANGE);
      item.Button.LocalPressed += (button, data) =>
      {
        var variableInput = GetNodeForType(variableType, [
          new NodeTypeRecord(typeof(DynamicVariableValueInput<>), null, null),
          new NodeTypeRecord(typeof(DynamicVariableObjectInput<>), null, null),
        ]);

        __instance.SpawnNode(variableInput, n =>
            {
              var globalValue = n.Slot.AttachComponent<GlobalValue<string>>();
              globalValue.SetValue(variableName);
              n.GetGlobalRef(0).Target = globalValue;
              __instance.ActiveHandler.CloseContextMenu();
            });
      };
    }
  }

  [HarmonyReversePatch]
  [HarmonyPatch(typeof(ProtoFluxHelper), "GetNodeForType")]
  [MethodImpl(MethodImplOptions.NoInlining)]
  internal static Type GetNodeForType(Type type, List<NodeTypeRecord> list) => throw new NotImplementedException();
}