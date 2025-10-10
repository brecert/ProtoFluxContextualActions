using System;
using System.Collections.Generic;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Time;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  public static readonly HashSet<Type> DeltaTimeGroup = [
    typeof(DeltaTime),
    typeof(SmoothDeltaTime),
    typeof(InvertedDeltaTime),
    typeof(InvertedSmoothDeltaTime),
  ];

  internal static IEnumerable<MenuItem> DeltaTimeGroupItems(ContextualContext context)
  {
    if (DeltaTimeGroup.Contains(context.NodeType))
    {
      foreach (var match in DeltaTimeGroup)
      {
        yield return new MenuItem(match);
      }
    }
  }
}