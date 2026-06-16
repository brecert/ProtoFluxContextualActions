using System;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.ProtoFlux;

using ProtoFluxContextualActions.Attributes;
using HarmonyLib;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Linq;
using ProtoFlux.Core;
using ProtoFlux.Runtimes.Execution.Nodes;
using ProtoFlux.Runtimes.Execution.Nodes.Operators;
using ProtoFlux.Runtimes.Execution.Nodes.Math.Quaternions;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Audio;
using SharpPipe;
using ProtoFluxContextualActions.Utils;
using System.Diagnostics.CodeAnalysis;
using ProtoFlux.Runtimes.Execution.Nodes.Math.SphericalHarmonics;
using ProtoFlux.Runtimes.Execution.Nodes.Math.Rects;
using ProtoFlux.Runtimes.Execution;

namespace ProtoFluxContextualActions.Patches;

[HarmonyPatchCategory("ProtoFluxTool Contextual Actions"), TweakCategory("Adds 'Contextual Actions' to the ProtoFlux Tool. Pressing secondary while holding a protoflux tool will open a context menu of actions based on what wire you're dragging instead of always spawning an input/display node. Pressing secondary again will spawn out an input/display node like normal.")]
[HarmonyPatch(typeof(ProtoFluxTool), nameof(ProtoFluxTool.OnSecondaryPress))]
internal static partial class ContextualSelectionActionsPatch
{

  internal struct MenuItem(
    Type node, Type? binding = null, string? name = null, bool overload = false,
    string group = "", Func<ProtoFluxNode, ProtoFluxElementProxy, ProtoFluxTool, bool>? onNodeSpawn = null,
    int orderOffset = 0) : IGroupItem
  {
    internal readonly Type node = node;

    internal readonly Type? binding = binding;

    internal readonly string? name = name;

    internal readonly bool overload = overload;


    internal readonly string group = group;

    // allows for items to be placed before/after others, without needing to reorder the code itself.
    internal readonly int orderOffset = orderOffset;

    internal readonly Func<ProtoFluxNode, ProtoFluxElementProxy, ProtoFluxTool, bool>? onNodeSpawn = onNodeSpawn;

    internal readonly string DisplayName => name ?? NodeMetadataHelper.GetMetadata(node).Name ?? node.GetNiceTypeName();

    internal Action<ProtoFluxTool, IGroupItem>? currentAction = null;

    readonly string IGroupItem.Name => DisplayName;
    readonly colorX IGroupItem.Color => node.GetTypeColor();
    readonly string IGroupItem.Group => group;
    readonly Action<ProtoFluxTool, IGroupItem> IGroupItem.OnClick => currentAction!;
  }

  [HarmonyPostfix]
  [HarmonyPatch(typeof(ProtoFluxTool), nameof(ProtoFluxTool.GenerateMenuItems))]
  internal static void GenerateMenuItemsPatch()
  {
    lastProxy = null;
  }

  static ProtoFluxElementProxy? lastProxy = null;

  [HarmonyPrefix]
  [HarmonyPatch(typeof(ProtoFluxTool), nameof(ProtoFluxTool.OnPrimaryRelease))]
  internal static void PrimaryReleasePatch(ProtoFluxTool __instance, SyncRef<ProtoFluxElementProxy> ____currentProxy)
  {
    if (!ProtoFluxContextualActions.ShouldDoDefaultActionOnPrimaryRelease()) return;
    if (!__instance.LocalUser.IsContextMenuOpen()) return;
    // only allow the contextmenu to trigger if the menu came from the tool
    if (__instance.LocalUser.GetUserContextMenu().CurrentSummoner != __instance) return;
    __instance.OnSecondaryPress();
  }

  internal static bool Prefix(ProtoFluxTool __instance, SyncRef<ProtoFluxElementProxy> ____currentProxy)
  {
    var grabbedReference = __instance.GetGrabbedReference();
    // Grabbed References usually mean we should not run the main function.
    if (grabbedReference != null) return true;
    var elementProxy = ____currentProxy.Target;
    if (elementProxy == null)
    {
      if (__instance.LocalUser.IsContextMenuOpen())
      {
        if (lastProxy != null)
        {
          __instance.StartDraggingWire(lastProxy);
          __instance.LocalUser.CloseContextMenu(__instance);
          lastProxy = null;
        }
      }
      return true;
    }

    IEnumerable<MenuItem> selectionItems = MenuItems(elementProxy)
      .Where(i => (i.binding ?? i.node)
      .IsValidGenericType(validForInstantiation: true)); // this isn't great, we should instead catch errors before they propigate to here.
    bool hasSwaps = false;
    ProtoFluxNode? swapRoot = null;
    var hit = GetHit(__instance);
    if (hit is { Collider.Slot: var hitSlot })
    {
      var hitNode = hitSlot.GetComponentInParents<ProtoFluxNode>();
      hasSwaps = hitNode != null;
      swapRoot = hitNode;
    }
    IEnumerable<IGroupItem> swapItems = hasSwaps
      ?
        ContextualSwapActionsPatch.GetMenuItems(__instance, swapRoot!, elementProxy, true)
        .Select<ContextualSwapActionsPatch.MenuItem, IGroupItem>(item => { item.group = string.IsNullOrEmpty(item.group) ? "Swaps" : "Swaps/" + item.group; return item; })
      :
        [];
    List<IGroupItem> items = selectionItems.Select<MenuItem, IGroupItem>(item => item)
      .Concat(swapItems)
      .ToList();
    // todo: pages / menu

    if (items.Count != 0)
    {
      if (__instance.LocalUser.IsContextMenuOpen())
      {
        if (elementProxy == null && lastProxy != null)
        {
          __instance.StartDraggingWire(lastProxy);
        }
        __instance.LocalUser.CloseContextMenu(__instance);
        return true;
      }
      Action<ProtoFluxTool, ProtoFluxElementProxy, MenuItem, ProtoFluxNode>? currentAction = null;
      colorX? targetColor = null;

      lastProxy = elementProxy;

      switch (elementProxy)
      {
        case ProtoFluxInputProxy inputProxy:
          {
            targetColor = inputProxy.InputType.Value.GetTypeColor();
            currentAction = ProcessInputProxyItem;
            break;
          }
        case ProtoFluxOutputProxy outputProxy:
          {
            targetColor = outputProxy.OutputType.Value.GetTypeColor();
            currentAction = ProcessOutputProxyItem;
            break;
          }
        case ProtoFluxImpulseProxy impulseProxy:
          {
            currentAction = ProcessImpulseProxyItem;
            break;
          }
        case ProtoFluxOperationProxy operationProxy:
          {
            currentAction = ProcessOperationProxyItem;
            break;
          }
        default:
          throw new Exception("found items for unsupported protoflux contextual action type");
      }

      selectionItems = selectionItems.Select(item =>
      {
        item.currentAction = (tool, item) => OnMenuItemClicked(tool, (MenuItem)item, (node) => currentAction(__instance, elementProxy, (MenuItem)item, node));
        return item;
      });

      items = selectionItems.Select<MenuItem, IGroupItem>(item => item).Concat(swapItems).ToList();

      // the idea behind this would have worked, but i must have written it wrong as this breaks all ordering of everything
      //items.Sort((a, b) => a.orderOffset - b.orderOffset);

      GroupManager grouper = new(__instance, items, targetColor);
      bool success = grouper.RenderRoot(true);

      return !success;
    }

    return true;
  }

  private static void OnMenuItemClicked(ProtoFluxTool tool, MenuItem item, Action<ProtoFluxNode> setup)
  {
    var nodeBinding = item.binding ?? ProtoFluxHelper.GetBindingForNode(item.node);
    tool.SpawnNode(nodeBinding, n =>
    {
      n.EnsureElementsInDynamicLists();
      setup(n);
      tool.LocalUser.CloseContextMenu(tool);
      CleanupDraggedWire(tool);
    });
  }

  private static void ProcessInputProxyItem(ProtoFluxTool tool, ProtoFluxElementProxy elementProxy, MenuItem item, ProtoFluxNode addedNode)
  {
    ProtoFluxInputProxy inputProxy = (ProtoFluxInputProxy)elementProxy;
    if (item.overload)
    {
      tool.StartTask(async () =>
      {
        // this is dumb
        // TODO: investigate why it's needed to avoid the one or two update disconnect issue
        await new Updates(1);

        if (item.onNodeSpawn != null)
        {
          bool doConnect = item.onNodeSpawn(addedNode, elementProxy, tool);

          if (!doConnect) return;
        }

        var output = addedNode.GetOutput(0); // TODO: specify
        elementProxy.Node.Target.TryConnectInput(inputProxy.NodeInput.Target, output, allowExplicitCast: false, undoable: true);
      });
    }
    else
    {
      if (item.onNodeSpawn != null)
      {
        bool doConnect = item.onNodeSpawn(addedNode, elementProxy, tool);

        if (!doConnect) return;
      }
      var output = addedNode.NodeOutputs
      .FirstOrDefault(o => typeof(INodeOutput<>).MakeGenericType(inputProxy.InputType).IsAssignableFrom(o.GetType()))
      ?? throw new Exception($"Could not find matching output of type '{inputProxy.InputType}' in '{addedNode}'");

      elementProxy.Node.Target.TryConnectInput(inputProxy.NodeInput.Target, output, allowExplicitCast: false, undoable: true);
    }
  }
  private static void ProcessOutputProxyItem(ProtoFluxTool tool, ProtoFluxElementProxy elementProxy, MenuItem item, ProtoFluxNode addedNode)
  {
    ProtoFluxOutputProxy outputProxy = (ProtoFluxOutputProxy)elementProxy;
    if (item.overload) throw new Exception("Overloading with ProtoFluxOutputProxy is not supported");
    var input = addedNode.NodeInputs
        .FirstOrDefault(i => i.TargetType.IsGenericType && (outputProxy.OutputType.Value.IsAssignableFrom(i.TargetType.GenericTypeArguments[0]) || ProtoFlux.Core.TypeHelper.CanImplicitlyConvertTo(outputProxy.OutputType, i.TargetType.GenericTypeArguments[0])))
        ?? (ISyncRef)addedNode.NodeInputLists.First().GetElement(0) ?? throw new Exception($"Could not find matching input of type '{outputProxy.OutputType}' in '{addedNode}'");

    tool.StartTask(async () =>
    {
      // this is dumb
      // TODO: investigate why it's needed for casting to work
      await new Updates();

      if (item.onNodeSpawn != null)
      {
        bool doConnect = item.onNodeSpawn(addedNode, elementProxy, tool);

        if (!doConnect) return;
      }

      addedNode.TryConnectInput(input, outputProxy.NodeOutput.Target, allowExplicitCast: false, undoable: true);
    });
  }
  private static void ProcessImpulseProxyItem(ProtoFluxTool tool, ProtoFluxElementProxy elementProxy, MenuItem item, ProtoFluxNode addedNode)
  {
    ProtoFluxImpulseProxy impulseProxy = (ProtoFluxImpulseProxy)elementProxy;
    if (item.overload) throw new Exception("Overloading with ProtoFluxImpulseProxy is not supported");

    if (item.onNodeSpawn != null)
    {
      bool doConnect = item.onNodeSpawn(addedNode, elementProxy, tool);

      if (!doConnect) return;
    }

    var operation = addedNode.NodeOperationCount > 0 ? addedNode.GetOperation(0) : addedNode.GetOperationList(0).GetElement(0) as INodeOperation;
    addedNode.TryConnectImpulse(impulseProxy.NodeImpulse.Target, operation!, undoable: true);
  }
  private static void ProcessOperationProxyItem(ProtoFluxTool tool, ProtoFluxElementProxy elementProxy, MenuItem item, ProtoFluxNode addedNode)
  {
    ProtoFluxOperationProxy operationProxy = (ProtoFluxOperationProxy)elementProxy;
    if (item.overload) throw new Exception("Overloading with ProtoFluxOperationProxy is not supported");
    if (item.onNodeSpawn != null)
    {
      bool doConnect = item.onNodeSpawn(addedNode, elementProxy, tool);

      if (!doConnect) return;
    }
    addedNode.TryConnectImpulse(addedNode.GetImpulse(0), operationProxy.NodeOperation.Target, undoable: true);
  }

  // note: if we can build up a graph then we can egraph reduce to make matches like this easier to spot automatically rather than needing to check each one manually
  // todo: detect add + 1 and offer to convert to inc?
  // todo: detect add + 1 or inc and write and offer to convert to increment?

  // todo: increase amount of available nodes for all types?
  // todo: organize certain nodes into useful groups?
  internal static IEnumerable<MenuItem> MenuItems(ProtoFluxElementProxy? target)
  {
    foreach (var item in GeneralNumericOperationMenuItems(target)) yield return item;
    foreach (var item in GeneralObjectOperationMenuItems(target)) yield return item;

    if (target is ProtoFluxInputProxy inputProxy)
    {
      foreach (var item in InputMenuItems(inputProxy)) yield return item;
    }

    else if (target is ProtoFluxOutputProxy outputProxy)
    {
      foreach (var item in OutputMenuItems(outputProxy)) yield return item;
    }

    else if (target is ProtoFluxImpulseProxy impulseProxy)
    {
      foreach (var item in ImpulseMenuItems(impulseProxy)) yield return item;
    }

    else if (target is ProtoFluxOperationProxy operationProxy)
    {
      foreach (var item in OperationMenuItems(operationProxy)) yield return item;
    }
  }

  internal static Dictionary<Type, List<Type>> UnpackNodeMapping(World world) =>
    world.GetPsuedoGenericTypesForWorld()
          .UnpackingNodes()
          .Where(i => i.Types.Count() == 1)
          .Select(i => (i.Node, Type: i.Types.First()))
          .GroupBy(i => i.Type, i => i.Node)
          .Select(i => (i.Key, (IEnumerable<Type>)i))
          .Concat([
            (typeof(Rect), [typeof(RectToXYWH), typeof(RectToMinMax), typeof(RectToPositionSize)]),
            (typeof(SphericalHarmonicsL1<>),  [typeof(UnpackSH1<>)]),
            (typeof(SphericalHarmonicsL2<>),  [typeof(UnpackSH2<>)]),
            (typeof(SphericalHarmonicsL3<>),  [typeof(UnpackSH3<>)]),
            (typeof(SphericalHarmonicsL4<>),  [typeof(UnpackSH4<>)]),
          ])
          .ToDictionary(i => i.Item1, i => i.Item2.ToList());

  internal static bool TryGetUnpackNode(World world, Type nodeType, [NotNullWhen(true)] out List<Type>? value)
  {
    if (ReflectionHelper.IsNullable(nodeType) && Nullable.GetUnderlyingType(nodeType).IsUnmanaged() && Nullable.GetUnderlyingType(nodeType) is var underlyingType and not null)
    {
      try
      {
        value = [typeof(UnpackNullable<>).MakeGenericType(underlyingType)];
        return true;
      }
      catch
      {
        value = null;
        return false;
      }
    }
    var mappings = UnpackNodeMapping(world);
    if (TypeUtils.TryGetGenericTypeDefinition(nodeType, out var genericTypeDefinition) && mappings.TryGetValue(genericTypeDefinition, out var genericUnpackNodeTypes))
    {
      value = [.. genericUnpackNodeTypes.Select(t => t.MakeGenericType(nodeType.GenericTypeArguments))];
      return true;
    }
    else
    {
      return mappings.TryGetValue(nodeType, out value);
    }
  }

  internal static Dictionary<Type, List<Type>> PackNodeMappings(World world) =>
    world.GetPsuedoGenericTypesForWorld()
          .PackingNodes()
          .Where(i => i.Types.Count() == 1)
          .Select(i => (i.Node, Type: i.Types.First()))
          .GroupBy(i => i.Type, i => i.Node)
          .Select(i => (i.Key, (IEnumerable<Type>)i))
          .Concat([
            (typeof(Rect), [typeof(RectFromXYWH), typeof(RectFromMinMax), typeof(RectFromPositionSize)]),
            (typeof(ZitaParameters), [typeof(ConstructZitaParameters)]),
            (typeof(SphericalHarmonicsL1<>),  [typeof(PackSH1<>)]),
            (typeof(SphericalHarmonicsL2<>),  [typeof(PackSH2<>)]),
            (typeof(SphericalHarmonicsL3<>),  [typeof(PackSH3<>)]),
            (typeof(SphericalHarmonicsL4<>),  [typeof(PackSH4<>)]),
          ])
          .ToDictionary(i => i.Item1, i => i.Item2.ToList());


  internal static bool TryGetPackNode(World world, Type nodeType, [NotNullWhen(true)] out List<Type>? value)
  {
    if (ReflectionHelper.IsNullable(nodeType) && Nullable.GetUnderlyingType(nodeType).IsUnmanaged() && Nullable.GetUnderlyingType(nodeType) is Type underlyingType)
    {
      try
      {
        value = [typeof(PackNullable<>).MakeGenericType(underlyingType)];
        return true;
      }
      catch
      {
        value = null;
        return false;
      }
    }

    var mappings = PackNodeMappings(world);
    if (TypeUtils.TryGetGenericTypeDefinition(nodeType, out var genericTypeDefinition) && mappings.TryGetValue(genericTypeDefinition, out var genericUnpackNodeType))
    {
      value = [.. genericUnpackNodeType.Select(t => t.MakeGenericType(nodeType.GenericTypeArguments))];
      return true;
    }
    else
    {
      return mappings.TryGetValue(nodeType, out value);
    }
  }

  internal static readonly Dictionary<Type, Type> InverseNodeMapping = new()
    {
        {typeof(float2x2), typeof(Inverse_Float2x2)},
        {typeof(float3x3), typeof(Inverse_Float3x3)},
        {typeof(float4x4), typeof(Inverse_Float4x4)},
        {typeof(double2x2), typeof(Inverse_Double2x2)},
        {typeof(double3x3), typeof(Inverse_Double3x3)},
        {typeof(double4x4), typeof(Inverse_Double4x4)},
        // shh
        {typeof(floatQ), typeof(InverseRotation_floatQ)},
        {typeof(doubleQ), typeof(InverseRotation_doubleQ)},
    };

  internal static bool TryGetInverseNode(Type valueType, [NotNullWhen(true)] out Type? value) =>
      InverseNodeMapping.TryGetValue(valueType, out value);

  internal static readonly Dictionary<Type, Type> TransposeNodeMapping = new()
    {
        {typeof(float2x2), typeof(Transpose_Float2x2)},
        {typeof(float3x3), typeof(Transpose_Float3x3)},
        {typeof(float4x4), typeof(Transpose_Float4x4)},
        {typeof(double2x2), typeof(Transpose_Double2x2)},
        {typeof(double3x3), typeof(Transpose_Double3x3)},
        {typeof(double4x4), typeof(Transpose_Double4x4)},
    };

  internal static bool TryGetTransposeNode(Type valueType, [NotNullWhen(true)] out Type? value) =>
      TransposeNodeMapping.TryGetValue(valueType, out value);

  private static bool IsIterationNode(Type nodeType) =>
      nodeType == typeof(For)
      || nodeType == typeof(AsyncFor)
      || nodeType == typeof(While)
      || nodeType == typeof(AsyncWhile);

  private static Type? GetIVariableValueType(Type type)
  {
    if (TypeUtils.MatchInterface(type, typeof(IVariable<,>), out var varType))
    {
      return varType.GenericTypeArguments[1];
    }
    return null;
  }

  [HarmonyReversePatch]
  [HarmonyPatch(typeof(ProtoFluxTool), "CleanupDraggedWire")]
  [MethodImpl(MethodImplOptions.NoInlining)]
  internal static void CleanupDraggedWire(ProtoFluxTool instance) => throw new NotImplementedException();

  [HarmonyReversePatch]
  [HarmonyPatch(typeof(ProtoFluxTool), "OnSecondaryPress")]
  [MethodImpl(MethodImplOptions.NoInlining)]
  internal static void OnSecondaryPress(ProtoFluxTool instance) => throw new NotImplementedException();

  [HarmonyReversePatch]
  [HarmonyPatch(typeof(ProtoFluxHelper), "GetNodeForType")]
  [MethodImpl(MethodImplOptions.NoInlining)]
  internal static Type GetNodeForType(Type type, List<NodeTypeRecord> list) => throw new NotImplementedException();

  [HarmonyReversePatch]
  [HarmonyPatch(typeof(Tool), "GetHit")]
  [MethodImpl(MethodImplOptions.NoInlining)]
  internal static RaycastHit? GetHit(Tool instance) => throw new NotImplementedException();
}