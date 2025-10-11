using System;
using System.Collections.Generic;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Interaction;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> GrabbableValuePropertyGroup = [
    typeof(IsGrabbableGrabbed),
    typeof(IsGrabbableScalable),
    typeof(IsGrabbableReceivable),
  ];


  internal static IEnumerable<MenuItem> GrabbableValuePropertyGroupItems(ContextualContext context)
  {
    if (GrabbableValuePropertyGroup.Contains(context.NodeType))
    {
      foreach (var match in GrabbableValuePropertyGroup)
      {
        yield return new MenuItem(match);
      }
    }
  }
}