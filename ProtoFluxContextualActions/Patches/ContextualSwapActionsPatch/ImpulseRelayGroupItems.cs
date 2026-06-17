using System;
using System.Collections.Generic;
using ProtoFlux.Runtimes.Execution.Nodes;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Input.Controllers;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Input.Keyboard;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Slots;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> ImpulseRelayGroup = [
    typeof(CallRelay),
    typeof(ContinuationRelay),
    typeof(AsyncCallRelay),
  ];

  internal static IEnumerable<MenuItem> ImpulseRelayGroupItems(ContextualContext context)
  {
    if (ImpulseRelayGroup.Contains(context.NodeType))
    {
      foreach (var match in ImpulseRelayGroup)
      {
        yield return new MenuItem(match);
      }
    }
  }
}