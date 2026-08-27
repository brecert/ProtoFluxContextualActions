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

namespace ProtoFluxContextualActions.Patches;

[HarmonyPatchCategory("ProtoFluxTool DynamicVariableInput Creation"), TweakCategory("Adds a context menu item to create DynamicVariableInputs when holding a dynamic variable component with the ProtoFlux tool.")]
[HarmonyPatch(typeof(ProtoFluxTool), nameof(ProtoFluxTool.GenerateMenuItems))]
internal static class DynamicVariableOutputPatch
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
      var inputItem = menu.AddItem(in label, Icon_Color_Output, RadiantUI_Constants.Hero.ORANGE);
      inputItem.Button.LocalPressed += (button, data) =>
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

      label = "Read";
      var readItem = menu.AddItem(in label, Icon_Color_Output, RadiantUI_Constants.Hero.CYAN);
      readItem.Button.LocalPressed += (button, data) =>
      {
        var variableRead = GetNodeForType(variableType, [
          new NodeTypeRecord(typeof(ReadDynamicValueVariable<>), null, null),
          new NodeTypeRecord(typeof(ReadDynamicObjectVariable<>), null, null),
        ]);
        var variableNameInput = typeof(ValueObjectInput<string>);

        INodeOutput? inputOutput = null;
        __instance.SpawnNode(variableNameInput, n =>
        {
          ((ValueObjectInput<string>)n).Value.Value = variableName;
          inputOutput = n.GetOutput(0);
          float3 upDir = n.Slot.Up;
          float3 rightDir = n.Slot.Right;
          float3 scaling = n.Slot.LocalScale;

          float3 delta = (upDir * -0.015f) + (rightDir * -0.25f);
          n.Slot.LocalPosition += delta * scaling;
        });
        __instance.SpawnNode(variableRead, n =>
        {
          n.GetInput(1).Target = inputOutput;
          __instance.ActiveHandler.CloseContextMenu();
        });
      };

      label = "Write";
      var writeItem = menu.AddItem(in label, Icon_Color_Output, RadiantUI_Constants.Hero.CYAN);
      writeItem.Button.LocalPressed += (button, data) =>
      {
        var variableRead = GetNodeForType(variableType, [
            new NodeTypeRecord(typeof(WriteDynamicValueVariable<>), null, null),
                    new NodeTypeRecord(typeof(WriteDynamicObjectVariable<>), null, null),
                ]);
        var variableNameInput = typeof(ValueObjectInput<string>);

        INodeOutput? inputOutput = null;
        __instance.SpawnNode(variableNameInput, n =>
        {
          ((ValueObjectInput<string>)n).Value.Value = variableName;
          inputOutput = n.GetOutput(0);
          float3 upDir = n.Slot.Up;
          float3 rightDir = n.Slot.Right;
          float3 scaling = n.Slot.LocalScale;

          float3 delta = (upDir * -0.015f) + (rightDir * -0.25f);
          n.Slot.LocalPosition += delta * scaling;
        });
        __instance.SpawnNode(variableRead, n =>
        {
          n.GetInput(1).Target = inputOutput;
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