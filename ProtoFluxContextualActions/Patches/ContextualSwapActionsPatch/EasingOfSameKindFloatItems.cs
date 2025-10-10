using System;
using System.Collections.Generic;
using ProtoFluxContextualActions.Tagging;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  internal static IEnumerable<MenuItem> EasingOfSameKindFloatItems(Type nodeType)
  {
    if (EasingGroups.ContainsNodeFloat(nodeType))
    {
      foreach (var match in EasingGroups.GetEasingOfSameKindFloat(nodeType))
      {
        yield return new MenuItem(match);
      }
    }
  }
}