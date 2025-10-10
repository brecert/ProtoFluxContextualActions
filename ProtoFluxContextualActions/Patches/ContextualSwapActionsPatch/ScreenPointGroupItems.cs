using System;
using System.Collections.Generic;
using ProtoFluxContextualActions.Tagging;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  internal static IEnumerable<MenuItem> ScreenPointGroupItems(Type nodeType)
  {
    if (Groups.ScreenPointGroup.Contains(nodeType))
    {
      foreach (var match in Groups.ScreenPointGroup)
      {
        yield return new MenuItem(match);
      }
    }
  }
}