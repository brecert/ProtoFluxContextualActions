using System;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.ProtoFlux;

using ProtoFluxContextualActions.Attributes;
using static ProtoFluxContextualActions.Utils.PsuedoGenericUtils;
using HarmonyLib;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Linq;
using ProtoFlux.Core;
using ProtoFlux.Runtimes.Execution.Nodes;
using ProtoFlux.Runtimes.Execution.Nodes.Operators;
using ProtoFlux.Runtimes.Execution.Nodes.Math.Quaternions;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Transform;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Slots;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Users;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Async;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Audio;
using SharpPipe;
using ProtoFlux.Runtimes.Execution.Nodes.TimeAndDate;
using ProtoFlux.Runtimes.Execution.Nodes.Math;
using ProtoFlux.Runtimes.Execution.Nodes.Strings.Characters;
using ProtoFlux.Runtimes.Execution.Nodes.Strings;
using ProtoFlux.Runtimes.Execution.Nodes.ParsingFormatting;
using ProtoFlux.Runtimes.Execution.Nodes.Actions;
using ProtoFluxContextualActions.Utils;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.References;
using FrooxEngine.ProtoFlux.CoreNodes;
using ProtoFlux.Runtimes.Execution.Nodes.Math.Bounds;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Worlds;
using Elements.Quantity;
using ProtoFlux.Runtimes.Execution.Nodes.Math.Quantity;
using ProtoFlux.Runtimes.Execution.Nodes.Utility;
using System.Diagnostics.CodeAnalysis;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Rendering;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Assets;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Utility;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Users.Roots;
using ProtoFluxContextualActions.Extensions;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Physics;
using Renderite.Shared;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Avatar;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Avatar.BodyNodes;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Interaction;
using ProtoFlux.Runtimes.Execution.Nodes.Enums;
using ProtoFlux.Runtimes.Execution.Nodes.Math.Constants;
using ProtoFlux.Runtimes.Execution.Nodes.Math.Random;
using ProtoFluxContextualActions.Tagging;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Input.Mouse;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Users.LocalScreen;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Interaction.Tools;
using ProtoFlux.Runtimes.Execution.Nodes.Math.SphericalHarmonics;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Input.Keyboard;
using ProtoFlux.Runtimes.Execution.Nodes.Math.Rects;
using ProtoFlux.Runtimes.Execution.Nodes.Utility.Uris;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Variables;
using System.Text.RegularExpressions;
using ProtoFlux.Runtimes.Execution;
using FrooxEngine.Undo;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Input.Controllers;
using System.Globalization;
using ProtoFlux.Runtimes.Execution.Nodes.Binary;
using ProtoFlux.Runtimes.Execution.Nodes.Color;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Operators;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Time;
using ProtoFlux.Runtimes.Execution.Nodes.Casts;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Locomotion;

namespace ProtoFluxContextualActions.Patches;

[HarmonyPatchCategory("ProtoFluxTool Contextual Actions"), TweakCategory("Adds 'Contextual Actions' to the ProtoFlux Tool. Pressing secondary while holding a protoflux tool will open a context menu of actions based on what wire you're dragging instead of always spawning an input/display node. Pressing secondary again will spawn out an input/display node like normal.")]
[HarmonyPatch(typeof(ProtoFluxTool), nameof(ProtoFluxTool.OnSecondaryPress))]
internal static class ContextualSelectionActionsPatch
{

  internal readonly struct MenuItem(
    Type node, Type? binding = null, string? name = null, bool overload = false,
    string group = "", Func<ProtoFluxNode, ProtoFluxElementProxy, ProtoFluxTool, bool>? onNodeSpawn = null,
    int orderOffset = 0,
    bool isSwap = false, ProtoFluxNode? swapNode = null, ContextualSwapActionsPatch.ConnectionTransferType? swapType = null)
  {
    internal readonly Type node = node;

    internal readonly Type? binding = binding;

    internal readonly string? name = name;

    internal readonly bool overload = overload;


    internal readonly string group = group;

    // allows for items to be placed before/after others, without needing to reorder the code itself.
    internal readonly int orderOffset = orderOffset;

    // including this here sucks, but it isnt like i can currently put this anywhere else, considering how everything is structured
    internal readonly bool isSwap = isSwap;
    internal readonly ProtoFluxNode? swapNode = swapNode;
    internal readonly ContextualSwapActionsPatch.ConnectionTransferType? swapType = swapType;

    internal readonly Func<ProtoFluxNode, ProtoFluxElementProxy, ProtoFluxTool, bool>? onNodeSpawn = onNodeSpawn;

    internal readonly string DisplayName => name ?? NodeMetadataHelper.GetMetadata(node).Name ?? node.GetNiceTypeName();
  }

  [HarmonyPostfix]
  [HarmonyPatch(typeof(ProtoFluxTool), nameof(ProtoFluxTool.GenerateMenuItems))]
  internal static void GenerateMenuItemsPatch()
  {
    lastProxy = null;
  }

  static ProtoFluxElementProxy? lastProxy = null;

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

    var selectionItems = MenuItems(elementProxy);
    bool hasSwaps = false;
    ProtoFluxNode? swapRoot = null;
    var hit = GetHit(__instance);
    if (hit is { Collider.Slot: var hitSlot })
    {
      var hitNode = hitSlot.GetComponentInParents<ProtoFluxNode>();
      hasSwaps = hitNode != null;
      swapRoot = hitNode;
    }
    var swapItems = hasSwaps ? ContextualSwapActionsPatch.GetMenuItems(__instance, swapRoot!, elementProxy, true).Select((item) => new MenuItem(item.node, group: "Swaps", name: item.name, isSwap: true, swapNode: swapRoot, swapType: item.connectionTransferType)) : [];
    var items = selectionItems.Concat(swapItems)
      .Where(i => (i.binding ?? i.node).IsValidGenericType(validForInstantiation: true)) // this isn't great, we should instead catch errors before they propigate to here.
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

      // the idea behind this would have worked, but i must have written it wrong as this breaks all ordering of everything
      //items.Sort((a, b) => a.orderOffset - b.orderOffset);
      GroupManager grouper = new(__instance, items, targetColor, (item) => OnMenuItemClicked(__instance, item, (node) => currentAction(__instance, elementProxy, item, node)));
      bool success = grouper.RenderRoot(true);

      return !success;
    }

    return true;
  }

  private static void OnMenuItemClicked(ProtoFluxTool tool, MenuItem item, Action<ProtoFluxNode> setup)
  {
    if (item.isSwap)
    {
      ContextualSwapActionsPatch.OnSwapNode(tool, item.swapNode!, new(item.node, item.name, item.swapType));
      return;
    }
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

  private static IEnumerable<MenuItem> ImpulseMenuItems(ProtoFluxImpulseProxy impulseProxy)
  {
    var nodeType = impulseProxy.Node.Target.NodeType;

    // TODO: convert to while?
    yield return new MenuItem(typeof(For), group: "Loops");
    yield return new MenuItem(typeof(If));
    yield return new MenuItem(typeof(ValueWrite<int>), group: "Variables"); // while using dummy works, having int be the default is better (and its more consistent)
    yield return new MenuItem(typeof(Sequence));
    yield return new MenuItem(typeof(While), group: "Loops");
    yield return new MenuItem(typeof(ImpulseDemultiplexer), name: "Impulse Demultiplex");

    yield return new MenuItem(typeof(DynamicImpulseTrigger));
    yield return new MenuItem(typeof(StartAsyncTask), group: "Async");
    yield return new MenuItem(typeof(AsyncFor), group: "Async/Loops");
    yield return new MenuItem(typeof(AsyncWhile), group: "Async/Loops");
    yield return new MenuItem(typeof(AsyncSequence), group: "Async");
    yield return new MenuItem(typeof(DelayUpdates), group: "Async");
    yield return new MenuItem(typeof(DelaySecondsFloat), group: "Async");
    yield return new MenuItem(typeof(AsyncDynamicImpulseTrigger), group: "Async");

    yield return new MenuItem(typeof(DataModelBooleanToggle), group: "Variables");

    if (IsIterationNode(nodeType))
    {
      yield return new MenuItem(typeof(ValueIncrement<int>), group: "Variables");
      yield return new MenuItem(typeof(ValueDecrement<int>), group: "Variables");
    }

    else if (nodeType == typeof(DuplicateSlot))
    {
      yield return new MenuItem(typeof(SetGlobalTransform));
      yield return new MenuItem(typeof(SetLocalTransform));

      yield return new MenuItem(typeof(SetSlotPersistentSelf));
      yield return new MenuItem(typeof(SetSlotActiveSelf));
    }

    else if (nodeType == typeof(RenderToTextureAsset))
    {
      yield return new MenuItem(typeof(AttachTexture2D));
      yield return new MenuItem(typeof(AttachSprite));
    }

    else if (nodeType.IsGenericType)
    {
      var typeDef = nodeType.GetGenericTypeDefinition();
      if (typeDef == typeof(FireOnValueChange<>) || typeDef == typeof(FireOnObjectValueChange<>) || typeDef == typeof(FireOnLocalValueChange<>) || typeDef == typeof(FireOnLocalObjectChange<>))
      {
        yield return new MenuItem(typeof(LocalImpulseTimeoutSeconds));
      }
    }

    else if (nodeType == typeof(ImpulseDemultiplexer))
    {
      yield return new MenuItem(typeof(ImpulseMultiplexer), name: "Impulse Multiplex");
    }
  }

  private static IEnumerable<MenuItem> OperationMenuItems(ProtoFluxOperationProxy operationProxy)
  {

    yield return new MenuItem(typeof(FireOnTrue));
    yield return new MenuItem(typeof(FireOnFalse));
    yield return new MenuItem(typeof(FireOnValueChange<bool>));

    yield return new MenuItem(typeof(FireWhileTrue), group: "Loops");
    yield return new MenuItem(typeof(SecondsTimer), group: "Loops");
    yield return new MenuItem(typeof(Update), group: "Loops");
    yield return new MenuItem(typeof(LocalUpdate), group: "Loops");

    yield return new MenuItem(typeof(DynamicImpulseReceiver));

    yield return new MenuItem(typeof(StartAsyncTask), group: "Async");
    yield return new MenuItem(typeof(AsyncDynamicImpulseReceiver), group: "Async");


    // Events are pretty useful
    yield return new MenuItem(typeof(OnLoaded), group: "Events");
    yield return new MenuItem(typeof(OnSaving), group: "Events");
    yield return new MenuItem(typeof(OnStart), group: "Events");
    yield return new MenuItem(typeof(OnDuplicate), group: "Events");
    yield return new MenuItem(typeof(OnDestroy), group: "Events");
    yield return new MenuItem(typeof(OnDestroying), group: "Events");
    yield return new MenuItem(typeof(OnPackageImported), group: "Events");
  }

  internal static IEnumerable<MenuItem> GeneralNumericOperationMenuItems(ProtoFluxElementProxy? target)
  {
    {
      // TODO: It's nice to have these work with any node, I think their precedence should be lower than manually specified ones and potentially hidden by default for many types that support but do not need, esp. comparison.
      //       When I'm more sure that Swapping won't world crash I think I can limit comparison to a single node and then swap to the right one as a sort of submenu?
      //       Feels a little weird though, ux is difficult. A custom uix menu could help.
      if (target != null)
      {
        Type? nodeType = null;
        var world = target.World;
        var psuedoGenericTypes = world.GetPsuedoGenericTypesForWorld();
        if (target is ProtoFluxOutputProxy { OutputType.Value: var outputType } && (outputType.IsUnmanaged() || typeof(ISphericalHarmonics).IsAssignableFrom(outputType)))
        {
          var coder = Traverse.Create(typeof(Coder<>).MakeGenericType(outputType));
          var isMatrix = outputType.IsMatrixType();
          var isQuaternion = outputType.IsQuaternionType();
          nodeType = outputType;
          // only handle values

          if (isQuaternion)
          {
            if (TryGetPsuedoGenericForType(world, "Slerp_", outputType) is Type slerpType)
            {
              yield return new MenuItem(slerpType);
            }

            if (TryGetPsuedoGenericForType(world, "Pow_", outputType) is Type powType)
            {
              yield return new MenuItem(powType);
            }

            if (coder.Property<bool>("SupportsMul").Value)
            {
              yield return new MenuItem(typeof(ValueMul<>).MakeGenericType(outputType));
            }

            if (coder.Property<bool>("SupportsDiv").Value)
            {
              yield return new MenuItem(typeof(ValueDiv<>).MakeGenericType(outputType));
            }
          }
          else
          {
            if (coder.Property<bool>("SupportsAddSub").Value)
            {
              yield return new MenuItem(typeof(ValueAdd<>).MakeGenericType(outputType));
              yield return new MenuItem(typeof(ValueSub<>).MakeGenericType(outputType));
            }

            if (coder.Property<bool>("SupportsMul").Value)
            {
              yield return new MenuItem(typeof(ValueMul<>).MakeGenericType(outputType));
            }

            if (coder.Property<bool>("SupportsDiv").Value)
            {
              yield return new MenuItem(typeof(ValueDiv<>).MakeGenericType(outputType));
            }

            if (coder.Property<bool>("SupportsNegate").Value)
            {
              yield return new MenuItem(typeof(ValueNegate<>).MakeGenericType(outputType), group: "Math");
            }

            if (coder.Property<bool>("SupportsMod").Value)
            {
              yield return new MenuItem(typeof(ValueMod<>).MakeGenericType(outputType), group: "Math");
            }

            if (coder.Property<bool>("SupportsAbs").Value && !isMatrix)
            {
              yield return new MenuItem(typeof(ValueAbs<>).MakeGenericType(outputType), group: "Math");
            }

            if (coder.Property<bool>("SupportsComparison").Value)
            {
              yield return new MenuItem(typeof(ValueMax<>).MakeGenericType(outputType), group: "Comparisons");
              // yield return new MenuItem(typeof(ValueLessThan<>).MakeGenericType(outputType));
              // yield return new MenuItem(typeof(ValueLessOrEqual<>).MakeGenericType(outputType));
              // yield return new MenuItem(typeof(ValueGreaterThan<>).MakeGenericType(outputType));
              // yield return new MenuItem(typeof(ValueGreaterOrEqual<>).MakeGenericType(outputType));
              // yield return new MenuItem(typeof(ValueEquals<>).MakeGenericType(outputType));
              // yield return new MenuItem(typeof(ValueNotEquals<>).MakeGenericType(outputType));
            }

            if (coder.Property<bool>("SupportsAddSub").Value)
            {
              yield return new MenuItem(typeof(ValueInc<>).MakeGenericType(outputType), group: "Math");
              yield return new MenuItem(typeof(ValueOneMinus<>).MakeGenericType(outputType), group: "Math");
              yield return new MenuItem(typeof(ValueDelta<>).MakeGenericType(outputType), group: "Math/Time");
            }
            if (coder.Property<bool>("SupportsMul").Value)
            {
              yield return new MenuItem(typeof(ValueSquare<>).MakeGenericType(outputType), group: "Math");
              yield return new MenuItem(typeof(MulDeltaTime<>).MakeGenericType(outputType), group: "Math/Time");
            }
            if (coder.Property<bool>("SupportsDiv").Value)
            {
              yield return new MenuItem(typeof(ValueReciprocal<>).MakeGenericType(outputType), group: "Math");
            }
          }

          if (coder.Property<bool>("SupportsLerp").Value)
          {
            yield return new MenuItem(typeof(ValueLerp<>).MakeGenericType(outputType), group: "Math/Lerping");
          }
          if (coder.Property<bool>("SupportsSmoothLerp").Value)
          {
            yield return new MenuItem(typeof(ValueSmoothLerp<>).MakeGenericType(outputType), group: "Math/Lerping");
          }
          if (coder.Property<bool>("SupportsConstantLerp").Value)
          {
            yield return new MenuItem(typeof(ValueConstantLerp<>).MakeGenericType(outputType), group: "Math/Lerping");
          }

          if (coder.Property<bool>("SupportsMinMax").Value)
          {
            yield return new MenuItem(typeof(ValueClamp<>).MakeGenericType(outputType), group: "Comparisons");
          }

          if (TryGetInverseNode(outputType, out var inverseNodeType))
          {
            yield return new MenuItem(inverseNodeType);
          }

          if (TryGetTransposeNode(outputType, out var transposeNodeType))
          {
            yield return new MenuItem(transposeNodeType, name: "Transpose");
          }

          // While not often used, masking is useful.
          if (psuedoGenericTypes.Mask.Any(n => n.Types.First() == outputType))
          {
            yield return new(psuedoGenericTypes.Mask.First(n => n.Types.First() == outputType).Node, group: "Comparisons");
          }

          if (psuedoGenericTypes.Round.Any(n => n.Types.First() == outputType))
          {
            yield return new(psuedoGenericTypes.Round.First(n => n.Types.First() == outputType).Node, group: "Math");
          }

          if (outputType == typeof(bool))
          {
            foreach (var node in psuedoGenericTypes.ZeroOne)
            {
              yield return new(node.Node, group: "Zero One");
            }
          }

          if (outputType == typeof(float))
          {
            yield return new(typeof(Remap_Float), group: "Math");
          }
          if (outputType == typeof(double))
          {
            yield return new(typeof(Remap_Double), group: "Math");
          }

          if (nodeType == typeof(Half)) yield return new(typeof(HalfAsUShort), group: "Math/Binary");
          if (nodeType == typeof(float)) yield return new(typeof(FloatAsUInt), group: "Math/Binary");
          if (nodeType == typeof(double)) yield return new(typeof(DoubleAsULong), group: "Math/Binary");

          if (nodeType == typeof(ushort)) yield return new(typeof(UShortAsHalf), group: "Math/Binary");
          if (nodeType == typeof(uint)) yield return new(typeof(UIntAsFloat), group: "Math/Binary");
          if (nodeType == typeof(ulong)) yield return new(typeof(ULongAsDouble), group: "Math/Binary");

          if (nodeType == typeof(byte) || nodeType == typeof(ushort) || nodeType == typeof(uint) || nodeType == typeof(ulong))
          {
            if (nodeType == typeof(uint) || nodeType == typeof(ulong))
            {
              yield return new(psuedoGenericTypes.AND.First(n => n.Types.First() == outputType).Node, group: "Math/Binary");
              yield return new(psuedoGenericTypes.ShiftLeft.First(n => n.Types.First() == outputType).Node, group: "Math/Binary");
            }

            yield return new(psuedoGenericTypes.ExtractBits.First(n => n.Types.First() == outputType).Node, group: "Math/Binary");
          }
        }
        if (target is ProtoFluxInputProxy { InputType.Value: var inputType } && (inputType.IsUnmanaged() || typeof(ISphericalHarmonics).IsAssignableFrom(inputType)))
        {
          nodeType = inputType;
          if (psuedoGenericTypes.ZeroOne.Any(n => n.Types.First() == nodeType))
          {
            yield return new(psuedoGenericTypes.ZeroOne.First(n => n.Types.First() == nodeType).Node, group: "Math");
          }

          if (nodeType == typeof(Half)) yield return new(typeof(UShortAsHalf), group: "Math/Binary");
          if (nodeType == typeof(float)) yield return new(typeof(UIntAsFloat), group: "Math/Binary");
          if (nodeType == typeof(double)) yield return new(typeof(ULongAsDouble), group: "Math/Binary");

          if (nodeType == typeof(ushort)) yield return new(typeof(HalfAsUShort), group: "Math/Binary");
          if (nodeType == typeof(uint)) yield return new(typeof(FloatAsUInt), group: "Math/Binary");
          if (nodeType == typeof(ulong)) yield return new(typeof(DoubleAsULong), group: "Math/Binary");

          if (nodeType == typeof(byte) || nodeType == typeof(ushort) || nodeType == typeof(uint) || nodeType == typeof(ulong))
          {

            yield return new(psuedoGenericTypes.ComposeBits.First(n => n.Types.First() == nodeType).Node, group: "Math/Binary");
          }
        }
        if (nodeType != null)
        {
          // keeping this around *just in case* something ends up needing it.
          // though, i dont know what would actually go here, despite trying multiple times.
        }
      }
    }
  }

  internal static IEnumerable<MenuItem> GeneralObjectOperationMenuItems(ProtoFluxElementProxy? target)
  {
    /*if (target != null)
	{	
      var targetType = target.GetType();
      var typeArgs = targetType.GenericTypeArguments;
      var nodeType = typeArgs[typeArgs.Length - 1];
      var coder = Traverse.Create(typeof(Coder<>).MakeGenericType(nodeType));

      if (coder.Property<bool>("SupportsComparison").Value)
      {
        yield return new MenuItem(typeof(ObjectEquals<>).MakeGenericType(nodeType));
      }
      if (nodeType.IsNullable())
	  {
        yield return new MenuItem(typeof(IsNull<>).MakeGenericType(nodeType));
        yield return new MenuItem(typeof(NotNull<>).MakeGenericType(nodeType));
	  }
      if (target is ProtoFluxOutputProxy { OutputType.Value: var outputType } && !outputType.IsUnmanaged())
      {
      }
	}*/
    yield break;
  }

  internal static Type GetUserControllerType(User user)
  {
    IStandardController controller = user.InputInterface.GetControllerNode(Chirality.Right);
    Type? controllerType = controller.GetType();
    if (controllerType != null)
    {
      if (controllerType == typeof(FrooxEngine.TouchController))
        return typeof(ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Input.Controllers.TouchController);
      if (controllerType == typeof(FrooxEngine.IndexController))
        return typeof(ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Input.Controllers.IndexController);
      if (controllerType == typeof(FrooxEngine.HPReverbController))
        return typeof(ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Input.Controllers.HPReverbController);
      if (controllerType == typeof(FrooxEngine.ViveController))
        return typeof(ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Input.Controllers.ViveController);
      if (controllerType == typeof(FrooxEngine.CosmosController))
        return typeof(ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Input.Controllers.CosmosController);
      if (controllerType == typeof(FrooxEngine.WindowsMRController))
        return typeof(ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Input.Controllers.WindowsMRController);
    }
    return typeof(StandardController);
  }

  #region Output Items
  /// <summary>
  /// Yields menu items when holding an output wire. 
  /// </summary>
  /// <param name="outputProxy"></param>
  /// <returns></returns>
  internal static IEnumerable<MenuItem> OutputMenuItems(ProtoFluxOutputProxy outputProxy)
  {
    var world = outputProxy.World;
    var nodeType = outputProxy.Node.Target.NodeType;
    var psuedoGenericTypes = world.GetPsuedoGenericTypesForWorld();

    var nodeInstance = outputProxy.Node.Target.NodeInstance;
    var query = new NodeQueryAcceleration(nodeInstance.Runtime.Group);
    var indirectlyConnectsToIterationNode = query.GetEvaluatingNodes(nodeInstance).Any(n => IsIterationNode(n.GetType()));

    if (TryGetUnpackNode(outputProxy.World, outputProxy.OutputType, out var unpackNodeTypes))
    {
      foreach (var unpackNodeType in unpackNodeTypes)
      {
        yield return new MenuItem(unpackNodeType);
      }
    }
    var outputType = outputProxy.OutputType.Value;

    var equalsNode = GetNodeForType(outputType, [
      new NodeTypeRecord(typeof(ValueEquals<>), null, null),
      new NodeTypeRecord(typeof(ObjectEquals<>), null, null),
    ]);
    yield return new MenuItem(equalsNode, group: "Comparisons");

    var conditionalNode = GetNodeForType(outputType, [
      new NodeTypeRecord(typeof(ValueConditional<>), null, null),
      new NodeTypeRecord(typeof(ObjectConditional<>), null, null),
    ]);
    yield return new MenuItem(conditionalNode, group: "Comparisons");

    if (outputType == typeof(Slot))
    {
      yield return new MenuItem(typeof(GlobalTransform));
      yield return new MenuItem(typeof(GetForward));
      yield return new MenuItem(typeof(GetChild));
      yield return new MenuItem(typeof(ChildrenCount));
      yield return new MenuItem(typeof(FindChildByTag), group: "Slots/Children"); // use tag here because it has less inputs which fits better when going to swap.
      yield return new MenuItem(typeof(GetSlotName), group: "Slots");

      yield return new MenuItem(typeof(SetSlotActiveSelf));
      yield return new MenuItem(typeof(SetSlotPersistentSelf), group: "Slots");

      yield return new MenuItem(typeof(SetGlobalTransform), group: "Slots/Transforms"); // swappable, but still useful to have right there

      yield return new MenuItem(typeof(DuplicateSlot));
      yield return new MenuItem(typeof(DestroySlot), group: "Slots");

      yield return new MenuItem(typeof(GetParentSlot), group: "Slots");
      yield return new MenuItem(typeof(SetParent), group: "Slots");

      yield return new MenuItem(typeof(GetActiveUser), group: "Slots");

      yield return new MenuItem(typeof(DynamicImpulseTrigger), group: "Events");

      yield return new MenuItem(typeof(SetForward), group: "Slots/Transforms");

      bool shouldRelay = ProtoFluxContextualActions.ShouldUseRelays();
      Type baseType = shouldRelay ? typeof(ObjectRelay<Slot>) : typeof(ChildrenCount);
      yield return new MenuItem(baseType, name: "Foreach Child", group: "Slots/Children", onNodeSpawn: (ProtoFluxNode node, ProtoFluxElementProxy proxy, ProtoFluxTool tool) =>
      {
        tool.StartTask(async () =>
        {
          Type childCountNode = typeof(FrooxEngine.ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Slots.ChildrenCount);
          Type forNode = typeof(FrooxEngine.ProtoFlux.Runtimes.Execution.Nodes.For);
          Type getChildNode = typeof(FrooxEngine.ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Slots.GetChild);
          Type relayNode = typeof(FrooxEngine.ProtoFlux.Runtimes.Execution.Nodes.ObjectRelay<Slot>);


          ProtoFluxNode? thisChildCountNode = null;
          ProtoFluxNode? thisForNode = null;
          ProtoFluxNode? thisGetChild = null;
          ProtoFluxNode? thisRelayNode = null;

          if (shouldRelay)
          {
            tool.SpawnNode(childCountNode, newNode =>
            {
              thisChildCountNode = newNode;
              newNode.EnsureVisual();
            });
            tool.SpawnNode(relayNode, newNode =>
          {
            thisRelayNode = newNode;
            newNode.EnsureVisual();
          });
          }
          tool.SpawnNode(forNode, newNode =>
          {
            thisForNode = newNode;
            newNode.EnsureVisual();
          });
          tool.SpawnNode(getChildNode, newNode =>
          {
            thisGetChild = newNode;
            newNode.EnsureVisual();
          });

          await new Updates(6);

          var nodeSlot = node.Slot;
          var origParent = nodeSlot.Parent;
          var tempSlot = origParent.AddSlot("Temp Flux Holder", false);
          tempSlot.CopyTransform(nodeSlot);
          nodeSlot.Parent = tempSlot;

          if (thisChildCountNode == null && shouldRelay) return;
          if (thisForNode == null) return;
          if (thisGetChild == null) return;
          if (thisRelayNode == null && shouldRelay) return;

          node.World.BeginUndoBatch("Create Foreach Child");

          node.Slot.CreateSpawnUndoPoint("Spawn Child Count");
          if (shouldRelay)
          {
            thisChildCountNode!.Slot.CreateSpawnUndoPoint("Spawn Child Count");
            thisRelayNode!.Slot.CreateSpawnUndoPoint("Spawn Relay");
          }
          thisForNode.Slot.CreateSpawnUndoPoint("Spawn For");
          thisGetChild.Slot.CreateSpawnUndoPoint("Spawn Get Child");

          // Inputs and outputs
          INodeOutput inputRelay = node.GetOutput(0);

          ISyncRef? childCountInstance = shouldRelay ? thisChildCountNode!.GetInput(0) : null;
          INodeOutput childCount = shouldRelay ? thisChildCountNode!.GetOutput(0) : node.GetOutput(0);

          ISyncRef forCount = thisForNode.GetInput(0);
          INodeOutput forIndex = thisForNode.GetOutput(0);

          ISyncRef childInstance = thisGetChild.GetInput(0);
          ISyncRef childIndex = thisGetChild.GetInput(1);

          ISyncRef? relayInstance = shouldRelay ? thisRelayNode!.GetInput(0) : null;
          INodeOutput? relayOutput = shouldRelay ? thisRelayNode!.GetOutput(0) : null;

          // Node Connections
          childInstance.Target = inputRelay;
          if (shouldRelay)
          {
            childCountInstance!.Target = inputRelay;
            relayInstance!.Target = inputRelay;
            childInstance.Target = relayOutput!;
          }

          forCount.Target = childCount;

          childIndex.Target = forIndex;

          // Positions
          float3 baseUp = nodeSlot.Up;
          float3 baseRight = nodeSlot.Right;

          void LocalTransformNode(ProtoFluxNode input, float X, float Y)
          {
            Slot target = input.Slot;
            target.CopyTransform(nodeSlot);
            target.Parent = nodeSlot.Parent;
            target.GlobalPosition += (baseUp * Y) + (baseRight * X);
          }

          var posOffset = shouldRelay ? 0 : -0.12f;

          LocalTransformNode(thisForNode, 0.27f + posOffset, -0.01125f);

          if (shouldRelay)
          {
            LocalTransformNode(thisChildCountNode!, 0.12f, 0.00375f);
            LocalTransformNode(thisRelayNode!, 0.075f, -0.105f);
          }

          LocalTransformNode(thisGetChild, 0.42f + posOffset, -0.11625f);

          node.World.EndUndoBatch();

          ProtoFluxNode?[] allNodes = [node, thisChildCountNode, thisForNode, thisGetChild, thisRelayNode];
          foreach (var node in allNodes)
          {
            if (node == null) continue;
            if (node.IsRemoved) continue;
            node.Slot.GetComponent<Grabbable>().Enabled = false;
          }
          var tempGrab = tempSlot.AttachComponent<Grabbable>();

          await new Updates(240);
          int i = 0;
          while (tempGrab.IsGrabbed && i < 200)
          {
            await new Updates(5);
            i++;
          }
          foreach (var node in allNodes)
          {
            if (node == null) continue;
            if (node.IsRemoved) continue;
            node.Slot.GetComponent<Grabbable>().Enabled = true;
          }

          tempSlot.Destroy(origParent);
        });

        return true;
      });

      yield return new MenuItem(
        typeof(ProtoFlux.Runtimes.Execution.Nodes.Casts.ObjectCast<Slot, IWorldElement>),
        name: "Allocating User",
        group: "Slots",
        onNodeSpawn: (ProtoFluxNode node, ProtoFluxElementProxy proxy, ProtoFluxTool tool) =>
        {
          tool.StartTask(async () =>
          {
            // Node spawning
            Type allocNode = typeof(FrooxEngine.ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.References.AllocatingUser);
            ProtoFluxNode? thisAllocNode = null;

            tool.SpawnNode(allocNode, newNode =>
            {
              thisAllocNode = newNode;
              newNode.EnsureVisual();
            });

            await new Updates(3);

            if (thisAllocNode == null)
            {
              node.Slot.Destroy();
              return;
            }

            node.World.BeginUndoBatch("Create Allocating User");

            node.Slot.CreateSpawnUndoPoint("Spawn Object Cast");
            thisAllocNode.Slot.CreateSpawnUndoPoint("Spawn Allocating User");

            // Inputs and outputs
            INodeOutput inputRelay = node.GetOutput(0);

            ISyncRef allocInstance = thisAllocNode.GetInput(0);

            allocInstance.Target = inputRelay;

            // Positions
            float3 baseUp = node.Slot.Up;
            float3 baseRight = node.Slot.Right;

            void LocalTransformNode(ProtoFluxNode input, float X, float Y)
            {
              Slot target = input.Slot;
              target.CopyTransform(node.Slot);
              target.GlobalPosition += (baseUp * Y) + (baseRight * X);
            }

            LocalTransformNode(thisAllocNode, 0.09f, 0.00375f);

            node.World.EndUndoBatch();
          });

          return true;
        }
      );

    }

    if (outputType == typeof(float2) || outputType == typeof(float3) || outputType == typeof(float4) ||
      outputType == typeof(double2) || outputType == typeof(double3) || outputType == typeof(double4))
    {
      yield return new(psuedoGenericTypes.Normalized.First(n => n.Types.First() == outputType).Node, group: "Vectors");
      yield return new(psuedoGenericTypes.Magnitude.First(n => n.Types.First() == outputType).Node, group: "Vectors");
      yield return new(psuedoGenericTypes.Dot.First(n => n.Types.First() == outputType).Node, group: "Vectors");
      yield return new(psuedoGenericTypes.Project.First(n => n.Types.First() == outputType).Node, group: "Vectors");
      if (outputType == typeof(float3) || outputType == typeof(double3))
      {
        yield return new(psuedoGenericTypes.Reflect.First(n => n.Types.First() == outputType).Node, group: "Vectors");
        yield return new(psuedoGenericTypes.Cross.First(n => n.Types.First() == outputType).Node, group: "Vectors");
      }
    }

    if (outputType == typeof(bool))
    {
      yield return new MenuItem(typeof(If));
      yield return new MenuItem(typeof(FireOnTrue), group: "Events");
      yield return new MenuItem(typeof(FireOnLocalTrue), group: "Events");
      yield return new MenuItem(typeof(FireWhileTrue), group: "Events");
      yield return new MenuItem(typeof(LocalFireWhileTrue), group: "Events");
    }

    var changeVariableNode = GetNodeForType(outputType, [
      new NodeTypeRecord(typeof(FireOnValueChange<>), null, null),
      new NodeTypeRecord(typeof(FireOnObjectValueChange<>), null, null),
      new NodeTypeRecord(typeof(FireOnRefChange<>), null, null),
    ]);
    yield return new MenuItem(changeVariableNode, group: "Events");
    var localChangeVariableNode = GetNodeForType(outputType, [
      new NodeTypeRecord(typeof(FireOnLocalValueChange<>), null, null),
      new NodeTypeRecord(typeof(FireOnLocalObjectChange<>), null, null),
    ]);
    yield return new MenuItem(localChangeVariableNode, group: "Events");

    if (!outputType.IsValueType)
    {
      yield return new MenuItem(typeof(IsNull<>).MakeGenericType(outputType), group: "Comparisons");
      yield return new MenuItem(typeof(NotNull<>).MakeGenericType(outputType), group: "Comparisons");
      yield return new MenuItem(typeof(NullCoalesce<>).MakeGenericType(outputType), group: "Comparisons");
    }

    if (outputType == typeof(string))
    {
      yield return new MenuItem(typeof(GetCharacter));
      yield return new MenuItem(typeof(StringLength));
      yield return new MenuItem(typeof(CountOccurrences));
      yield return new MenuItem(typeof(IndexOfString));
      yield return new MenuItem(typeof(Contains));
      yield return new MenuItem(typeof(Substring));
      yield return new MenuItem(typeof(FormatString));

      yield return new MenuItem(typeof(StripRTF_Tags));

      yield return new MenuItem(typeof(ConcatenateString));
      yield return new MenuItem(typeof(StringJoin));
      yield return new MenuItem(typeof(StringInsert));

      yield return new MenuItem(typeof(UnescapeString));
      yield return new MenuItem(typeof(UnescapeUriDataString));
    }
    else if (outputType == typeof(char))
		{
			yield return new MenuItem(typeof(CharToString));
		}

    else if (outputType == typeof(DateTime))
    {
      yield return new MenuItem(typeof(Sub_DateTime));
      yield return new MenuItem(typeof(Add_DateTime_TimeSpan));
      yield return new MenuItem(typeof(ToLocalTime));
    }

    else if (outputType == typeof(BoundingBox))
    {
      yield return new MenuItem(typeof(EncapsulateBounds));
      yield return new MenuItem(typeof(EncapsulatePoint));
      yield return new MenuItem(typeof(TransformBounds));
      yield return new MenuItem(typeof(BoundingBoxProperties));
    }

    else if (outputType == typeof(Camera))
    {
      yield return new(typeof(RenderToTextureAsset));
    }

    /*else if (outputType == typeof(int) && (IsIterationNode(nodeType) || nodeType == typeof(IndexOfString)))
    {
      yield return new MenuItem(typeof(ValueInc<int>));
      yield return new MenuItem(typeof(ValueDec<int>));
    }*/

    if (outputType == typeof(UserRef))
    {
      yield return new MenuItem(typeof(UserRefAsVariable));
    }

    if (outputType == typeof(UserRoot))
    {
      yield return new MenuItem(typeof(ActiveUserRootUser));
      yield return new MenuItem(typeof(UserRootGlobalScale));
      yield return new MenuItem(typeof(HeadSlot));
      yield return new MenuItem(typeof(HeadPosition));
      yield return new MenuItem(typeof(HeadRotation));
    }

    if (outputType == typeof(User))
    {
      yield return new MenuItem(typeof(UserUsername), group: "Info");
      yield return new MenuItem(typeof(UserUserID), group: "Info");
      yield return new MenuItem(typeof(IsLocalUser), group: "Info");
      yield return new MenuItem(typeof(UserVR_Active), group: "Info");
      yield return new MenuItem(typeof(UserRootSlot), group: "");
      yield return new MenuItem(typeof(UserUserRoot), group: "");


      yield return new MenuItem(typeof(FindCharacterControllerFromUser));

      yield return new MenuItem(typeof(GetActiveLocomotionModule));

      yield return new MenuItem(typeof(StandardController), group: "Input");
      Type controllerType = GetUserControllerType(Engine.Current.WorldManager.FocusedWorld.LocalUser);
      if (controllerType != typeof(StandardController)) yield return new MenuItem(controllerType, group: "Input");
      // todo: find a way to get the user from the output flux node?
      // if the user isnt null, add the controller type of the user to the list
    }

    if (outputType == typeof(BodyNode))
    {
      yield return new MenuItem(typeof(BodyNodeSlot));
      yield return new MenuItem(typeof(BodyNodeChirality));
      yield return new MenuItem(typeof(OtherSide));
      yield return new MenuItem(typeof(RelativeBodyNode));
    }

    if (outputType == typeof(Grabber))
    {
      yield return new MenuItem(typeof(GrabberBodyNode));
    }

    if (outputType == typeof(CharacterController))
    {
      yield return new MenuItem(typeof(CharacterLinearVelocity), group: "Velocity");
      yield return new MenuItem(typeof(IsCharacterOnGround), group: "State");
      yield return new MenuItem(typeof(CharacterControllerUser), group: "State");

      yield return new MenuItem(typeof(SetCharacterVelocity), group: "Velocity");
      yield return new MenuItem(typeof(ApplyCharacterImpulse), group: "Velocity");
    }

    if (outputType == typeof(ILocomotionModule))
		{
			yield return new MenuItem(typeof(GetLocomotionArchetype));
		}

    if (outputType == typeof(Type))
    {
      yield return new MenuItem(typeof(TypeColor));
      yield return new MenuItem(typeof(NiceTypeName));
    }

    if (outputType == typeof(Key))
    {
      yield return new MenuItem(typeof(KeyHeld));
    }

    if (outputType == typeof(object))
    {
      yield return new MenuItem(typeof(GetType));
      yield return new MenuItem(typeof(ToString_object));
    }

    else if (outputType == typeof(RefID))
    {
      yield return new MenuItem(typeof(ToString_object));
    }

    else {
      if (psuedoGenericTypes.ObjToString.Any(n => n.Types.First() == outputType))
      {
        yield return new(psuedoGenericTypes.ObjToString.First(n => n.Types.First() == outputType).Node, group: "Casts");
      }
      else if (outputType != typeof(string)) {
        yield return new(typeof(ToString_object), group: "Casts");
      }
    }

    if (outputType == typeof(colorX))
    {
      // add color swaps to allow this to work better?
      yield return new MenuItem(typeof(ColorXMulValue));
      yield return new MenuItem(typeof(ColorXSetAlpha));
      yield return new MenuItem(typeof(ColorXToHexCode));
    }

    if (typeof(IWorldElement).IsAssignableFrom(outputType) && outputType != typeof(IWorldElement))
    {
      yield return new MenuItem(
        typeof(ObjectCast<,>).MakeGenericType(outputType, typeof(IWorldElement)),
        name: "IWorldElement", group: "Casts"
      );
    }
    if (outputType != typeof(object))
		{
      if (outputType.IsUnmanaged())
      {
        yield return new MenuItem(
          typeof(ValueToObjectCast<>).MakeGenericType(outputType),
          name: "Object", group: "Casts"
        );
      }
      else if (ReflectionHelper.IsNullable(outputType))
			{
				yield return new MenuItem(typeof(NullableToObjectCast<>).MakeGenericType(Nullable.GetUnderlyingType(outputType) ?? outputType), name: "Object", group: "Casts");
			}
      else if (outputType.IsClass)
      {
        yield return new MenuItem(
          typeof(ObjectCast<,>).MakeGenericType(outputType, typeof(object)),
          name: "Object", group: "Casts"
        );
      }
		}
    

    if (outputType == typeof(IWorldElement))
    {
      yield return new MenuItem(typeof(ReferenceID));
      yield return new MenuItem(
        typeof(ReferenceID),
        name: "RefID -> ULong",
        onNodeSpawn: (ProtoFluxNode node, ProtoFluxElementProxy proxy, ProtoFluxTool tool) =>
        {
          tool.StartTask(async () =>
          {
            // Node spawning
            Type refIDObjectCastNode = typeof(FrooxEngine.ProtoFlux.Runtimes.Execution.Nodes.Casts.ValueToObjectCast<RefID>);
            Type toStringNode = typeof(FrooxEngine.ProtoFlux.Runtimes.Execution.Nodes.ParsingFormatting.ToString_object);
            Type stringRemoveNode = typeof(FrooxEngine.ProtoFlux.Runtimes.Execution.Nodes.Strings.StringRemove);
            Type parseULongNode = typeof(FrooxEngine.ProtoFlux.Runtimes.Execution.Nodes.ParsingFormatting.Parse_Ulong);
            Type lengthInputNode = ProtoFluxHelper.GetInputNode(typeof(int));
            Type numberStyleNode = ProtoFluxHelper.GetInputNode(typeof(NumberStyles));

            ProtoFluxNode? thisRefIDObjectCastNode = null;
            ProtoFluxNode? thisToStringNode = null;
            ProtoFluxNode? thisStringRemoveNode = null;
            ProtoFluxNode? thisParseULongNode = null;
            ProtoFluxNode? thisLengthInputNode = null;
            ProtoFluxNode? thisNumberStyleNode = null;

            tool.SpawnNode(refIDObjectCastNode, newNode =>
            {
              thisRefIDObjectCastNode = newNode;
              newNode.EnsureVisual();
            });
            tool.SpawnNode(toStringNode, newNode =>
            {
              thisToStringNode = newNode;
              newNode.EnsureVisual();
            });
            tool.SpawnNode(stringRemoveNode, newNode =>
            {
              thisStringRemoveNode = newNode;
              newNode.EnsureVisual();
            });
            tool.SpawnNode(parseULongNode, newNode =>
            {
              thisParseULongNode = newNode;
              newNode.EnsureVisual();
            });
            tool.SpawnNode(lengthInputNode, newNode =>
            {
              thisLengthInputNode = newNode;
              newNode.EnsureVisual();
            });
            tool.SpawnNode(numberStyleNode, newNode =>
            {
              thisNumberStyleNode = newNode;
              newNode.EnsureVisual();
            });

            await new Updates(6);

            var nodeSlot = node.Slot;
            var origParent = nodeSlot.Parent;
            var tempSlot = origParent.AddSlot("Temp Flux Holder", false);
            tempSlot.CopyTransform(nodeSlot);
            nodeSlot.Parent = tempSlot;

            if (
              thisRefIDObjectCastNode == null ||
              thisToStringNode == null ||
              thisStringRemoveNode == null ||
              thisParseULongNode == null ||
              thisLengthInputNode == null ||
              thisNumberStyleNode == null)
            {
              node.Slot.Destroy();
              thisRefIDObjectCastNode?.Slot.Destroy();
              thisToStringNode?.Slot.Destroy();
              thisStringRemoveNode?.Slot.Destroy();
              thisParseULongNode?.Slot.Destroy();
              thisLengthInputNode?.Slot.Destroy();
              thisNumberStyleNode?.Slot.Destroy();
              return;
            }

            node.World.BeginUndoBatch("Create RefID -> ULong");

            node.Slot.CreateSpawnUndoPoint("Spawn Object Cast");
            thisRefIDObjectCastNode.Slot.CreateSpawnUndoPoint("Spawn ToString Node");
            thisToStringNode.Slot.CreateSpawnUndoPoint("Spawn ToString Node");
            thisStringRemoveNode.Slot.CreateSpawnUndoPoint("Spawn String Remove Node");
            thisParseULongNode.Slot.CreateSpawnUndoPoint("Spawn Parse ULong");
            thisLengthInputNode.Slot.CreateSpawnUndoPoint("Spawn Length Input");
            thisNumberStyleNode.Slot.CreateSpawnUndoPoint("Spawn Number Styles Input");

            // Inputs and outputs
            INodeOutput inputRelay = node.GetOutput(0);

            ISyncRef refIDInstance = thisRefIDObjectCastNode.GetInput(0);
            INodeOutput refIDValue = thisRefIDObjectCastNode.GetOutput(0);
            ISyncRef objectInstance = thisToStringNode.GetInput(0);
            INodeOutput objectValue = thisToStringNode.GetOutput(0);
            ISyncRef stringRemoveInstance = thisStringRemoveNode.GetInput(0);
            ISyncRef stringRemoveLength = thisStringRemoveNode.GetInput(2);
            INodeOutput stringRemoveValue = thisStringRemoveNode.GetOutput(0);
            ISyncRef parseULongInstance = thisParseULongNode.GetInput(0);
            ISyncRef parseULongStyle = thisParseULongNode.GetInput(1);

            INodeOutput lengthValue = thisLengthInputNode.GetOutput(0);
            INodeOutput numberStylesValue = thisNumberStyleNode.GetOutput(0);

            refIDInstance.Target = inputRelay;
            objectInstance.Target = refIDValue;

            stringRemoveInstance.Target = thisToStringNode;
            parseULongInstance.Target = stringRemoveValue;

            stringRemoveLength.Target = lengthValue;
            parseULongStyle.Target = numberStylesValue;

            (thisLengthInputNode as FrooxEngine.ProtoFlux.Runtimes.Execution.Nodes.ValueInput<int>)?.Value.Value = 2;
            (thisNumberStyleNode as FrooxEngine.ProtoFlux.Runtimes.Execution.Nodes.ValueInput<NumberStyles>)?.Value.Value = NumberStyles.HexNumber;

            // Positions
            float3 baseUp = nodeSlot.Up;
            float3 baseRight = nodeSlot.Right;

            void LocalTransformNode(ProtoFluxNode input, float X, float Y)
            {
              Slot target = input.Slot;
              target.CopyTransform(nodeSlot);
              target.Parent = nodeSlot.Parent;
              target.GlobalPosition += (baseUp * Y) + (baseRight * X);
            }

            LocalTransformNode(thisRefIDObjectCastNode, 0.09f, -0.00375f);

            LocalTransformNode(thisToStringNode, 0.18f, -0.03f);
            LocalTransformNode(thisStringRemoveNode, 0.33f, -0.03f);
            LocalTransformNode(thisParseULongNode, 0.495f, -0.03f);

            LocalTransformNode(thisLengthInputNode, 0.18f, -0.135f);
            LocalTransformNode(thisNumberStyleNode, 0.27f, 0.075f);

            node.World.EndUndoBatch();

            ProtoFluxNode?[] allNodes = [node, thisRefIDObjectCastNode, thisToStringNode, thisStringRemoveNode, thisParseULongNode, thisLengthInputNode, thisNumberStyleNode];
            foreach (var node in allNodes)
            {
              if (node == null) continue;
              if (node.IsRemoved) continue;
              node.Slot.GetComponent<Grabbable>().Enabled = false;
            }
            var tempGrab = tempSlot.AttachComponent<Grabbable>();

            await new Updates(240);
            int i = 0;
            while (tempGrab.IsGrabbed && i < 200)
            {
              await new Updates(5);
              i++;
            }
            foreach (var node in allNodes)
            {
              if (node == null) continue;
              if (node.IsRemoved) continue;
              node.Slot.GetComponent<Grabbable>().Enabled = true;
            }

            tempSlot.Destroy(origParent);
          });

          return true;
        }
      );
    }

    if (outputType == typeof(bool) || outputType == typeof(bool2) || outputType == typeof(bool3) || outputType == typeof(bool4))
    {
      yield return new(psuedoGenericTypes.AND.First(n => n.Types.First() == outputType).Node);
      yield return new(psuedoGenericTypes.OR.First(n => n.Types.First() == outputType).Node);
      yield return new(psuedoGenericTypes.NOT.First(n => n.Types.First() == outputType).Node);

      if (outputType != typeof(bool))
      {
        yield return new(psuedoGenericTypes.All.First(n => n.Types.First() == outputType).Node);
        yield return new(psuedoGenericTypes.Any.First(n => n.Types.First() == outputType).Node);
        yield return new(psuedoGenericTypes.None.First(n => n.Types.First() == outputType).Node);
      }
    }

    if (outputType.IsEnum)
    {
      yield return new MenuItem(typeof(NextValue<>).MakeGenericType(outputType), name: typeof(NextValue<>).GetNiceName());
      yield return new MenuItem(typeof(ShiftEnum<>).MakeGenericType(outputType), name: typeof(ShiftEnum<>).GetNiceName());
      yield return new MenuItem(typeof(TryEnumToInt<>).MakeGenericType(outputType), name: "TryEnumToInt<T>");

      var enumType = outputType.GetEnumUnderlyingType();
      if (NodeUtils.TryGetEnumToNumberNode(enumType, out var toNumberType))
      {
        yield return new MenuItem(toNumberType.MakeGenericType(outputType));
      }
    }

    if (TypeUtils.MatchInterface(outputType, typeof(IQuantity<>), out var quantityType))
    {
      var baseType = quantityType.GenericTypeArguments[0];
      yield return new MenuItem(typeof(BaseValue<>).MakeGenericType(baseType));
      yield return new MenuItem(typeof(FormatQuantity<>).MakeGenericType(baseType));
    }

    if (TypeUtils.MatchInterface(outputType, typeof(ICollider), out _))
    {
      yield return new MenuItem(typeof(IsCharacterController));
      yield return new MenuItem(typeof(AsCharacterController));
    }

    if (TypeUtils.MatchesType(typeof(IValue<>), outputType))
    {
      var typeArg = outputType.GenericTypeArguments[0];
      yield return new MenuItem(typeof(FieldAsVariable<>).MakeGenericType(typeArg));
    }

    if (TypeUtils.MatchesType(typeof(ISyncRef<>), outputType))
    {
      var typeArg = outputType.GenericTypeArguments[0];
      yield return new MenuItem(typeof(ReferenceInterfaceAsVariable<>).MakeGenericType(typeArg));
    }

    if (TypeUtils.MatchesType(typeof(SyncRef<>), outputType))
    {
      var typeArg = outputType.GenericTypeArguments[0];
      yield return new MenuItem(typeof(ReferenceAsVariable<>).MakeGenericType(typeArg));
      yield return new MenuItem(typeof(ReferenceTarget<>).MakeGenericType(typeArg));
    }

    if (TypeUtils.MatchInterface(outputType, typeof(IAssetProvider<AudioClip>), out _))
    {
      yield return new MenuItem(typeof(PlayOneShot));
    }

    if (typeof(IComponent).IsAssignableFrom(outputType))
    {
      yield return new MenuItem(typeof(GetSlot));
    }

    if (typeof(IGrabbable).IsAssignableFrom(outputType))
    {
      yield return new MenuItem(typeof(IsGrabbableGrabbed));
      yield return new MenuItem(typeof(IsGrabbableScalable));
      yield return new MenuItem(typeof(IsGrabbableReceivable));
      yield return new MenuItem(typeof(GrabbablePriority));
      yield return new MenuItem(typeof(GrabbableGrabber));
    }

    if (TypeUtils.MatchInterface(outputType, typeof(IAssetProvider<>), out var assetProviderType))
    {
      yield return new MenuItem(typeof(GetAsset<>).MakeGenericType(assetProviderType.GenericTypeArguments[0]));
    }

    if (outputType == typeof(int))
    {
      yield return new MenuItem(typeof(ImpulseMultiplexer), name: "Impulse Multiplex", group: "Comparisons/Selection");
    }

    var multiplexNode = GetNodeForType(outputType, [
      new NodeTypeRecord(typeof(ValueMultiplex<>), null, null),
      new NodeTypeRecord(typeof(ObjectMultiplex<>), null, null),
    ]);
    var indexOfFirstMatchNode = GetNodeForType(outputType, [
      new NodeTypeRecord(typeof(IndexOfFirstValueMatch<>), null, null),
      new NodeTypeRecord(typeof(IndexOfFirstObjectMatch<>), null, null),
    ]);
    yield return new MenuItem(multiplexNode, group: "Comparisons/Selection");
    yield return new MenuItem(indexOfFirstMatchNode, group: "Comparisons/Selection");

    if (nodeType == typeof(DataModelBooleanToggle) && outputType == typeof(bool))
    {
      yield return new(typeof(FireOnLocalValueChange<bool>));
    }

    if (Groups.MousePositionGroup.Contains(nodeType))
    {
      foreach (var node in Groups.ScreenPointGroup)
      {
        yield return new(node);
      }
    }

    if (Groups.WorldTimeFloatGroup.Contains(nodeType))
    {
      yield return new MenuItem(typeof(Sin_Float));
    }
    else if (Groups.WorldTimeDoubleGroup.Contains(nodeType))
    {
      yield return new MenuItem(typeof(Sin_Double));
    }

    if (TypeUtils.MatchesType(typeof(EnumToInt<>), nodeType) || TypeUtils.MatchesType(typeof(TryEnumToInt<>), nodeType))
    {
      yield return new MenuItem(typeof(ValueMultiplex<dummy>));
    }

    if (nodeType == typeof(CountOccurrences) || nodeType == typeof(ChildrenCount) || nodeType == typeof(WorldUserCount))
    {
      yield return new MenuItem(typeof(For));
    }

    if (ContextualSwapActionsPatch.DeltaTimeGroup.Contains(nodeType.GetGenericTypeDefinitionOrSameType()))
    {
      foreach (var dtOperationType in ContextualSwapActionsPatch.DeltaTimeOperationGroup)
      {
        yield return new MenuItem(dtOperationType.MakeGenericType(typeof(float)));
      }
    }

    var outputNode = outputProxy.Node.Target.NodeInstance;
    Type? nodeVariable = GetIVariableValueType(outputNode.GetType());

    if (nodeVariable != null)
    {
      MenuItem createVariableNode(Type node, string name, bool connectNode = false)
      {
        return new MenuItem(
          node,
          name: name,
          onNodeSpawn: (ProtoFluxNode newNode, ProtoFluxElementProxy proxy, ProtoFluxTool _) =>
          {
            ProtoFluxOutputProxy output = (ProtoFluxOutputProxy)proxy;

            ISyncRef targetRef = newNode.GetReference(0);

            newNode.TryConnectReference(targetRef, outputProxy.Node.Target, undoable: true);

            return connectNode;
          },
          group: "Variables"
        );
      }
      var variableInput = GetNodeForType(nodeVariable, [
        new NodeTypeRecord(typeof(ValueWrite<>), null, null),
        new NodeTypeRecord(typeof(ObjectWrite<>), null, null),
      ]);
      var variableLatchInput = GetNodeForType(nodeVariable, [
        new NodeTypeRecord(typeof(ValueWriteLatch<>), null, null),
        new NodeTypeRecord(typeof(ObjectWriteLatch<>), null, null),
      ]);
      yield return createVariableNode(variableInput, "Write");
      yield return createVariableNode(variableLatchInput, "Write Latch");

      // todo: figure out ValueIncrement<> and ValueDecrement<> and why they never spawn properly
    }
    else
    {
      var variableInput = GetNodeForType(outputType, [
        new NodeTypeRecord(typeof(ValueWrite<>), null, null),
        new NodeTypeRecord(typeof(ObjectWrite<>), null, null),
      ]);
      var variableLatchInput = GetNodeForType(outputType, [
        new NodeTypeRecord(typeof(ValueWriteLatch<>), null, null),
        new NodeTypeRecord(typeof(ObjectWriteLatch<>), null, null),
      ]);
      yield return new MenuItem(variableInput, group: "Variables");
      yield return new MenuItem(variableLatchInput, group: "Variables");
    }
  }
  #endregion

  /// <summary>
  /// Generates menu items when holding an input wire.
  /// </summary>
  /// <param name="inputProxy"></param>
  /// <returns></returns>
  internal static IEnumerable<MenuItem> InputMenuItems(ProtoFluxInputProxy inputProxy)
  {
    var world = inputProxy.World;
    var inputType = inputProxy.InputType.Value;
    var nodeType = inputProxy.Node.Target.NodeType;
    var psuedoGenericTypes = world.GetPsuedoGenericTypesForWorld();

    // one level deep check
    var nodeInstance = inputProxy.Node.Target.NodeInstance;
    var query = new NodeQueryAcceleration(nodeInstance.Runtime.Group);
    var indirectlyConnectsToIterationNode = query.GetEvaluatingNodes(nodeInstance).Any(n => IsIterationNode(n.GetType()));

    if (TryGetPackNode(inputProxy.World, inputType, out var packNodeTypes))
    {
      foreach (var packNodeType in packNodeTypes)
      {
        yield return new MenuItem(packNodeType);
      }
    }

    if (inputType == typeof(float))
    {
      foreach (var worldTimeType in Groups.WorldTimeFloatGroup)
      {
        yield return new MenuItem(worldTimeType, group: "Time");
      }
      yield return new MenuItem(typeof(DeltaTime), group: "Time");
    }

    if (inputType == typeof(string))
    {
      yield return new MenuItem(typeof(FormatString));
      yield return new MenuItem(typeof(ToString_object));
    }
    else if (inputType == typeof(User))
    {
      // Select a User in the current session
      List<User> users = [];
      inputProxy.Slot.World.GetUsers(users);
      foreach (User user in users)
      {
        yield return new MenuItem(
          typeof(FrooxEngine.ProtoFlux.Runtimes.Execution.Nodes.RefObjectInput<User>),
          name: user.UserName,
          onNodeSpawn: (node, proxy, tool) =>
          {
            var comp = node.Slot.GetComponent<FrooxEngine.ProtoFlux.Runtimes.Execution.Nodes.RefObjectInput<User>>();
            comp.Target.Target = user;
            return true;
          },
          binding: typeof(FrooxEngine.ProtoFlux.Runtimes.Execution.Nodes.RefObjectInput<User>),
          group: "User List"
        );
      }

      yield return new MenuItem(typeof(LocalUser));
      yield return new MenuItem(typeof(HostUser));
      yield return new MenuItem(typeof(UserFromUsername), group: "User From");
      yield return new MenuItem(typeof(UserFromID), group: "User From");
      yield return new MenuItem(typeof(GetActiveUser));
      yield return new MenuItem(typeof(GetActiveUserSelf));

      yield return new MenuItem(
        typeof(AllocatingUser),
        name: "Allocating User",
        group: "User From",
        binding: typeof(FrooxEngine.ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.References.AllocatingUser)
      );
    }

    else if (inputType == typeof(UserRoot))
    {
      yield return new MenuItem(typeof(GetActiveUserRoot));
      yield return new MenuItem(typeof(LocalUserRoot));
      yield return new MenuItem(typeof(UserUserRoot));
    }

    else if (inputType == typeof(bool))
    {
      yield return new MenuItem(typeof(ValueEquals<int>));
      yield return new MenuItem(typeof(AND_Bool));
      yield return new MenuItem(typeof(NOT_Bool));

      // Sometimes this can be really helpful to have around
      yield return new MenuItem(typeof(DataModelBooleanToggle));
    }

    else if (inputType == typeof(DateTime))
    {
      yield return new MenuItem(typeof(UtcNow));
      yield return new MenuItem(typeof(FromUnixMilliseconds));
    }

    else if (inputType == typeof(TimeSpan))
    {
      yield return new MenuItem(typeof(Parse_TimeSpan));
      yield return new MenuItem(typeof(TimeSpanFromTicks));
      yield return new MenuItem(typeof(TimeSpanFromMilliseconds));
      yield return new MenuItem(typeof(TimeSpanFromSeconds));
      yield return new MenuItem(typeof(TimeSpanFromMinutes));
      yield return new MenuItem(typeof(TimeSpanFromHours));
      yield return new MenuItem(typeof(TimeSpanFromDays));
    }

    else if (inputType == typeof(Slot))
    {
      yield return new MenuItem(typeof(RootSlot));
      yield return new MenuItem(typeof(LocalUserSlot));
      yield return new MenuItem(typeof(LocalUserSpace));
    }

    else if (inputType == typeof(BoundingBox))
    {
      yield return new MenuItem(typeof(ComputeBoundingBox));
      yield return new MenuItem(typeof(FromCenterSize));
      yield return new MenuItem(typeof(Empty));
      yield return new MenuItem(typeof(EncapsulateBounds));
      yield return new MenuItem(typeof(EncapsulatePoint));
      yield return new MenuItem(typeof(TransformBounds));
    }

    else if (inputType == typeof(CharacterController))
    {
      yield return new MenuItem(typeof(FindCharacterControllerFromSlot));
      yield return new MenuItem(typeof(FindCharacterControllerFromUser));
    }

    else if (inputType == typeof(Type))
    {
      yield return new MenuItem(typeof(GetType));
    }

    else if (inputType == typeof(Chirality))
    {
      yield return new MenuItem(typeof(BodyNodeChirality));
      yield return new MenuItem(typeof(ToolEquippingSide));
    }

    else if (inputType == typeof(BodyNode))
    {
      yield return new MenuItem(typeof(GrabberBodyNode));
    }

    else if (inputType == typeof(Grabber))
    {
      yield return new MenuItem(typeof(GetUserGrabber));
      yield return new MenuItem(typeof(GrabbableGrabber));
    }

    else if (inputType == typeof(Uri))
    {
      yield return new MenuItem(typeof(StringToAbsoluteURI));
    }

    else if (TypeUtils.MatchInterface(inputType, typeof(IQuantity<>), out var quantityType))
    {
      var baseType = quantityType.GenericTypeArguments[0];
      yield return new MenuItem(typeof(FromBaseValue<>).MakeGenericType(baseType));
      yield return new MenuItem(typeof(ParseQuantity<>).MakeGenericType(baseType));
    }

    else if (nodeType == typeof(ValueMul<floatQ>) && inputProxy.ElementName == "B")
    {
      yield return new MenuItem(typeof(GetForward), overload: true);
      // yield return new MenuItem(
      //     name: "ValueInput<float>",
      //     node: typeof(ExternalValueInput<FrooxEngineContext, float3>),
      //     binding: typeof(FrooxEngine.ProtoFlux.Runtimes.Execution.Nodes.ValueInput<float3>),
      //     overload: true
      // );
    }
    else if (inputType == typeof(float3))
    {
      yield return new MenuItem(typeof(GetForward), group: "Directions");
      yield return new MenuItem(typeof(GetBackward), group: "Directions");
      yield return new MenuItem(typeof(GetUp), group: "Directions");
      yield return new MenuItem(typeof(GetDown), group: "Directions");
      yield return new MenuItem(typeof(GetLeft), group: "Directions");
      yield return new MenuItem(typeof(GetRight), group: "Directions");
    }

    else if (inputType == typeof(int) && (IsIterationNode(nodeType) || indirectlyConnectsToIterationNode))
    {
      //yield return new MenuItem(typeof(ValueInc<int>));
      //yield return new MenuItem(typeof(ValueDec<int>));
      yield return new MenuItem(typeof(ChildrenCount));
      yield return new MenuItem(typeof(CountOccurrences));
    }

    if (inputProxy.ElementName == nameof(LocalScreenPointToDirection.NormalizedScreenPoint))
    {
      yield return new MenuItem(typeof(NormalizedMousePosition));
    }

    if (TypeUtils.MatchInterface(inputType, typeof(IAsset), out _))
    {
      yield return new MenuItem(typeof(GetAsset<>).MakeGenericType(inputType));
    }

    if (inputType.IsEnum)
    {
      // yield return new MenuItem(typeof(NextValue<>).MakeGenericType(inputType));
      // yield return new MenuItem(typeof(ShiftEnum<>).MakeGenericType(inputType));

      var enumType = inputType.GetEnumUnderlyingType();
      if (NodeUtils.TryGetNumberToEnumNode(enumType, out var toNumberType))
      {
        yield return new MenuItem(toNumberType.MakeGenericType(inputType));
      }
    }

    if (inputType == typeof(int) && (
        typeof(ValueMultiplex<>).IsAssignableFrom(nodeType)
        || typeof(ObjectMultiplex<>).IsAssignableFrom(nodeType)
        || typeof(ValueDemultiplex<>).IsAssignableFrom(nodeType)
        || typeof(ObjectDemultiplex<>).IsAssignableFrom(nodeType)))
    {
      yield return new MenuItem(typeof(ImpulseDemultiplexer), name: "Impulse Demultiplexer");
      yield return new MenuItem(typeof(IndexOfFirstValueMatch<dummy>));
    }


    if (TypeUtils.MatchesType(typeof(ValueMul<>), nodeType))
    {
      var atan2Type = TryGetPsuedoGenericForType(inputProxy.World, "Atan2_", nodeType.GenericTypeArguments[0]);
      var nodeHasAtan2Connection = inputProxy.Node.Target.NodeInstance.AllInputElements().Any(i => i.Source is IOutput source && source.OwnerNode.GetType() == atan2Type);
      if (nodeHasAtan2Connection)
      {
        yield return new MenuItem(typeof(RadToDeg), overload: true);
      }
    }

    // todo: playoneshot group
    if ((nodeType == typeof(PlayOneShot) || nodeType == typeof(PlayOneShotAndWait)) && inputProxy.ElementName == "Speed")
    {
      yield return new MenuItem(typeof(RandomFloat));
    }

    // Can be swapped to Local or Store at any point
    var variableInput = GetNodeForType(inputType, [
      new NodeTypeRecord(typeof(DataModelValueFieldStore<>), null, null),
      new NodeTypeRecord(typeof(DataModelObjectFieldStore<>), null, null),
      new NodeTypeRecord(typeof(DataModelObjectRefStore<>), null, null),
      new NodeTypeRecord(typeof(StoredObject<>), null, null),
    ]);
    yield return new MenuItem(variableInput);

    var dynVariableInput = GetNodeForType(inputType, [
      new NodeTypeRecord(typeof(DynamicVariableValueInput<>), null, null),
      new NodeTypeRecord(typeof(DynamicVariableObjectInput<>), null, null),
    ]);
    var spatialVariableInput = GetNodeForType(inputType, [
      new NodeTypeRecord(typeof(SampleValueSpatialVariable<>), null, null),
      new NodeTypeRecord(typeof(SampleObjectSpatialVariable<>), null, null),
    ]);

    yield return new MenuItem(dynVariableInput);
    yield return new MenuItem(spatialVariableInput);
    
    if (psuedoGenericTypes.Parse.Any(n => n.Types.First() == inputType))
    {
      yield return new(psuedoGenericTypes.Parse.First(n => n.Types.First() == inputType).Node);
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