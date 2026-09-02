using System;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.ProtoFlux;

using System.Collections.Generic;
using System.Linq;

using ProtoFlux.Core;
using ProtoFlux.Runtimes.Execution.Nodes;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Transform;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Slots;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Users;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Audio;
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
using ProtoFluxContextualActions.Tagging;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Interaction.Tools;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Input.Keyboard;
using ProtoFlux.Runtimes.Execution.Nodes.Utility.Uris;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Variables;
using FrooxEngine.Undo;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Input.Controllers;
using System.Globalization;
using ProtoFlux.Runtimes.Execution.Nodes.Color;
using ProtoFlux.Runtimes.Execution.Nodes.Casts;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Locomotion;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Playback;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Avatar.Anchors;
using ProtoFlux.Runtimes.Execution.Nodes.Math.Quaternions;
using ProtoFlux.Runtimes.Execution.Nodes.Math.Rects;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Interaction.Focusing;
using ProtoFlux.Runtimes.Execution;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Input.Haptics;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Components;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Elements;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Network;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Animation;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Security;
using System.Reflection;

namespace ProtoFluxContextualActions.Patches;


static partial class ContextualSelectionActionsPatch
{
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
    yield return new MenuItem(outputType == typeof(bool) ? typeof(ValueConditional<int>) : conditionalNode, group: "Comparisons");

    var delayValueNode = GetNodeForType(outputType, [
      new NodeTypeRecord(typeof(DelayValue<>), null, null),
      new NodeTypeRecord(typeof(DelayObject<>), null, null),
    ]);
    yield return new MenuItem(delayValueNode, group: "Comparisons");

    foreach (var collectionItem in CollectionItems(outputType))
    {
      yield return collectionItem;
    }

    if (outputType == typeof(Slot))
    {
      yield return new MenuItem(typeof(GlobalTransform));
      yield return new MenuItem(typeof(GetForward));
      yield return new MenuItem(typeof(Children));
      yield return new MenuItem(typeof(GetChild));
      yield return new MenuItem(typeof(ChildrenCount));
      yield return new MenuItem(typeof(SetSlotActiveSelf));
      yield return new MenuItem(typeof(GetSlotName));

      yield return new MenuItem(typeof(SetSlotPersistentSelf), group: "Slots");

      yield return new MenuItem(typeof(DuplicateSlot));
      yield return new MenuItem(typeof(DestroySlot));

      yield return new MenuItem(typeof(GetParentSlot), group: "Slots");
      yield return new MenuItem(typeof(SetParent));

      yield return new MenuItem(typeof(FindChildByTag), group: "Slots"); // use tag here because it has less inputs which fits better when going to swap.
      yield return new MenuItem(typeof(DestroySlotChildren), group: "Slots");
      yield return new MenuItem(typeof(GetActiveUser));

      yield return new MenuItem(typeof(TransformPoint), group: "Slots");

      yield return new MenuItem(typeof(DynamicImpulseTrigger), group: "Events");

      bool shouldRelay = ProtoFluxContextualActions.ShouldUseRelays;
      Type baseType = shouldRelay ? typeof(ObjectRelay<Slot>) : typeof(ChildrenCount);

      yield return new MenuItem(typeof(AllocatingUser), name: "Allocating User", group: "Slots");

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

    if (outputType == typeof(float3))
    {
      yield return new(typeof(TransformPoint), group: "Vectors");
      yield return new(typeof(FromEuler_floatQ), group: "Vectors");
      yield return new(typeof(AxisAngle_floatQ), group: "Vectors");
      yield return new(typeof(LookRotation_floatQ), group: "Vectors");
    }
    if (outputType == typeof(floatQ))
    {
      yield return new(typeof(TransformRotation), group: "Vectors");
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
      yield return new MenuItem(typeof(StringLength));

      yield return new MenuItem(typeof(IndexOfString));
      yield return new MenuItem(typeof(SplitString));

      yield return new MenuItem(typeof(Contains), group: "Strings");
      yield return new MenuItem(typeof(Contains), group: "Comparisons");
      yield return new MenuItem(typeof(Substring));
      yield return new MenuItem(typeof(TrimString), group: "Strings");
      yield return new MenuItem(typeof(IsStringEmpty), group: "Strings");
      yield return new MenuItem(typeof(IsStringEmpty), group: "Comparisons");

      yield return new MenuItem(typeof(FormatString));
      yield return new MenuItem(typeof(ReplaceSubstring), group: "Strings");

      yield return new MenuItem(typeof(ProtoFlux.Runtimes.Execution.Nodes.Strings.ToLower), group: "Strings");

      yield return new MenuItem(typeof(GetCharacter), group: "Strings");

      yield return new MenuItem(typeof(CountOccurrences), group: "Strings");

      yield return new MenuItem(typeof(StripRTF_Tags), group: "Strings");

      // Multi, Join and Insert can be swapped to
      yield return new MenuItem(typeof(ConcatenateString));

      yield return new MenuItem(typeof(UnescapeString), group: "Strings");
      yield return new MenuItem(typeof(UnescapeUriDataString), group: "Strings");

      yield return new MenuItem(typeof(StringToAbsoluteURI), group: "Strings");
    }
    else if (outputType == typeof(char))
    {
      yield return new MenuItem(typeof(CharToString));
    }
    else if (outputType == typeof(Uri))
    {
      yield return new MenuItem(typeof(GET_String));
      yield return new MenuItem(typeof(FocusWorld));
    }
    else if (typeof(IEnumerable<string>).IsAssignableFrom(outputType))
    {
      yield return new MenuItem(typeof(JoinString));
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

    else if (typeof(ITexture2D).IsAssignableFrom(outputType))
    {
      yield return new(typeof(GetTexture2D_Pixel));
      yield return new(typeof(SampleTexture2D_UV));
      yield return new(typeof(Texture2D_Format));
    }
    else if (typeof(Texture3D).IsAssignableFrom(outputType))
    {
      yield return new(typeof(GetTexture3D_Pixel));
      yield return new(typeof(SampleTexture3D_UVW));
      yield return new(typeof(Texture3D_Format));
    }

    if (TypeUtils.MatchInterface(outputType, typeof(IField<>), out var matchedType))
    {
      Type innerType = matchedType.GenericTypeArguments[0];
      yield return new(typeof(FieldAsVariable<>).MakeGenericType(innerType));

      if (innerType.SupportsConstantLerp() && typeof(TweenValue<>).TryMakeGenericType(innerType) is { } tweenType)
      {
        yield return new(tweenType);
      }

      var fieldHookNode = GetNodeForType(innerType, [
        new NodeTypeRecord(typeof(ValueFieldHook<>), null, null),
        new NodeTypeRecord(typeof(ObjectFieldHook<>), null, null),
      ]);
      yield return new(fieldHookNode);
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
      yield return new MenuItem(typeof(DefaultUserRootScale));
    }

    if (outputType == typeof(User))
    {
      yield return new MenuItem(typeof(UserUsername), group: "Info");
      yield return new MenuItem(typeof(UserUserID), group: "Info");
      yield return new MenuItem(typeof(IsLocalUser), group: "Info");
      yield return new MenuItem(typeof(UserVR_Active), group: "Info");
      yield return new MenuItem(typeof(IsContextMenuOpen), group: "Info");
      yield return new MenuItem(typeof(GeneralHeadset), group: "Info");
      yield return new MenuItem(typeof(UserRootSlot));
      yield return new MenuItem(typeof(UserUserRoot));

      yield return new MenuItem(typeof(FindCharacterControllerFromUser), group: "Info/Sources");

      yield return new MenuItem(typeof(GetActiveLocomotionModule), group: "Info/Sources");

      yield return new MenuItem(typeof(UserFingerPoseSource), group: "Info/Sources");

      yield return new MenuItem(typeof(SwitchLocomotionModule));

      yield return new MenuItem(typeof(DefaultUserScale));

      yield return new MenuItem(typeof(StandardController), group: "Input");
      Type controllerType = GetUserControllerType(Engine.Current.WorldManager.FocusedWorld.LocalUser);
      if (controllerType != typeof(StandardController)) yield return new MenuItem(controllerType, group: "Input");
      // todo: find a way to get the user from the output flux node?
      // if the user isnt null, add the controller type of the user to the list
    }

    if (psuedoGenericTypes.PackTangentPoint2.Any(t => t.Node == nodeType))
    {
      Type tangentType = psuedoGenericTypes.PackTangentPoint2.First(t => t.Node == nodeType).Types.First();
      yield return new MenuItem(psuedoGenericTypes.BezierCurve.First(t => t.Types.First() == tangentType).Node);
    }

    if (outputType == typeof(BodyNode))
    {
      yield return new MenuItem(typeof(BodyNodeSlot));
      yield return new MenuItem(typeof(BodyNodeChirality));
      yield return new MenuItem(typeof(OtherSide));
      yield return new MenuItem(typeof(RelativeBodyNode));
      yield return new MenuItem(typeof(GetSide));

      yield return new MenuItem(typeof(ReleaseAllGrabbed));
    }

    if (outputType == typeof(Grabber))
    {
      yield return new MenuItem(typeof(GrabberBodyNode));
      yield return new MenuItem(typeof(GrabbedGrabbables));
    }

    if (outputType == typeof(CharacterController))
    {
      yield return new MenuItem(typeof(CharacterLinearVelocity), group: "Velocity");
      yield return new MenuItem(typeof(IsCharacterOnGround), group: "State");
      yield return new MenuItem(typeof(CharacterControllerUser), group: "State");

      yield return new MenuItem(typeof(CharacterGravity), group: "Gravity");
      yield return new MenuItem(typeof(SetCharacterGravity), group: "Gravity");


      yield return new MenuItem(typeof(CharacterGroundCollider), group: "State");

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

    if (typeof(IFingerPoseSourceComponent).IsAssignableFrom(outputType))
    {
      yield return new MenuItem(typeof(FingerPose));
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

    else
    {
      if (psuedoGenericTypes.ObjToString.Any(n => n.Types.First() == outputType))
      {
        yield return new(psuedoGenericTypes.ObjToString.First(n => n.Types.First() == outputType).Node, group: "Casts");
      }
      else if (outputType != typeof(string))
      {
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

    if (outputType == typeof(JoinRequestHandle))
    {
      yield return new MenuItem(typeof(AllowJoin));
      yield return new MenuItem(typeof(DenyJoin));
      yield return new MenuItem(typeof(AssignRole));
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
      if (outputType.IsUnmanaged() || typeof(ISphericalHarmonics).IsAssignableFrom(outputType))
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

    if (outputType.IsAssignableTo(typeof(IWorldElement)))
    {
      yield return new MenuItem(typeof(ReferenceID), group: "Casts");
    }

    if (outputType == typeof(IWorldElement))
    {
      yield return new MenuItem(typeof(IsRemoved));
      if (ProtoFluxContextualActions.ShouldDisplayUnsupportedActions)
      {
        yield return new MenuItem(typeof(ReferenceID));
        yield return new MenuItem(
          typeof(ReferenceID),
          name: "RefID -> ULong",
          onNodeSpawn: (node, proxy, tool) =>
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

              ProtoFluxNode? SpawnNode(Type nodeType)
              {
                return tool.SpawnNode(nodeType, node => node.EnsureVisual());
              }

              ProtoFluxNode? refObjCast = SpawnNode(refIDObjectCastNode);
              ProtoFluxNode? toStr = SpawnNode(toStringNode);
              ProtoFluxNode? strRemove = SpawnNode(stringRemoveNode);
              ProtoFluxNode? parseULong = SpawnNode(parseULongNode);
              ProtoFluxNode? lenInput = SpawnNode(lengthInputNode);
              ProtoFluxNode? styleInput = SpawnNode(numberStyleNode);

              ProtoFluxNode?[] nodes = [node, refObjCast, toStr, strRemove, parseULong, lenInput, styleInput];

              await new Updates(6);

              var nodeSlot = node.Slot;
              var origParent = nodeSlot.Parent;
              var tempSlot = origParent.AddSlot("Temp Flux Holder", false);
              tempSlot.CopyTransform(nodeSlot);
              nodeSlot.Parent = tempSlot;

              if (nodes.Any(n => n == null))
              {
                foreach (var node in nodes)
                {
                  node?.Slot.Destroy();
                }
                return;
              }

              node.World.BeginUndoBatch("Create RefID -> ULong");

              foreach (var node in nodes)
              {
                node!.Slot.CreateSpawnUndoPoint("Spawn Node");
              }

              // Inputs and outputs
              INodeOutput inputRelay = node.GetOutput(0);

              ISyncRef refIDInstance = refObjCast!.GetInput(0);
              INodeOutput refIDValue = refObjCast.GetOutput(0);
              ISyncRef objectInstance = toStr!.GetInput(0);
              INodeOutput objectValue = toStr.GetOutput(0);
              ISyncRef stringRemoveInstance = strRemove!.GetInput(0);
              ISyncRef stringRemoveLength = strRemove.GetInput(2);
              INodeOutput stringRemoveValue = strRemove.GetOutput(0);
              ISyncRef parseULongInstance = parseULong!.GetInput(0);
              ISyncRef parseULongStyle = parseULong.GetInput(1);

              INodeOutput lengthValue = lenInput!.GetOutput(0);
              INodeOutput numberStylesValue = styleInput!.GetOutput(0);

              refIDInstance.Target = inputRelay;
              objectInstance.Target = refIDValue;

              stringRemoveInstance.Target = toStr;
              parseULongInstance.Target = stringRemoveValue;

              stringRemoveLength.Target = lengthValue;
              parseULongStyle.Target = numberStylesValue;

              (lenInput as FrooxEngine.ProtoFlux.Runtimes.Execution.Nodes.ValueInput<int>)?.Value.Value = 2;
              (styleInput as FrooxEngine.ProtoFlux.Runtimes.Execution.Nodes.ValueInput<NumberStyles>)?.Value.Value = NumberStyles.HexNumber;

              node.World.EndUndoBatch();

              foreach (var node in nodes)
              {
                if (node == null) continue;
                if (node.IsRemoved) continue;
                node.Slot.GetComponent<Grabbable>().Enabled = false;
              }
              var tempGrab = tempSlot.AttachComponent<Grabbable>();

              // for fixing prints that snap the nodes early
              await new Updates(6);

              // Positions
              void setPositions()
              {
                float3 baseUp = nodeSlot.Up;
                float3 baseRight = nodeSlot.Right;

                void LocalTransformNode(ProtoFluxNode input, float X, float Y)
                {
                  Slot target = input.Slot;
                  target.CopyTransform(nodeSlot);
                  target.Parent = nodeSlot.Parent;
                  target.GlobalPosition += (baseUp * Y) + (baseRight * X);
                }

                LocalTransformNode(refObjCast, 0.09f, -0.00375f);

                LocalTransformNode(toStr, 0.18f, -0.03f);
                LocalTransformNode(strRemove, 0.33f, -0.03f);
                LocalTransformNode(parseULong, 0.495f, -0.03f);

                LocalTransformNode(lenInput, 0.18f, -0.135f);
                LocalTransformNode(styleInput, 0.27f, 0.075f);
              }
              setPositions();

              await new Updates(ProtoFluxContextualActions.StructureReleaseUpdates);

              int i = 0;
              while (tempGrab.IsGrabbed && i < 200)
              {
                await new Updates(5);
                i++;
              }
              foreach (var node in nodes)
              {
                if (node == null) continue;
                if (node.IsRemoved) continue;
                var nodeGrabbable = node.Slot.GetComponent<Grabbable>();
                nodeGrabbable.Enabled = true;
                // send a drop event, just in case
                typeof(Grabbable).GetMethod("RunGrabEvent", BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(nodeGrabbable, [true]);
              }

              tempSlot.Destroy(origParent);

              // set positions again after everything / make lightprint not break            
              await new Updates(3);

              setPositions();
            });

            return true;
          }
        );
      }
    }

    if (typeof(IPlayable).IsAssignableFrom(outputType))
    {
      yield return new(typeof(Play));
      yield return new(typeof(Pause));
      yield return new(typeof(Resume));
      yield return new(typeof(Stop));

      yield return new(typeof(Wait), group: "Async");
      yield return new(typeof(PlayAndWait), group: "Async");

      yield return new(typeof(Position), group: "State");
      yield return new(typeof(SetPosition), group: "State");
      yield return new(typeof(ShiftPosition), group: "State");
      yield return new(typeof(NormalizedPosition), group: "State");
      yield return new(typeof(SetNormalizedPosition), group: "State");
      yield return new(typeof(ClipLengthFloat), group: "State");

      yield return new(typeof(Speed), group: "State");
      yield return new(typeof(SetSpeed), group: "State");

      yield return new(typeof(IsPlaying), group: "Playback");
      yield return new(typeof(IsLooped), group: "Playback");
      yield return new(typeof(Toggle), group: "Playback");
      yield return new(typeof(PlaybackState), group: "Playback");
    }

    if (outputType == typeof(SyncPlayback))
    {
      yield return new(typeof(PlaybackDrive), group: "Playback");
    }

    if (typeof(ITool).IsAssignableFrom(outputType))
    {
      yield return new(typeof(EquipTool));
      yield return new(typeof(ToolEquippingSide));
      yield return new(typeof(ToolEquippingSlot));
      yield return new(typeof(IsToolEquipped));
      yield return new(typeof(IsToolInUse));
    }
    if (outputType == typeof(RawDataTool))
    {
      yield return new(typeof(GetRawDataToolHit));
    }

    if (typeof(IFocusable).IsAssignableFrom(outputType))
    {
      yield return new(typeof(HasLocalFocus));
      yield return new(typeof(FocusFocusable));
      yield return new(typeof(DefocusFocusable));
    }

    if (typeof(IComponent).IsAssignableFrom(outputType))
    {
      yield return new(typeof(GetComponentEnabled));
      yield return new(typeof(SetComponentEnabled));
      yield return new(typeof(GetUserFromComponent));
    }

    if (outputType == typeof(Rect))
    {
      yield return new(typeof(EncapsulateRect));
      yield return new(typeof(TranslateRect));
    }
    if (outputType == typeof(Chirality))
    {
      yield return new(typeof(TriggerHapticsOnController));
    }

    if (typeof(IAvatarAnchor).IsAssignableFrom(outputType))
    {
      yield return new(typeof(AnchorUser));
      yield return new(typeof(AnchoredUser));
      yield return new(typeof(IsAnchorOccupied));
      yield return new(typeof(ReleaseUser));
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
      yield return new MenuItem(typeof(PlayOneShotAndWait));
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
            ISyncRef targetRef = newNode.GetReference(0);

            newNode.TryConnectReference(targetRef, outputProxy.Node.Target, false);

            return connectNode;
          },
          group: "Variables"
        );
      }
      if (outputType.TryGetGenericTypeDefinition(out var nodeVarType) && nodeVarType == typeof(IVariable<,>))
      {
        if (nodeVariable.IsUnmanaged())
        {
          yield return new MenuItem(typeof(ValueIndirectWrite<,>).MakeGenericType(outputType.GenericTypeArguments), name: "Indirect Write");
          yield return new MenuItem(typeof(ValueIndirectWriteLatch<,>).MakeGenericType(outputType.GenericTypeArguments), name: "Indirect Write Latch");
        }
        else
        {
          yield return new MenuItem(typeof(ObjectIndirectWrite<,>).MakeGenericType(outputType.GenericTypeArguments), name: "Indirect Write");
          yield return new MenuItem(typeof(ObjectIndirectWriteLatch<,>).MakeGenericType(outputType.GenericTypeArguments), name: "Indirect Write Latch");
        }
      }
      else
      {
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

        if (nodeVariable.IsUnmanaged())
        {
          yield return createVariableNode(typeof(ValueIncrement<,>).MakeGenericType(typeof(FrooxEngineContext), nodeVariable), "Increment");
        }
      }
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
}
