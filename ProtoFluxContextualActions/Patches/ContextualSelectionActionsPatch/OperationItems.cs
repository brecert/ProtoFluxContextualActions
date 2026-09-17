using FrooxEngine.ProtoFlux;

using ProtoFlux.Runtimes.Execution.Nodes.Actions;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Async;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Slots;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Worlds;

namespace ProtoFluxContextualActions.Patches;


static partial class ContextualSelectionActionsPatch
{
  private static IEnumerable<MenuItem> OperationMenuItems(ProtoFluxOperationProxy operationProxy)
  {
    if (operationProxy.IsAsync.Value)
    {
      yield return new(typeof(StartAsyncTask));
      yield return new(typeof(AsyncDynamicImpulseReceiver));
    }
    else
    {
      yield return new(typeof(FireOnTrue));
      yield return new(typeof(FireOnFalse));
      yield return new(typeof(FireOnValueChange<bool>));

      yield return new(typeof(FireWhileTrue), group: "Loops");
      yield return new(typeof(SecondsTimer), group: "Loops");
      yield return new(typeof(Update), group: "Loops");
      yield return new(typeof(LocalUpdate), group: "Loops");

      yield return new(typeof(DynamicImpulseReceiver));

      // Events are pretty useful
      yield return new(typeof(OnLoaded), group: "Events");
      yield return new(typeof(OnSaving), group: "Events");
      yield return new(typeof(WorldSaved), group: "World Events");
      yield return new(typeof(OnStart), group: "Events");
      yield return new(typeof(OnDuplicate), group: "Events");
      yield return new(typeof(OnDestroy), group: "Events");
      yield return new(typeof(OnDestroying), group: "Events");
      yield return new(typeof(OnPackageImported), group: "Events");

      yield return new(typeof(UserJoined), group: "World Events");
      yield return new(typeof(UserLeft), group: "World Events");
      yield return new(typeof(UserSpawn), group: "World Events");

      yield return new(typeof(WorldFocused), group: "Events");
      yield return new(typeof(WorldUnFocused), group: "Events");

      yield return new(typeof(StartAsyncTask), group: "Async");
      yield return new(typeof(AsyncDynamicImpulseReceiver), group: "Async");
    }

  }
}
