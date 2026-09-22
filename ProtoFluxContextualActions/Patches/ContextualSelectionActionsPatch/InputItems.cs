using Elements.Core;
using Elements.Quantity;

using FrooxEngine;
using FrooxEngine.ProtoFlux;

using ProtoFlux.Core;
using ProtoFlux.Runtimes.Execution.Nodes;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Assets;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Avatar;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Avatar.Anchors;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Avatar.BodyNodes;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Input.Display;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Input.Keyboard;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Input.Mouse;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Interaction;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Interaction.Focusing;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Interaction.Tools;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Locomotion;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Physics;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.References;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Slots;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Time;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Transform;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Users;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Users.LocalScreen;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Variables;
using ProtoFlux.Runtimes.Execution.Nodes.Math.Bounds;
using ProtoFlux.Runtimes.Execution.Nodes.Math.Constants;
using ProtoFlux.Runtimes.Execution.Nodes.Math.Quantity;
using ProtoFlux.Runtimes.Execution.Nodes.Math.Random;
using ProtoFlux.Runtimes.Execution.Nodes.Operators;
using ProtoFlux.Runtimes.Execution.Nodes.ParsingFormatting;
using ProtoFlux.Runtimes.Execution.Nodes.Strings;
using ProtoFlux.Runtimes.Execution.Nodes.TimeAndDate;
using ProtoFlux.Runtimes.Execution.Nodes.Utility;
using ProtoFlux.Runtimes.Execution.Nodes.Utility.Uris;

using ProtoFluxContextualActions.Extensions;
using ProtoFluxContextualActions.Tagging;
using ProtoFluxContextualActions.Utils;

using Renderite.Shared;

using static ProtoFluxContextualActions.Utils.PsuedoGenericUtils;

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
        yield return new(packNodeType);
      }
    }

    if (inputType == typeof(float))
    {
      foreach (var worldTimeType in Groups.WorldTimeFloatGroup)
      {
        yield return new(worldTimeType, group: "Time");
      }
      yield return new(typeof(DeltaTime), group: "Time");
    }

    if (inputType == typeof(string))
    {
      yield return new(typeof(FormatString));
      yield return new(typeof(ToString_object));
    }
    else if (inputType == typeof(User))
    {
      // Select a User in the current session
      List<User> users = [];
      inputProxy.Slot.World.GetUsers(users);
      foreach (var user in users)
      {
        yield return new(
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

      yield return new(typeof(LocalUser));
      yield return new(typeof(HostUser));
      yield return new(typeof(UserFromUsername), group: "User From");
      yield return new(typeof(UserFromID), group: "User From");
      yield return new(typeof(GetActiveUser));
      yield return new(typeof(GetActiveUserSelf));

      yield return new(typeof(NearestUserHead));

      yield return new(
        typeof(AllocatingUser),
        name: "Allocating User",
        group: "User From",
        binding: typeof(FrooxEngine.ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.References.AllocatingUser)
      );
    }

    else if (inputType == typeof(UserRoot))
    {
      yield return new(typeof(GetActiveUserRoot));
      yield return new(typeof(LocalUserRoot));
      yield return new(typeof(UserUserRoot));
    }

    else if (inputType == typeof(bool))
    {
      yield return new(typeof(ValueEquals<int>));
      yield return new(typeof(AND_Bool));
      yield return new(typeof(NOT_Bool));

      // Sometimes this can be really helpful to have around
      yield return new(typeof(DataModelBooleanToggle));

      yield return new(typeof(LeftMousePressed), group: "Input");
      yield return new(typeof(LeftMouseHeld), group: "Input");
      yield return new(typeof(RightMousePressed), group: "Input");
      yield return new(typeof(RightMouseHeld), group: "Input");

      yield return new(typeof(KeyHeld), group: "Input");
    }

    else if (inputType == typeof(float))
    {
      yield return new(typeof(MouseScrollDelta), group: "Input");
      yield return new(typeof(LocalWindowAspectRatio), group: "Input");
    }
    else if (inputType == typeof(float2))
    {
      yield return new(typeof(MouseScrollDelta2D), group: "Input");
      yield return new(typeof(MousePosition), group: "Input");
    }
    else if (inputType == typeof(int2))
    {
      yield return new(typeof(LocalWindowResolution), group: "Input");
      yield return new(typeof(LocalPrimaryResolution), group: "Input");
    }

    else if (inputType == typeof(DateTime))
    {
      yield return new(typeof(UtcNow));
      yield return new(typeof(FromUnixMilliseconds));
    }

    else if (inputType == typeof(TimeSpan))
    {
      yield return new(typeof(Parse_TimeSpan));
      yield return new(typeof(TimeSpanFromTicks));
      yield return new(typeof(TimeSpanFromMilliseconds));
      yield return new(typeof(TimeSpanFromSeconds));
      yield return new(typeof(TimeSpanFromMinutes));
      yield return new(typeof(TimeSpanFromHours));
      yield return new(typeof(TimeSpanFromDays));
    }

    else if (inputType == typeof(Slot))
    {
      yield return new(typeof(RootSlot));
      yield return new(typeof(LocalUserSlot));
      yield return new(typeof(LocalUserSpace));
      yield return new(typeof(UserRootSlot));
    }

    else if (inputType == typeof(BoundingBox))
    {
      yield return new(typeof(ComputeBoundingBox));
      yield return new(typeof(FromCenterSize));
      yield return new(typeof(Empty));
      yield return new(typeof(EncapsulateBounds));
      yield return new(typeof(EncapsulatePoint));
      yield return new(typeof(TransformBounds));
    }

    else if (inputType == typeof(CharacterController))
    {
      yield return new(typeof(FindCharacterControllerFromSlot));
      yield return new(typeof(FindCharacterControllerFromUser));
    }

    else if (inputType == typeof(Type))
    {
      yield return new(typeof(GetType));
    }

    else if (inputType == typeof(Chirality))
    {
      yield return new(typeof(BodyNodeChirality));
      yield return new(typeof(ToolEquippingSide));
    }

    else if (inputType == typeof(BodyNode))
    {
      yield return new(typeof(GrabberBodyNode));
      yield return new(typeof(GetSide));
    }

    else if (inputType == typeof(Grabber))
    {
      yield return new(typeof(GetUserGrabber));
      yield return new(typeof(GrabbableGrabber));
    }

    else if (typeof(IFingerPoseSourceComponent).IsAssignableFrom(inputType))
    {
      yield return new(typeof(UserFingerPoseSource));
    }

    else if (nodeType == typeof(GetLocomotionArchetype))
    {
      yield return new(typeof(GetActiveLocomotionModule));
    }

    else if (inputType == typeof(Uri))
    {
      yield return new(typeof(StringToAbsoluteURI));
    }

    else if (inputType == typeof(Guid))
    {
      yield return new(typeof(ParseGUID));
      yield return new(typeof(RandomGUID));
      yield return new(typeof(EmptyGUID));
    }

    else if (TypeUtils.MatchInterface(inputType, typeof(IQuantity<>), out var quantityType))
    {
      var baseType = quantityType.GenericTypeArguments[0];
      yield return new(typeof(FromBaseValue<>).MakeGenericType(baseType));
      yield return new(typeof(ParseQuantity<>).MakeGenericType(baseType));
    }

    else if (nodeType == typeof(ValueMul<floatQ>) && inputProxy.ElementName == "B")
    {
      yield return new(typeof(GetForward), overload: true);
      // yield return new(
      //     name: "ValueInput<float>",
      //     node: typeof(ExternalValueInput<FrooxEngineContext, float3>),
      //     binding: typeof(FrooxEngine.ProtoFlux.Runtimes.Execution.Nodes.ValueInput<float3>),
      //     overload: true
      // );
    }
    else if (inputType == typeof(float3))
    {
      yield return new(typeof(GetForward), group: "Directions");
      yield return new(typeof(GetBackward), group: "Directions");
      yield return new(typeof(GetUp), group: "Directions");
      yield return new(typeof(GetDown), group: "Directions");
      yield return new(typeof(GetLeft), group: "Directions");
      yield return new(typeof(GetRight), group: "Directions");
    }

    else if (inputType == typeof(int) && (IsIterationNode(nodeType) || indirectlyConnectsToIterationNode))
    {
      //yield return new(typeof(ValueInc<int>));
      //yield return new(typeof(ValueDec<int>));
      yield return new(typeof(ChildrenCount));
      yield return new(typeof(CountOccurrences));
    }

    if (typeof(IAvatarAnchor).IsAssignableFrom(inputType))
    {
      yield return new(typeof(GetUserAnchor));
    }

    if (typeof(IFocusable).IsAssignableFrom(inputType))
    {
      yield return new(typeof(GetActiveFocus));
    }

    if (inputProxy.ElementName == nameof(LocalScreenPointToDirection.NormalizedScreenPoint))
    {
      yield return new(typeof(NormalizedMousePosition));
    }

    if (TypeUtils.MatchInterface(inputType, typeof(IAsset), out _))
    {
      yield return new(typeof(GetAsset<>).MakeGenericType(inputType));
    }

    if (inputType.IsEnum)
    {
      // yield return new(typeof(NextValue<>).MakeGenericType(inputType));
      // yield return new(typeof(ShiftEnum<>).MakeGenericType(inputType));

      var enumType = inputType.GetEnumUnderlyingType();
      if (NodeUtils.TryGetNumberToEnumNode(enumType, out var toNumberType))
      {
        yield return new(toNumberType.MakeGenericType(inputType));
      }
    }

    if (inputType == typeof(int) && (
        typeof(ValueMultiplex<>).IsAssignableFrom(nodeType)
        || typeof(ObjectMultiplex<>).IsAssignableFrom(nodeType)
        || typeof(ValueDemultiplex<>).IsAssignableFrom(nodeType)
        || typeof(ObjectDemultiplex<>).IsAssignableFrom(nodeType)))
    {
      yield return new(typeof(ImpulseDemultiplexer), name: "Impulse Demultiplexer");
      yield return new(typeof(IndexOfFirstValueMatch<dummy>));
    }


    if (TypeUtils.MatchesType(typeof(ValueMul<>), nodeType))
    {
      var atan2Type = TryGetPsuedoGenericForType(inputProxy.World, "Atan2_", nodeType.GenericTypeArguments[0]);
      var nodeHasAtan2Connection = inputProxy.Node.Target.NodeInstance.AllInputElements().Any(i => i.Source is IOutput source && source.OwnerNode.GetType() == atan2Type);
      if (nodeHasAtan2Connection)
      {
        yield return new(typeof(RadToDeg), overload: true);
      }
    }


    // Can be swapped to Local or Store at any point
    // var variableInput = GetNodeForType(inputType, [
    //   new NodeTypeRecord(typeof(DataModelValueFieldStore<>), null, null),
    //   new NodeTypeRecord(typeof(DataModelObjectFieldStore<>), null, null),
    //   new NodeTypeRecord(typeof(DataModelObjectRefStore<>), null, null),
    //   new NodeTypeRecord(typeof(StoredObject<>), null, null),
    // ]);
    // yield return new(variableInput);
    //

    if (inputType == typeof(string))
    {
      var globalOutput = GetNodeForType(inputType, [
        new NodeTypeRecord(typeof(GlobalToValueOutput<>), null, null),
        new NodeTypeRecord(typeof(GlobalToObjectOutput<>), null, null),
      ]);
      yield return new(globalOutput);
    }


    var dynVariableInput = GetNodeForType(inputType, [
      new NodeTypeRecord(typeof(DynamicVariableValueInput<>), null, null),
      new NodeTypeRecord(typeof(DynamicVariableObjectInput<>), null, null),
    ]);

    var spatialVariableInput = GetNodeForType(inputType, [
      new NodeTypeRecord(typeof(SampleValueSpatialVariable<>), null, null),
      new NodeTypeRecord(typeof(SampleObjectSpatialVariable<>), null, null),
    ]);

    if (inputType.IsDataModelType())
    {
      yield return new(dynVariableInput);
      yield return new(spatialVariableInput);
    }


    IEnumerable<(Type Node, IEnumerable<Type> Types)> randomLerp = [.. psuedoGenericTypes.RandomLerp, .. psuedoGenericTypes.RandomSlerp];
    IEnumerable<(Type Node, IEnumerable<Type> Types)> randomColor = [.. psuedoGenericTypes.RandomHue, .. psuedoGenericTypes.RandomRGBA, .. psuedoGenericTypes.RandomGrayscale];

    IEnumerable<(Type Node, IEnumerable<Type> Types)> randomPoint = [new(typeof(RandomPointInCircle), [typeof(float2)]), new(typeof(RandomPointInSphere), [typeof(float3)])];

    IEnumerable<(Type Node, IEnumerable<Type> Types)> allRandom = [.. psuedoGenericTypes.Random, new(typeof(RandomRotation), [typeof(floatQ)]), .. randomLerp, .. randomPoint, .. randomColor];

    if (allRandom.Any(t => t.Types.First() == inputType))
    {
      foreach (var node in allRandom.Where(t => t.Types.First() == inputType))
      {
        yield return new(node.Node, group: "Selection");
      }
    }
    if (inputType.IsEnum)
    {
      yield return new(typeof(RandomEnum<>).MakeGenericType(inputType), name: "Random Enum", group: "Selection");
    }

    var pickRandomNode = GetNodeForType(inputType, [
      new NodeTypeRecord(typeof(PickRandomValue<>), null, null),
      new NodeTypeRecord(typeof(PickRandomObject<>), null, null),
    ]);
    yield return new(pickRandomNode, group: "Selection");

    var multiplexNode = GetNodeForType(inputType, [
      new NodeTypeRecord(typeof(ValueMultiplex<>), null, null),
      new NodeTypeRecord(typeof(ObjectMultiplex<>), null, null),
    ]);
    yield return new(multiplexNode, group: "Selection");

    if (psuedoGenericTypes.Parse.Any(n => n.Types.First() == inputType))
    {
      yield return new(psuedoGenericTypes.Parse.First(n => n.Types.First() == inputType).Node);
    }
  }
}
