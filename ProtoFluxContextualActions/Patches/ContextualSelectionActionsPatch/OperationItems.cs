using FrooxEngine.ProtoFlux;

using System.Collections.Generic;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Slots;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Async;
using ProtoFlux.Runtimes.Execution.Nodes.Actions;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Worlds;

namespace ProtoFluxContextualActions.Patches;


static partial class ContextualSelectionActionsPatch
{
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
    yield return new MenuItem(typeof(WorldSaved), group: "World Events");
    yield return new MenuItem(typeof(OnStart), group: "Events");
    yield return new MenuItem(typeof(OnDuplicate), group: "Events");
    yield return new MenuItem(typeof(OnDestroy), group: "Events");
    yield return new MenuItem(typeof(OnDestroying), group: "Events");
    yield return new MenuItem(typeof(OnPackageImported), group: "Events");

    yield return new MenuItem(typeof(UserJoined), group: "World Events");
    yield return new MenuItem(typeof(UserLeft), group: "World Events");
    yield return new MenuItem(typeof(UserSpawn), group: "World Events");

    yield return new MenuItem(typeof(WorldFocused), group: "Events");
    yield return new MenuItem(typeof(WorldUnFocused), group: "Events");
  }
}