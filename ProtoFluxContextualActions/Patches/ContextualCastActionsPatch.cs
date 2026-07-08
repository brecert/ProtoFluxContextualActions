using System;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.ProtoFlux;

using ProtoFluxContextualActions.Attributes;
using HarmonyLib;
using System.Linq;
using ProtoFluxContextualActions.Utils;
using ProtoFlux.Runtimes.Execution.Nodes.Strings;
using ProtoFlux.Runtimes.Execution.Nodes.ParsingFormatting;
using ProtoFlux.Runtimes.Execution.Nodes.Casts;
using ProtoFlux.Core;

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
      ContextMenu menu = await __instance.LocalUser.OpenContextMenu(__instance, __instance.ActiveHandler.Slot);
      menu.AddMenuItem("Tools.ProtoFlux.ExplicitCast".AsLocaleKey(), colorX.Orange, () =>
      {
        node.TryConnectInput(input, output, allowExplicitCast: true, undoable: true);
        menu.Close();
      });
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
      var nodeBinding = ProtoFluxHelper.GetBindingForNode(zeroOneNode);
      menu.AddMenuItem("0/1", colorX.Cyan, () =>
      {
        tool.SpawnNode(nodeBinding, n =>
        {
          n.EnsureElementsInDynamicLists();
          n.GetInput(0).Target = output;
          input.Target = n.GetOutput(0);
          menu.Close();
        });
      });
    }

    if (outputType == typeof(string) && inputType == typeof(bool))
    {
      var nodeBinding = ProtoFluxHelper.GetBindingForNode(typeof(IsStringEmpty));
      menu.AddMenuItem("String Empty", colorX.Cyan, () =>
      {
        tool.SpawnNode(nodeBinding, n =>
        {
          n.EnsureElementsInDynamicLists();
          n.GetInput(0).Target = output;
          input.Target = n.GetOutput(0);
          menu.Close();
        });
      });
    }

    if (outputType == typeof(string) && psuedoGenericTypes.Parse.Any(n => n.Types.First() == inputType))
    {
      Type parseNode = psuedoGenericTypes.Parse.First(n => n.Types.First() == inputType).Node;
      var nodeBinding = ProtoFluxHelper.GetBindingForNode(parseNode);
      menu.AddMenuItem("Parse", colorX.Cyan, () =>
      {
        tool.SpawnNode(nodeBinding, n =>
        {
          n.EnsureElementsInDynamicLists();
          n.GetInput(0).Target = output;
          input.Target = n.GetOutput(0);
          menu.Close();
        });
      });
    }

    if (inputType == typeof(string))
    {
      if (psuedoGenericTypes.ObjToString.Any(n => n.Types.First() == outputType))
      {
        Type toStringNode = psuedoGenericTypes.ObjToString.First(n => n.Types.First() == outputType).Node;
        var nodeBinding = ProtoFluxHelper.GetBindingForNode(toStringNode);
        menu.AddMenuItem("To String", colorX.Cyan, () =>
        {
          tool.SpawnNode(nodeBinding, n =>
          {
            n.EnsureElementsInDynamicLists();
            n.GetInput(0).Target = output;
            input.Target = n.GetOutput(0);
            menu.Close();
          });
        });
      }
      else
      {
        // type has no direct cast. use T->object->ToString


        Type? castNode = null;
        if (outputType.IsUnmanaged())
        {
          castNode = typeof(ValueToObjectCast<>).MakeGenericType(outputType);
        }
        else if (ReflectionHelper.IsNullable(outputType))
        {
          castNode = typeof(NullableToObjectCast<>).MakeGenericType(Nullable.GetUnderlyingType(outputType) ?? outputType);
        }
        else
        {
          castNode = typeof(ObjectCast<,>).MakeGenericType(outputType, typeof(object));
        }
        if (castNode != null)
        {
          Type toStringNode = typeof(ToString_object);
          var toStringBinding = ProtoFluxHelper.GetBindingForNode(toStringNode);
          var castBinding = ProtoFluxHelper.GetBindingForNode(castNode);
          menu.AddMenuItem("To String", colorX.Cyan, () =>
          {
            tool.SpawnNode(castBinding, cast =>
            {
              cast.EnsureElementsInDynamicLists();
              cast.GetInput(0).Target = output;
              tool.SpawnNode(toStringBinding, toString =>
              {
                toString.EnsureElementsInDynamicLists();
                toString.GetInput(0).Target = cast.GetOutput(0);
                input.Target = toString.GetOutput(0);
              });
              menu.Close();
            });
          });
        }
      }
    }

    // UniLog.Warning($"here is literally every fucking cast node\n\n\n");
    // foreach (var v in psuedoGenericTypes.Cast)
    // {
    //   UniLog.Warning($"Node: {v.Node}, types: {v.Types.ToArray()}");
    // }
    // UniLog.Warning($"yeah\n\n\n");

    if (psuedoGenericTypes.Cast.Any(n => n.Types.SequenceEqual([outputType, inputType])))
    {
      Type valueCastNode = psuedoGenericTypes.Cast.First(n => n.Types.SequenceEqual([outputType, inputType])).Node;
      var nodeBinding = ProtoFluxHelper.GetBindingForNode(valueCastNode);
      menu.AddMenuItem("Value Cast", colorX.Cyan, () =>
      {
        tool.SpawnNode(nodeBinding, n =>
        {
          n.EnsureElementsInDynamicLists();
          n.GetInput(0).Target = output;
          input.Target = n.GetOutput(0);
          menu.Close();
        });
      });
    }

    if (inputType == typeof(int))
    {
      if (psuedoGenericTypes.RoundToInt.Any(t => t.Types.First() == outputType))
      {
        Type valueCastNode = psuedoGenericTypes.RoundToInt.First(t => t.Types.First() == outputType).Node;
        var nodeBinding = ProtoFluxHelper.GetBindingForNode(valueCastNode);
        menu.AddMenuItem("Round", colorX.Cyan, () =>
        {
          tool.SpawnNode(nodeBinding, n =>
          {
            n.EnsureElementsInDynamicLists();
            n.GetInput(0).Target = output;
            input.Target = n.GetOutput(0);
            menu.Close();
          });
        });
      }
      if (psuedoGenericTypes.FloorToInt.Any(t => t.Types.First() == outputType))
      {
        Type valueCastNode = psuedoGenericTypes.FloorToInt.First(t => t.Types.First() == outputType).Node;
        var nodeBinding = ProtoFluxHelper.GetBindingForNode(valueCastNode);
        menu.AddMenuItem("Floor To Int", colorX.Cyan, () =>
        {
          tool.SpawnNode(nodeBinding, n =>
          {
            n.EnsureElementsInDynamicLists();
            n.GetInput(0).Target = output;
            input.Target = n.GetOutput(0);
            menu.Close();
          });
        });
      }
      if (psuedoGenericTypes.CeilToInt.Any(t => t.Types.First() == outputType))
      {
        Type valueCastNode = psuedoGenericTypes.CeilToInt.First(t => t.Types.First() == outputType).Node;
        var nodeBinding = ProtoFluxHelper.GetBindingForNode(valueCastNode);
        menu.AddMenuItem("Ceil To Int", colorX.Cyan, () =>
        {
          tool.SpawnNode(nodeBinding, n =>
          {
            n.EnsureElementsInDynamicLists();
            n.GetInput(0).Target = output;
            input.Target = n.GetOutput(0);
            menu.Close();
          });
        });
      }
    }
  }
}