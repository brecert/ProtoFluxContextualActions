using System;
using System.Collections.Generic;
using ProtoFluxContextualActions.Tagging;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  internal static IEnumerable<MenuItem> MousePositionGroupItems(Type nodeType)
  {
    if (Groups.MousePositionGroup.Contains(nodeType))
    {
      foreach (var match in Groups.MousePositionGroup)
      {
        yield return new MenuItem(match);
      }
    }
  }
}