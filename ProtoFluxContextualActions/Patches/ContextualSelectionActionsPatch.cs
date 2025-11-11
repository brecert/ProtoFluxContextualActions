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

namespace ProtoFluxContextualActions.Patches;

[HarmonyPatchCategory("ProtoFluxTool Contextual Actions"), TweakCategory("Adds 'Contextual Actions' to the ProtoFlux Tool. Pressing secondary while holding a protoflux tool will open a context menu of actions based on what wire you're dragging instead of always spawning an input/display node. Pressing secondary again will spawn out an input/display node like normal.")]
[HarmonyPatch(typeof(ProtoFluxTool), nameof(ProtoFluxTool.OnSecondaryPress))]
internal static class ContextualSelectionActionsPatch
{

  internal readonly struct MenuItem(Type node, Type? binding = null, string? name = null, bool overload = false)
  {
    internal readonly Type node = node;

    internal readonly Type? binding = binding;

    internal readonly string? name = name;

    internal readonly bool overload = overload;

    internal readonly string DisplayName => name ?? NodeMetadataHelper.GetMetadata(node).Name ?? node.GetNiceTypeName();
  }

  internal static bool Prefix(ProtoFluxTool __instance, SyncRef<ProtoFluxElementProxy> ____currentProxy)
  {
    var elementProxy = ____currentProxy.Target;
    var items = MenuItems(__instance).Take(10).ToArray();
    // todo: pages / menu

    if (items.Length != 0)
    {
      if (__instance.LocalUser.IsContextMenuOpen())
      {
        __instance.LocalUser.CloseContextMenu(__instance);
        return true;
      }

      __instance.StartTask(async () =>
      {
        var menu = await __instance.LocalUser.OpenContextMenu(__instance, __instance.Slot);
        Traverse.Create(menu).Field<float?>("_speedOverride").Value = 10; // faster for better swiping

        switch (elementProxy)
        {
          case ProtoFluxInputProxy inputProxy:
            {
              foreach (var item in items)
              {
                AddMenuItem(__instance, menu, inputProxy.InputType.Value.GetTypeColor(), item, addedNode =>
                {
                  if (item.overload)
                  {
                    __instance.StartTask(async () =>
                    {
                      // this is dumb
                      // TODO: investigate why it's needed to avoid the one or two update disconnect issue
                      await new Updates(1);
                      var output = addedNode.GetOutput(0); // TODO: specify
                      elementProxy.Node.Target.TryConnectInput(inputProxy.NodeInput.Target, output, allowExplicitCast: false, undoable: true);
                    });
                  }
                  else
                  {
                    var output = addedNode.NodeOutputs
                      .FirstOrDefault(o => typeof(INodeOutput<>).MakeGenericType(inputProxy.InputType).IsAssignableFrom(o.GetType()))
                      ?? throw new Exception($"Could not find matching output of type '{inputProxy.InputType}' in '{addedNode}'");

                    elementProxy.Node.Target.TryConnectInput(inputProxy.NodeInput.Target, output, allowExplicitCast: false, undoable: true);
                  }
                });
              }
              break;
            }
          case ProtoFluxOutputProxy outputProxy:
            {
              foreach (var item in items)
              {
                AddMenuItem(__instance, menu, outputProxy.OutputType.Value.GetTypeColor(), item, addedNode =>
                {
                  if (item.overload) throw new Exception("Overloading with ProtoFluxOutputProxy is not supported");
                  var input = addedNode.NodeInputs
                    .FirstOrDefault(i => i.TargetType.IsGenericType && (outputProxy.OutputType.Value.IsAssignableFrom(i.TargetType.GenericTypeArguments[0]) || ProtoFlux.Core.TypeHelper.CanImplicitlyConvertTo(outputProxy.OutputType, i.TargetType.GenericTypeArguments[0])))
                    ?? throw new Exception($"Could not find matching input of type '{outputProxy.OutputType}' in '{addedNode}'");

                  __instance.StartTask(async () =>
                  {
                    // this is dumb
                    // TODO: investigate why it's needed for casting to work
                    await new Updates();
                    addedNode.TryConnectInput(input, outputProxy.NodeOutput.Target, allowExplicitCast: false, undoable: true);
                  });
                });
              }
              break;
            }
          case ProtoFluxImpulseProxy impulseProxy:
            {
              foreach (var item in items)
              {
                // the colors should almost always be the same so unique colors are more important maybe?
                AddMenuItem(__instance, menu, item.node.GetTypeColor(), item, n =>
                {
                  if (item.overload) throw new Exception("Overloading with ProtoFluxImpulseProxy is not supported");
                  n.TryConnectImpulse(impulseProxy.NodeImpulse.Target, n.GetOperation(0), undoable: true);
                });
              }
              break;
            }
          case ProtoFluxOperationProxy operationProxy:
            {
              foreach (var item in items)
              {
                AddMenuItem(__instance, menu, item.node.GetTypeColor(), item, n =>
                {
                  if (item.overload) throw new Exception("Overloading with ProtoFluxOperationProxy is not supported");
                  n.TryConnectImpulse(n.GetImpulse(0), operationProxy.NodeOperation.Target, undoable: true);
                });
              }
              break;
            }
          default:
            throw new Exception("found items for unsupported protoflux contextual action type");
        }
      });

      return false;
    }

    return true;
  }

  private static void AddMenuItem(ProtoFluxTool __instance, ContextMenu menu, colorX color, MenuItem item, Action<ProtoFluxNode> setup)
  {
    var nodeMetadata = NodeMetadataHelper.GetMetadata(item.node);
    var label = (LocaleString)item.DisplayName;
    var menuItem = menu.AddItem(in label, (Uri?)null, color);
    menuItem.Button.LocalPressed += (button, data) =>
    {
      var nodeBinding = item.binding ?? ProtoFluxHelper.GetBindingForNode(item.node);
      __instance.SpawnNode(nodeBinding, n =>
      {
        n.EnsureElementsInDynamicLists();
        setup(n);
        __instance.LocalUser.CloseContextMenu(__instance);
        CleanupDraggedWire(__instance);
      });
    };
  }

  // note: if we can build up a graph then we can egraph reduce to make matches like this easier to spot automatically rather than needing to check each one manually
  // todo: detect add + 1 and offer to convert to inc?
  // todo: detect add + 1 or inc and write and offer to convert to increment?

  internal static IEnumerable<MenuItem> MenuItems(ProtoFluxTool __instance)
  {
    var _currentProxy = Traverse.Create(__instance).Field("_currentProxy").GetValue<SyncRef<ProtoFluxElementProxy>>();
    var target = _currentProxy?.Target;

    foreach (var item in GeneralNumericOperationMenuItems(target)) yield return item;

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
    yield return new MenuItem(typeof(For));
    yield return new MenuItem(typeof(If));
    yield return new MenuItem(typeof(ValueWrite<dummy>));
    yield return new MenuItem(typeof(Sequence));

    if (IsIterationNode(nodeType))
    {
      yield return new MenuItem(typeof(ValueIncrement<int>)); // dec can be swapped to?
      yield return new MenuItem(typeof(ValueDecrement<int>)); // dec can be swapped to?
    }

    else if (nodeType == typeof(DuplicateSlot))
    {
      yield return new MenuItem(typeof(SetGlobalTransform));
      yield return new MenuItem(typeof(SetLocalTransform));
    }

    else if (nodeType == typeof(RenderToTextureAsset))
    {
      yield return new MenuItem(typeof(AttachTexture2D));
      yield return new MenuItem(typeof(AttachSprite));
    }

    switch (impulseProxy.ImpulseType.Value)
    {
      case ImpulseType.AsyncCall:
      case ImpulseType.AsyncResumption:
        yield return new MenuItem(typeof(AsyncFor));
        yield return new MenuItem(typeof(AsyncSequence));
        break;
    }
  }

  private static IEnumerable<MenuItem> OperationMenuItems(ProtoFluxOperationProxy operationProxy)
  {
    if (operationProxy.IsAsync)
    {
      yield return new MenuItem(typeof(StartAsyncTask));
    }

    if (operationProxy.Node.Target.NodeName.Contains("Debug"))
    {
      yield return new MenuItem(typeof(Update));
      yield return new MenuItem(typeof(LocalUpdate));
      yield return new MenuItem(typeof(SecondsTimer));
    }
  }

  internal static IEnumerable<MenuItem> GeneralNumericOperationMenuItems(ProtoFluxElementProxy? target)
  {
    {
      // TODO: It's nice to have these work with any node, I think their precedence should be lower than manually specified ones and potentially hidden by default for many types that support but do not need, esp. comparison.
      //       When I'm more sure that Swapping won't world crash I think I can limit comparison to a single node and then swap to the right one as a sort of submenu?
      //       Feels a little weird though, ux is difficult. A custom uix menu could help.
      if (target is ProtoFluxOutputProxy { OutputType.Value: var outputType } && (outputType.IsUnmanaged() || typeof(ISphericalHarmonics).IsAssignableFrom(outputType)))
      {
        var world = target.World;
        var coder = Traverse.Create(typeof(Coder<>).MakeGenericType(outputType));
        var isMatrix = outputType.IsMatrixType();
        var isQuaternion = outputType.IsQuaternionType();
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
            yield return new MenuItem(typeof(ValueNegate<>).MakeGenericType(outputType));
          }

          if (coder.Property<bool>("SupportsMod").Value)
          {
            yield return new MenuItem(typeof(ValueMod<>).MakeGenericType(outputType));
          }

          if (coder.Property<bool>("SupportsAbs").Value && !isMatrix)
          {
            yield return new MenuItem(typeof(ValueAbs<>).MakeGenericType(outputType));
          }

          if (coder.Property<bool>("SupportsComparison").Value)
          {
            // yield return new MenuItem(typeof(ValueLessThan<>).MakeGenericType(outputType));
            // yield return new MenuItem(typeof(ValueLessOrEqual<>).MakeGenericType(outputType));
            // yield return new MenuItem(typeof(ValueGreaterThan<>).MakeGenericType(outputType));
            // yield return new MenuItem(typeof(ValueGreaterOrEqual<>).MakeGenericType(outputType));
            yield return new MenuItem(typeof(ValueEquals<>).MakeGenericType(outputType));
            // yield return new MenuItem(typeof(ValueNotEquals<>).MakeGenericType(outputType));
          }
        }


        if (TryGetInverseNode(outputType, out var inverseNodeType))
        {
          yield return new MenuItem(inverseNodeType);
        }

        if (TryGetTransposeNode(outputType, out var transposeNodeType))
        {
          yield return new MenuItem(transposeNodeType, name: "Transpose");
        }
      }
    }
  }

  #region Output Items
  /// <summary>
  /// Yields menu items when holding an output wire. 
  /// </summary>
  /// <param name="outputProxy"></param>
  /// <returns></returns>
  internal static IEnumerable<MenuItem> OutputMenuItems(ProtoFluxOutputProxy outputProxy)
  {
    var nodeType = outputProxy.Node.Target.NodeType;

    if (TryGetUnpackNode(outputProxy.World, outputProxy.OutputType, out var unpackNodeTypes))
    {
      foreach (var unpackNodeType in unpackNodeTypes)
      {
        yield return new MenuItem(unpackNodeType);
      }
    }
    var outputType = outputProxy.OutputType.Value;

    if (outputType == typeof(Slot))
    {
      yield return new MenuItem(typeof(GlobalTransform));
      yield return new MenuItem(typeof(GetForward));
      yield return new MenuItem(typeof(GetChild));
      yield return new MenuItem(typeof(ChildrenCount));
      yield return new MenuItem(typeof(FindChildByTag)); // use tag here because it has less inputs which fits better when going to swap.
      yield return new MenuItem(typeof(GetSlotName));
    }

    else if (outputType == typeof(bool))
    {
      yield return new MenuItem(typeof(If));
      yield return new MenuItem(typeof(ValueConditional<int>)); // dummy type when // todo: convert to multi?
      yield return new MenuItem(typeof(AND_Bool));
      yield return new MenuItem(typeof(OR_Bool));
      yield return new MenuItem(typeof(NOT_Bool));
    }

    else if (outputType == typeof(string))
    {
      yield return new MenuItem(typeof(GetCharacter));
      yield return new MenuItem(typeof(StringLength));
      yield return new MenuItem(typeof(CountOccurrences));
      yield return new MenuItem(typeof(IndexOfString));
      yield return new MenuItem(typeof(Contains));
      yield return new MenuItem(typeof(Substring));
      yield return new MenuItem(typeof(FormatString));
    }

    else if (outputType == typeof(DateTime))
    {
      yield return new MenuItem(typeof(Sub_DateTime));
      yield return new MenuItem(typeof(Add_DateTime_TimeSpan));
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

    else if (outputType == typeof(int) && (IsIterationNode(nodeType) || nodeType == typeof(IndexOfString)))
    {
      yield return new MenuItem(typeof(ValueInc<int>));
      yield return new MenuItem(typeof(ValueDec<int>));
    }

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
      yield return new MenuItem(typeof(UserUsername));
      yield return new MenuItem(typeof(UserUserID));
      yield return new MenuItem(typeof(IsLocalUser));
      yield return new MenuItem(typeof(UserVR_Active));
      yield return new MenuItem(typeof(UserRootSlot));
      yield return new MenuItem(typeof(UserUserRoot));
    }

    if (outputType == typeof(BodyNode))
    {
      yield return new MenuItem(typeof(BodyNodeSlot));
      yield return new MenuItem(typeof(BodyNodeChirality));
      yield return new MenuItem(typeof(OtherSide));
    }

    if (outputType == typeof(Grabber))
    {
      yield return new MenuItem(typeof(GrabberBodyNode));
    }

    if (outputType == typeof(CharacterController))
    {
      yield return new MenuItem(typeof(CharacterLinearVelocity));
      yield return new MenuItem(typeof(IsCharacterOnGround));
      yield return new MenuItem(typeof(CharacterControllerUser));

      yield return new MenuItem(typeof(SetCharacterVelocity));
      yield return new MenuItem(typeof(SetCharacterGravity));
      yield return new MenuItem(typeof(ApplyCharacterForce));
      yield return new MenuItem(typeof(ApplyCharacterImpulse));
    }

    if (outputType == typeof(Type))
    {
      yield return new MenuItem(typeof(IndexOfFirstObjectMatch<Type>));
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

    if (outputType.IsEnum)
    {
      yield return new MenuItem(typeof(NextValue<>).MakeGenericType(outputType), name: typeof(NextValue<>).GetNiceName());
      yield return new MenuItem(typeof(ShiftEnum<>).MakeGenericType(outputType), name: typeof(ShiftEnum<>).GetNiceName());
      yield return new MenuItem(typeof(TryEnumToInt<>).MakeGenericType(outputType), name: "TryEnumToInt<T>");
      yield return new MenuItem(typeof(ValueEquals<>).MakeGenericType(outputType));

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

    if (outputType == typeof(int) && (
        nodeType == typeof(ImpulseDemultiplexer)
        || TypeUtils.MatchesType(typeof(IndexOfFirstValueMatch<>), nodeType)
        || TypeUtils.MatchesType(typeof(IndexOfFirstObjectMatch<>), nodeType)
        ))
    {
      yield return new MenuItem(typeof(ValueMultiplex<dummy>), name: "Value Multiplex");
      yield return new MenuItem(typeof(ImpulseMultiplexer), name: "Impulse Multiplex");
      yield return new MenuItem(typeof(ValueDemultiplex<dummy>), name: "Value Demultiplex");
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
  }
  #endregion

  /// <summary>
  /// Generates menu items when holding an input wire.
  /// </summary>
  /// <param name="inputProxy"></param>
  /// <returns></returns>
  internal static IEnumerable<MenuItem> InputMenuItems(ProtoFluxInputProxy inputProxy)
  {
    var inputType = inputProxy.InputType.Value;
    var nodeType = inputProxy.Node.Target.NodeType;

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

    if (inputType == typeof(User))
    {
      yield return new MenuItem(typeof(LocalUser));
      yield return new MenuItem(typeof(HostUser));
      yield return new MenuItem(typeof(UserFromUsername));
      yield return new MenuItem(typeof(GetActiveUser));
      yield return new MenuItem(typeof(GetActiveUserSelf));
    }

    else if (inputType == typeof(UserRoot))
    {
      yield return new MenuItem(typeof(GetActiveUserRoot));
      yield return new MenuItem(typeof(LocalUserRoot));
      yield return new MenuItem(typeof(UserUserRoot));
    }

    else if (inputType == typeof(bool))
    {
      // I want to use dummy's here but it's not safe to do so.
      yield return new MenuItem(typeof(ValueLessThan<int>));
      yield return new MenuItem(typeof(ValueLessOrEqual<int>));
      yield return new MenuItem(typeof(ValueGreaterThan<int>));
      yield return new MenuItem(typeof(ValueGreaterOrEqual<int>));
      yield return new MenuItem(typeof(ValueEquals<int>));
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

    else if (nodeType == typeof(Mul_FloatQ_Float3) && inputProxy.ElementName == "B")
    {
      yield return new MenuItem(typeof(GetForward));
      yield return new MenuItem(typeof(GetBackward));
      yield return new MenuItem(typeof(GetUp));
      yield return new MenuItem(typeof(GetDown));
      yield return new MenuItem(typeof(GetLeft));
      yield return new MenuItem(typeof(GetRight));
    }

    else if (inputType == typeof(int) && (IsIterationNode(nodeType) || indirectlyConnectsToIterationNode))
    {
      yield return new MenuItem(typeof(ValueInc<int>));
      yield return new MenuItem(typeof(ValueDec<int>));
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