using System;
using System.Collections.Generic;
using ProtoFlux.Runtimes.Execution.Nodes.TimeAndDate;
using ProtoFluxContextualActions.Tagging;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  internal static IEnumerable<MenuItem> WorldTimeGroupItems(ContextualContext context)
  {
    if (Groups.WorldTimeFloatGroup.Contains(context.NodeType))
    {
      foreach (var match in Groups.WorldTimeFloatGroup)
      {
        yield return new MenuItem(match);
      }
    }
  }
}