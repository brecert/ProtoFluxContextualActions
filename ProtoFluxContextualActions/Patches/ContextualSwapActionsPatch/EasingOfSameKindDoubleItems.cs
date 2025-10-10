using System;
using System.Collections.Generic;
using ProtoFluxContextualActions.Tagging;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  internal static IEnumerable<MenuItem> EasingOfSameKindDoubleItems(Type nodeType)
  {
    if (EasingGroups.ContainsNodeDouble(nodeType))
    {
      foreach (var match in EasingGroups.GetEasingOfSameKindDouble(nodeType))
      {
        yield return new MenuItem(match);
      }
    }
  }
}