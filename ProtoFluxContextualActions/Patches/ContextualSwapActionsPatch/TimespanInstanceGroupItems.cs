using System;
using System.Collections.Generic;
using ProtoFlux.Runtimes.Execution.Nodes.TimeAndDate;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> TimespanInstanceGroup = [
    typeof(TimeSpanFromDays),
    typeof(TimeSpanFromHours),
    typeof(TimeSpanFromMilliseconds),
    typeof(TimeSpanFromMinutes),
    typeof(TimeSpanFromSeconds),
    typeof(TimeSpanFromTicks),
  ];

  internal static IEnumerable<MenuItem> TimespanInstanceGroupItems(ContextualContext context)
  {
    if (TimespanInstanceGroup.Contains(context.NodeType))
    {
      foreach (var match in TimespanInstanceGroup)
      {
        yield return new MenuItem(match);
      }
    }
  }
}