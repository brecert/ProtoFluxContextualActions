using FrooxEngine.ProtoFlux;

using System.Collections.Generic;
using ProtoFlux.Runtimes.Execution.Nodes;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Transform;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Slots;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Async;
using ProtoFlux.Runtimes.Execution.Nodes.Actions;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Rendering;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Assets;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Variables;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Debugging;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Audio;

namespace ProtoFluxContextualActions.Patches;


static partial class ContextualSelectionActionsPatch
{
  private static IEnumerable<MenuItem> ImpulseMenuItems(ProtoFluxImpulseProxy impulseProxy)
  {
    var nodeType = impulseProxy.Node.Target.NodeType;

    // TODO: convert to while?
    yield return new MenuItem(typeof(For), group: "Loops");
    yield return new MenuItem(typeof(If));
    yield return new MenuItem(typeof(ValueWrite<int>), group: "Variables"); // while using dummy works, having int be the default is better (and its more consistent)
    yield return new MenuItem(typeof(Sequence));
    yield return new MenuItem(typeof(While), group: "Loops");

    yield return new MenuItem(typeof(ImpulseMultiplexer), name: "Impulse Multiplex");
    yield return new MenuItem(typeof(ImpulseDemultiplexer), name: "Impulse Demultiplex");

    yield return new MenuItem(typeof(DynamicImpulseTrigger), name: "Dynamic Impulse Trigger");
    yield return new MenuItem(typeof(StartAsyncTask), group: "Async");
    yield return new MenuItem(typeof(AsyncFor), group: "Async/Loops");
    yield return new MenuItem(typeof(AsyncWhile), group: "Async/Loops");
    yield return new MenuItem(typeof(AsyncSequence), group: "Async");
    yield return new MenuItem(typeof(DelayUpdates), group: "Async");
    yield return new MenuItem(typeof(DelaySecondsFloat), group: "Async");
    yield return new MenuItem(typeof(AsyncDynamicImpulseTrigger), group: "Async");

    yield return new MenuItem(typeof(DataModelBooleanToggle), group: "Variables");

    yield return new MenuItem(typeof(DebugSphere), group: "Debug");
    yield return new MenuItem(typeof(DebugVector), group: "Debug");
    yield return new MenuItem(typeof(DebugAxes), group: "Debug");
    yield return new MenuItem(typeof(DebugLine), group: "Debug");
    yield return new MenuItem(typeof(DebugText), group: "Debug");
    yield return new MenuItem(typeof(DebugTriangle), group: "Debug");
    yield return new MenuItem(typeof(DebugBox), group: "Debug");


    yield return new MenuItem(typeof(PlayOneShot));

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
}