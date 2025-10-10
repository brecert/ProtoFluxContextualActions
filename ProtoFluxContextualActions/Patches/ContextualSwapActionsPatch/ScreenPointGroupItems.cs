using System;
using System.Collections.Generic;
using ProtoFluxContextualActions.Tagging;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  internal static IEnumerable<MenuItem> ScreenPointGroupItems(ContextualContext context)
  {
    if (Groups.ScreenPointGroup.Contains(context.NodeType))
    {
      foreach (var match in Groups.ScreenPointGroup)
      {
        yield return new MenuItem(match);
      }
    }
  }
}