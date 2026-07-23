using System;
using System.Collections.Generic;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Interaction;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> GrabbableEventGroup = [
    typeof(OnGrabbableGrabbed),
    typeof(OnGrabbableReleased),
  ];


  internal static IEnumerable<MenuItem> GrabbableEventGroupItems(ContextualContext context)
  {
    if (GrabbableEventGroup.Contains(context.NodeType))
    {
      foreach (var match in GrabbableEventGroup)
      {
        yield return new MenuItem(match);
      }
    }
  }
}