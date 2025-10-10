using System;
using System.Collections.Generic;
using System.Linq;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  internal static IEnumerable<MenuItem> BinaryOperationsGroupItems(Type nodeType, Dictionary<Type, Type[]> binaryOperationsGroup)
  {
    if (binaryOperationsGroup.TryGetValue(nodeType, out var genericTypes))
    {
      var matchingNodes = binaryOperationsGroup.Where(a => genericTypes.SequenceEqual(a.Value)).Select(a => a.Key);
      foreach (var match in matchingNodes)
      {
        yield return new MenuItem(match);
      }
    }
  }
}