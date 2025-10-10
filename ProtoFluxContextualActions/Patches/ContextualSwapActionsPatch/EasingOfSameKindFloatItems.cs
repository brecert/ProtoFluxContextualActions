using System;
using System.Collections.Generic;
using ProtoFluxContextualActions.Tagging;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  internal static IEnumerable<MenuItem> EasingOfSameKindFloatItems(ContextualContext context)
  {
    if (EasingGroups.ContainsNodeFloat(context.NodeType))
    {
      foreach (var match in EasingGroups.GetEasingOfSameKindFloat(context.NodeType))
      {
        yield return new MenuItem(match);
      }
    }
  }
}