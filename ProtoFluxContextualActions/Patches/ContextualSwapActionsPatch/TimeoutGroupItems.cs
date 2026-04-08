using System;
using System.Collections.Generic;
using ProtoFlux.Runtimes.Execution.Nodes.Actions;
using ProtoFlux.Runtimes.Execution.Nodes.TimeAndDate;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> TimeoutGroup = [
    typeof(LocalImpulseTimeoutSeconds),
    typeof(LocalImpulseTimeoutTimeSpan)
  ];
  internal static IEnumerable<MenuItem> TimeoutGroupItems(ContextualContext context)
  {
    if (TimeoutGroup.Contains(context.NodeType))
    {
      foreach (var match in TimeoutGroup)
      {
        yield return new MenuItem(match);
      }
    }
  }
}