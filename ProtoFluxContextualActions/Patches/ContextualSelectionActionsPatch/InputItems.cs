using System;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.ProtoFlux;

using static ProtoFluxContextualActions.Utils.PsuedoGenericUtils;
using System.Collections.Generic;
using System.Linq;
using ProtoFlux.Core;
using ProtoFlux.Runtimes.Execution.Nodes;
using ProtoFlux.Runtimes.Execution.Nodes.Operators;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Transform;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Slots;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Users;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Audio;
using ProtoFlux.Runtimes.Execution.Nodes.TimeAndDate;
using ProtoFlux.Runtimes.Execution.Nodes.Strings;
using ProtoFlux.Runtimes.Execution.Nodes.ParsingFormatting;
using ProtoFluxContextualActions.Utils;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.References;
using ProtoFlux.Runtimes.Execution.Nodes.Math.Bounds;
using Elements.Quantity;
using ProtoFlux.Runtimes.Execution.Nodes.Math.Quantity;
using ProtoFlux.Runtimes.Execution.Nodes.Utility;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Assets;
using ProtoFluxContextualActions.Extensions;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Physics;
using Renderite.Shared;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Avatar.BodyNodes;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Interaction;
using ProtoFlux.Runtimes.Execution.Nodes.Math.Constants;
using ProtoFlux.Runtimes.Execution.Nodes.Math.Random;
using ProtoFluxContextualActions.Tagging;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Input.Mouse;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Users.LocalScreen;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Interaction.Tools;
using ProtoFlux.Runtimes.Execution.Nodes.Utility.Uris;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Variables;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Time;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Avatar.Anchors;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Avatar;

namespace ProtoFluxContextualActions.Patches;


static partial class ContextualSelectionActionsPatch
{
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
      yield return new MenuItem(typeof(GetSide));
    }

    else if (inputType == typeof(Grabber))
    {
      yield return new MenuItem(typeof(GetUserGrabber));
      yield return new MenuItem(typeof(GrabbableGrabber));
    }

    else if (typeof(IFingerPoseSourceComponent).IsAssignableFrom(inputType))
    {
      yield return new MenuItem(typeof(UserFingerPoseSource));
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

    if (typeof(IAvatarAnchor).IsAssignableFrom(inputType))
    {
      yield return new(typeof(GetUserAnchor));
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
}