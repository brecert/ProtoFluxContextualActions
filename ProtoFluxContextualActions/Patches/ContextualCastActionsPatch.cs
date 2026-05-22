using System;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.ProtoFlux;

using ProtoFluxContextualActions.Attributes;
using HarmonyLib;
using System.Linq;
using ProtoFluxContextualActions.Utils;
using ProtoFlux.Runtimes.Execution.Nodes.Strings;

[HarmonyPatchCategory("ProtoFluxTool Contextual Cast Actions"), TweakCategory("Adds 'Contextual Cast Actions' to the ProtoFlux Tool. Casting certain types to others may suggest extra actions, rather than only allowing explicit casts.")]
[HarmonyPatch(typeof(ProtoFluxTool), "TryConnect", argumentTypes: [typeof(ProtoFluxNode), typeof(ISyncRef), typeof(INodeOutput)])]
internal static class ContextualSelectionActionsPatch
{
  internal static bool Prefix(ProtoFluxTool __instance, ProtoFluxNode node, ISyncRef input, INodeOutput output)
  {
    if (node.TryConnectInput(input, output, allowExplicitCast: false, undoable: true))
    {
      return false;
    }

    __instance.StartTask(async delegate
    {
      ContextMenu menu = await __instance.LocalUser.OpenContextMenu(__instance, __instance.ActiveHandler?.Slot);
      ContextMenuItem contextMenuItem = menu.AddItem("Tools.ProtoFlux.ExplicitCast".AsLocaleKey(), (Uri)null, new colorX?(colorX.Orange));
      contextMenuItem.Button.LocalPressed += delegate
      {
        node.TryConnectInput(input, output, allowExplicitCast: true, undoable: true);
        menu.Close();
      };
      TryGetExtraCasts(__instance, node, input, output, menu);
      menu.AddItem("General.Cancel".AsLocaleKey(), (Uri?)null, new colorX?(colorX.White), (ButtonEventHandler)menu.CloseMenu);
    });
    return false;
  }

  internal static void TryGetExtraCasts(ProtoFluxTool tool, ProtoFluxNode node, ISyncRef input, INodeOutput output, ContextMenu menu)
  {
    var world = node.World;
    var psuedoGenericTypes = world.GetPsuedoGenericTypesForWorld();

    Type outputType = output.MappedOutput.OutputType;
    Type baseInputType = input.TargetType;
    Type inputType = baseInputType.IsGenericType ? baseInputType.GenericTypeArguments.Last() : baseInputType;

    if (outputType == typeof(bool) && psuedoGenericTypes.ZeroOne.Any(n => n.Types.First() == inputType))
    {
      Type zeroOneNode = psuedoGenericTypes.ZeroOne.First(n => n.Types.First() == inputType).Node;
      ContextMenuItem zeroOneItem = menu.AddItem("0/1", (Uri)null, new colorX?(colorX.Cyan));
      var nodeBinding = ProtoFluxHelper.GetBindingForNode(zeroOneNode);
      zeroOneItem.Button.LocalPressed += delegate
      {
        tool.SpawnNode(nodeBinding, n =>
        {
          n.EnsureElementsInDynamicLists();
          n.GetInput(0).Target = output;
          input.Target = n.GetOutput(0);
          menu.Close();
        });
      };
    }

    if (outputType == typeof(string) && inputType == typeof(bool))
    {
      ContextMenuItem zeroOneItem = menu.AddItem("String Empty", (Uri)null, new colorX?(colorX.Cyan));
      var nodeBinding = ProtoFluxHelper.GetBindingForNode(typeof(IsStringEmpty));
      zeroOneItem.Button.LocalPressed += delegate
      {
        tool.SpawnNode(nodeBinding, n =>
        {
          n.EnsureElementsInDynamicLists();
          n.GetInput(0).Target = output;
          input.Target = n.GetOutput(0);
          menu.Close();
        });
      };
    }

    if (outputType == typeof(string) && psuedoGenericTypes.Parse.Any(n => n.Types.First() == inputType))
    {
      Type parseNode = psuedoGenericTypes.Parse.First(n => n.Types.First() == inputType).Node;
      ContextMenuItem parseItem = menu.AddItem("Parse", (Uri)null, new colorX?(colorX.Cyan));
      var nodeBinding = ProtoFluxHelper.GetBindingForNode(parseNode);
      parseItem.Button.LocalPressed += delegate
      {
        tool.SpawnNode(nodeBinding, n =>
        {
          n.EnsureElementsInDynamicLists();
          n.GetInput(0).Target = output;
          input.Target = n.GetOutput(0);
          menu.Close();
        });
      };
    }

    if (inputType == typeof(string) && psuedoGenericTypes.ObjToString.Any(n => n.Types.First() == outputType))
    {
      Type parseNode = psuedoGenericTypes.ObjToString.First(n => n.Types.First() == outputType).Node;
      ContextMenuItem parseItem = menu.AddItem("To String", (Uri)null, new colorX?(colorX.Cyan));
      var nodeBinding = ProtoFluxHelper.GetBindingForNode(parseNode);
      parseItem.Button.LocalPressed += delegate
      {
        tool.SpawnNode(nodeBinding, n =>
        {
          n.EnsureElementsInDynamicLists();
          n.GetInput(0).Target = output;
          input.Target = n.GetOutput(0);
          menu.Close();
        });
      };
    }
  }
}