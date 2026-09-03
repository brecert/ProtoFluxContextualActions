using System;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.ProtoFlux;

using ProtoFluxContextualActions.Attributes;
using HarmonyLib;
using System.Linq;
using ProtoFluxContextualActions.Utils;
using ProtoFlux.Runtimes.Execution.Nodes.ParsingFormatting;
using ProtoFlux.Runtimes.Execution.Nodes.Casts;
using ProtoFlux.Core;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Reflection;
using ProtoFluxContextualActions.Utils.ProtoFlux;
using FrooxEngine.UIX;
using Elements.Quantity;

[HarmonyPatchCategory("ProtoFluxTool Contextual Cast Actions"), TweakCategory("Adds 'Contextual Cast Actions' to the ProtoFlux Tool. Casting certain types to others may suggest extra actions, rather than only allowing explicit casts.")]
[HarmonyPatch(typeof(ProtoFluxTool), "TryConnect", argumentTypes: [typeof(ProtoFluxNode), typeof(ISyncRef), typeof(INodeOutput)])]
internal static class ContextualSelectionActionsPatch
{
  internal readonly struct MenuItem(Type node, string? name = null, Action<ProtoFluxNode>? onSpawn = null)
  {
    internal readonly Type node = node;
    internal readonly string? name = name;
    internal readonly string DisplayName => name ?? NodeMetadataHelper.GetMetadata(node).Name ?? node.GetNiceTypeName();
    internal readonly Action<ProtoFluxNode>? onSpawn = onSpawn;
  }

  internal static bool Prefix(ProtoFluxTool __instance, ProtoFluxNode node, ISyncRef input, INodeOutput output)
  {
    var tool = __instance;

    if (node.TryConnectInput(input, output, allowExplicitCast: false, undoable: true))
    {
      return false;
    }

    __instance.StartTask(async delegate
    {
      var menu = await __instance.LocalUser.OpenContextMenu(__instance, __instance.ActiveHandler.Slot);
      menu.AddMenuItem("Tools.ProtoFlux.ExplicitCast".AsLocaleKey(), colorX.Orange, () =>
      {
        node.TryConnectInput(input, output, allowExplicitCast: true, undoable: true);
        menu.Close();
      });


      foreach (var castItem in TryGetExtraCasts(__instance, node, input, output))
      {
        menu.AddMenuItem(
          name: castItem.DisplayName,
          icon: (Uri?)null,
          color: new colorX?(colorX.White),
          onClicked: () => SpawnNode(tool, node, castItem, (castNode) =>
          {
            if (castItem.onSpawn is { } onSpawn)
            {
              onSpawn(castNode);
            }
            else
            {
              var castInput = castNode.GetInput(0); // todo: specify...
              var castOutput = castNode.GetOutput(0); // todo: specify...
              castNode.TryConnectInput(castInput, output, allowExplicitCast: true, undoable: true);
              node.TryConnectInput(input, castOutput, allowExplicitCast: true, undoable: true);
            }
          })
        );
      }

      menu.AddItem("General.Cancel".AsLocaleKey(), (Uri?)null, new colorX?(colorX.White), menu.CloseMenu);
    });
    return false;
  }

  private static void SpawnNode(ProtoFluxTool tool, ProtoFluxNode toNode, MenuItem item, Action<ProtoFluxNode> setup)
  {
    var nodeBinding = ProtoFluxHelper.GetBindingForNode(item.node);
    var node = tool.SpawnNode(nodeBinding, n =>
    {
      n.EnsureElementsInDynamicLists();
      setup(n);
      tool.LocalUser.CloseContextMenu(tool);
      CleanupDraggedWire(tool);
    });
    // todo: make not hardcoded?
    // todo: handle casts?
    node.Slot.GlobalPosition = toNode.Slot.LocalPointToGlobal(
      float3.Left * ProtoFluxNodeVisual.DEFAULT_WIDTH * ProtoFluxNodeVisual.DEFAULT_SCALE * 1.75f
    );
  }

  internal static IEnumerable<MenuItem> TryGetExtraCasts(ProtoFluxTool tool, ProtoFluxNode node, ISyncRef input, INodeOutput output)
  {
    var world = node.World;
    var psuedoGenericTypes = world.GetPsuedoGenericTypesForWorld();

    var outputType = output.MappedOutput.OutputType;
    var baseInputType = input.TargetType;
    var inputType = baseInputType.IsGenericType ? baseInputType.GenericTypeArguments.Last() : baseInputType;

    if (CastMap.TryGetValue((inputType, outputType), out var casts))
    {
      foreach (var cast in casts)
      {
        yield return new(cast);
      }
    }

    if (outputType == typeof(string))
    {
      if (psuedoGenericTypes.Parse.FirstOrDefault(n => n.Types.SequenceEqual([inputType])) is { Node: { } parseNode })
      {
        yield return new(parseNode);
      }
      ;
    }

    if (inputType == typeof(string))
    {
      if (psuedoGenericTypes.ToString_.FirstOrDefault(n => n.Types.SequenceEqual([outputType])) is { Node: { } toStringNode })
      {
        yield return new(toStringNode);
      }
      else
      {
        // todo: make this automatic.
        // todo: better layout
        yield return new(typeof(ToString_object), onSpawn: toStringNode =>
        {
          var castNode = outputType switch
          {
            var t when t.IsUnmanaged() => typeof(ValueToObjectCast<>).TryMakeGenericType(t),
            var t when ReflectionHelper.IsNullable(t) => typeof(NullableToObjectCast<>).TryMakeGenericType(Nullable.GetUnderlyingType(t) ?? t),
            var t => typeof(ObjectCast<,>).TryMakeGenericType(t, typeof(object))
          };

          if (castNode != null && ProtoFluxHelper.GetBindingForNode(castNode) is { } castNodeBinding)
          {
            var n = tool.SpawnNode(castNodeBinding, setup: n =>
            {
              n.GetInput(0).Target = output;
              toStringNode.GetInput(0).Target = n.GetOutput(0);
              node.TryConnectInput(input, toStringNode.GetOutput(0), allowExplicitCast: false, undoable: true);
            });
            n.Slot.GlobalPosition = node.Slot.LocalPointToGlobal(
              float3.Left * ProtoFluxNodeVisual.DEFAULT_WIDTH * ProtoFluxNodeVisual.DEFAULT_SCALE * 2.5f
            );
          }
        });
      }
    }
  }

  static Dictionary<(Type, Type), HashSet<Type>> CastMap =>
    field ??= GetCastPairs()
      .GroupBy(a => a.io)
      .ToDictionary(g => g.Key, g => g.Select(a => a.node).ToHashSet());

  static IEnumerable<((Type input, Type output) io, Type node)> GetCastPairs()
  {
    foreach (var type in NodeTypes())
    {
      var outputs = NodeMetadataUtils.GetOutputMetadata(type).ToList();
      var inputs = NodeMetadataUtils.GetInputMetadata(type).ToList();

      if (outputs.Count != 1) continue;
      if (inputs.Count != 1) continue;

      yield return ((outputs.First().OutputType, inputs.First().InputType), type);
    }
  }

  public static IEnumerable<Type> NodeTypes() =>
    Traverse.Create(typeof(ProtoFluxHelper)).Field<Dictionary<Type, Type>>("protoFluxToBindingMapping").Value.Keys;

  [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "CleanupDraggedWire")]
  extern static void CleanupDraggedWire(ProtoFluxTool instance);
}
