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

[HarmonyPatchCategory("ProtoFluxTool SpatialVariable Creation"), TweakCategory("Adds a context menu item to create SampleSpatialVariable nodes when holding a spatial variable source component with the ProtoFlux tool.")]
[HarmonyPatch(typeof(ProtoFluxTool), nameof(ProtoFluxTool.GenerateMenuItems))]
internal static class SpatialVariableSamplePatch
{
  static readonly Uri Icon_Color_Output = new("resdb:///e0a4e5f5dd6c0fc7e2b089b873455f908a8ede7de4fd37a3430ef71917a543ec.png");

  internal static void Postfix(ProtoFluxTool __instance, InteractionHandler tool, ContextMenu menu)
  {
    var grabbedReference = __instance.GetGrabbedReference();

    if (grabbedReference != null && TypeUtils.MatchInterface(grabbedReference.GetType(), typeof(ISpatialVariable<>), out var matchedType))
    {
      // var variableName = ((ISpatialVariable)grabbedReference!).VariableName;
      var variableType = matchedType!.GenericTypeArguments[0];

      var label = (LocaleString)"Sample";
      var item = menu.AddItem(in label, Icon_Color_Output, RadiantUI_Constants.Hero.ORANGE);
      item.Button.LocalPressed += (button, data) =>
      {
        // todo: valid generic checking
        var variableInput = GetNodeForType(variableType, [
          new NodeTypeRecord(typeof(SampleValueSpatialVariable<>), null, null),
          new NodeTypeRecord(typeof(SampleObjectSpatialVariable<>), null, null),
        ]);

        __instance.SpawnNode(variableInput, n =>
            {
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