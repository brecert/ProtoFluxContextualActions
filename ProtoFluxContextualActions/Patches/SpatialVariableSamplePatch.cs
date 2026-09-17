using System.Diagnostics;
using System.Runtime.CompilerServices;

using Elements.Core;

using FrooxEngine;
using FrooxEngine.ProtoFlux;
using FrooxEngine.ProtoFlux.Runtimes.Execution.Nodes;
using FrooxEngine.ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Variables;

using HarmonyLib;

using ProtoFluxContextualActions.Attributes;
using ProtoFluxContextualActions.Utils;

namespace ProtoFluxContextualActions.Patches;

[PatchGroup("SpatialVariable Creation", "When enabled, adds a context menu item to create SampleSpatialVariable nodes when holding a spatial variable source component with the ProtoFlux Tool.")]
[HarmonyPatch(typeof(ProtoFluxTool), nameof(ProtoFluxTool.GenerateMenuItems))]
internal static class SpatialVariableSamplePatch
{
  static readonly Uri Icon_Color_Output = new("resdb:///e0a4e5f5dd6c0fc7e2b089b873455f908a8ede7de4fd37a3430ef71917a543ec.png");

  internal static void Postfix(ProtoFluxTool __instance, InteractionHandler tool, ContextMenu menu)
  {
    var grabbedReference = __instance.GetGrabbedReference();

    if (grabbedReference != null && TypeUtils.MatchInterface(grabbedReference.GetType(), typeof(ISpatialVariable<>), out var matchedType))
    {
      var spatialVariable = (ISpatialVariable)grabbedReference;
      var variableType = matchedType.GenericTypeArguments[0];
      var variableName = spatialVariable.VariableName;

      var label = (LocaleString)"Sample";
      var item = menu.AddItem(in label, Icon_Color_Output, RadiantUI_Constants.Hero.ORANGE);
      item.Button.LocalPressed += (button, data) =>
      {
        // todo: valid generic checking
        var sampleSpatialVariableType = GetNodeForType(variableType, [
          new NodeTypeRecord(typeof(SampleValueSpatialVariable<>), null, null),
          new NodeTypeRecord(typeof(SampleObjectSpatialVariable<>), null, null),
        ]);

        __instance.SpawnNode(typeof(ValueObjectInput<string>), n =>
        {
          var variableNameNode = (n as ValueObjectInput<string>)!;
          variableNameNode.Value.Value = variableName;
          var inputOutput = variableNameNode.GetOutput(0)!;

          var delta = variableNameNode.Slot.Right * -0.25f;
          variableNameNode.Slot.LocalPosition += delta * variableNameNode.Slot.LocalScale;

          __instance.SpawnNode(sampleSpatialVariableType, sampleSpatialVariableNode =>
          {
            sampleSpatialVariableNode.GetInput(1).Target = inputOutput;
            __instance.ActiveHandler.CloseContextMenu();
          });
        });
      };
    }
  }

  [HarmonyReversePatch]
  [HarmonyPatch(typeof(ProtoFluxHelper), "GetNodeForType")]
  [MethodImpl(MethodImplOptions.NoInlining)]
  internal static Type GetNodeForType(Type type, List<NodeTypeRecord> list) => throw new UnreachableException();
}
