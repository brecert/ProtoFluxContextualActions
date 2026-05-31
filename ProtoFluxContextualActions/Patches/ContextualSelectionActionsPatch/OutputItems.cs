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
        typeof(ObjectCast<Slot, IWorldElement>),
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

    if (outputType == typeof(float3))
    {
      yield return new(typeof(TransformPoint), group: "Vectors");
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

      yield return new MenuItem(typeof(Contains));
      yield return new MenuItem(typeof(Substring));
      yield return new MenuItem(typeof(TrimString));
      yield return new MenuItem(typeof(IsStringEmpty));

      yield return new MenuItem(typeof(FormatString));
      yield return new MenuItem(typeof(ReplaceSubstring));

      yield return new MenuItem(typeof(ProtoFlux.Runtimes.Execution.Nodes.Strings.ToLower));

      yield return new MenuItem(typeof(GetCharacter));

      yield return new MenuItem(typeof(CountOccurrences));

      yield return new MenuItem(typeof(StripRTF_Tags));

      // Multi, Join and Insert can be swapped to
      yield return new MenuItem(typeof(ConcatenateString));

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
      yield return new MenuItem(typeof(GetSide));
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
      else
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