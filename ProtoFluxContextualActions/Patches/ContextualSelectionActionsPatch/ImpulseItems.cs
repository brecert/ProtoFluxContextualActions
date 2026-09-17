using FrooxEngine.ProtoFlux;

using ProtoFlux.Runtimes.Execution.Nodes;
using ProtoFlux.Runtimes.Execution.Nodes.Actions;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Assets;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Async;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Audio;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Debugging;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Rendering;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Slots;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Transform;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Undo;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Variables;

namespace ProtoFluxContextualActions.Patches;


static partial class ContextualSelectionActionsPatch
{
  private static IEnumerable<MenuItem> ImpulseMenuItems(ProtoFluxImpulseProxy impulseProxy)
  {
    var nodeType = impulseProxy.Node.Target.NodeType;

    // TODO: convert to while?
    yield return new(typeof(For), group: "Loops");
    yield return new(typeof(If));
    yield return new(typeof(Sequence));
    yield return new(typeof(While), group: "Loops");

    yield return new(typeof(ValueWrite<int>));
    yield return new(typeof(ValueWrite<int>), group: "Variables"); // while using dummy works, having int be the default is better (and its more consistent)

    yield return new(typeof(ImpulseMultiplexer), name: "Impulse Multiplex", group: "Selection");
    yield return new(typeof(ImpulseDemultiplexer), name: "Impulse Demultiplex", group: "Selection");

    yield return new(typeof(DynamicImpulseTrigger), name: "Dynamic Impulse Trigger", group: "Actions");
    yield return new(typeof(PlayOneShot), group: "Actions");

    yield return new(typeof(StartAsyncTask), group: "Async");
    yield return new(typeof(AsyncFor), group: "Async/Loops");
    yield return new(typeof(AsyncWhile), group: "Async/Loops");
    yield return new(typeof(AsyncSequence), group: "Async");
    yield return new(typeof(DelayUpdates), group: "Async");
    yield return new(typeof(DelaySecondsFloat), group: "Async");
    yield return new(typeof(AsyncDynamicImpulseTrigger), group: "Async");

    yield return new(typeof(DataModelBooleanToggle), group: "Variables");

    yield return new(typeof(DebugSphere), group: "Actions/Debug");
    yield return new(typeof(DebugVector), group: "Actions/Debug");
    yield return new(typeof(DebugAxes), group: "Actions/Debug");
    yield return new(typeof(DebugLine), group: "Actions/Debug");
    yield return new(typeof(DebugText), group: "Actions/Debug");
    yield return new(typeof(DebugTriangle), group: "Actions/Debug");
    yield return new(typeof(DebugBox), group: "Actions/Debug");

    yield return new(typeof(BeginUndoBatch), group: "Actions/Undo");
    yield return new(typeof(EndUndoBatch), group: "Actions/Undo");
    yield return new(typeof(CreateUndoBatch), group: "Actions/Undo");
    yield return new(typeof(UndoableDestroy), group: "Actions/Undo");
    yield return new(typeof(CreateFieldUndoStep), group: "Actions/Undo");
    yield return new(typeof(CreateReferenceUndoStep), group: "Actions/Undo");
    yield return new(typeof(CreateSpawnUndoStep), group: "Actions/Undo");
    yield return new(typeof(CreateTransformUndoStep), group: "Actions/Undo");
    yield return new(typeof(CreateTypeFieldUndoStep), group: "Actions/Undo");

    if (IsIterationNode(nodeType))
    {
      yield return new(typeof(ValueIncrement<int>), group: "Variables");
      yield return new(typeof(ValueDecrement<int>), group: "Variables");
    }

    else if (nodeType == typeof(DuplicateSlot))
    {
      yield return new(typeof(SetGlobalTransform));
      yield return new(typeof(SetLocalTransform));

      yield return new(typeof(SetSlotPersistentSelf));
      yield return new(typeof(SetSlotActiveSelf));
    }

    else if (nodeType == typeof(RenderToTextureAsset))
    {
      yield return new(typeof(AttachTexture2D));
      yield return new(typeof(AttachSprite));
    }

    else if (nodeType.IsGenericType)
    {
      var typeDef = nodeType.GetGenericTypeDefinition();
      if (typeDef == typeof(FireOnValueChange<>) || typeDef == typeof(FireOnObjectValueChange<>) || typeDef == typeof(FireOnLocalValueChange<>) || typeDef == typeof(FireOnLocalObjectChange<>))
      {
        yield return new(typeof(LocalImpulseTimeoutSeconds));
      }
    }

    else if (nodeType == typeof(ImpulseDemultiplexer))
    {
      yield return new(typeof(ImpulseMultiplexer), name: "Impulse Multiplex");
    }
  }
}
