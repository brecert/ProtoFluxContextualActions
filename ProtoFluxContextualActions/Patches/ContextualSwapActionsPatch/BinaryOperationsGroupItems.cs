using System;
using System.Collections.Generic;
using System.Linq;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  internal static IEnumerable<MenuItem> BinaryOperationsGroupItems(ContextualContext context)
  {
    if (BinaryOperationsGroup.TryGetValue(context.NodeType, out var genericTypes))
    {
      var matchingNodes = BinaryOperationsGroup.Where(a => genericTypes.SequenceEqual(a.Value)).Select(a => a.Key);
      foreach (var match in matchingNodes)
      {
        yield return new MenuItem(match);
      }
    }
  }
}