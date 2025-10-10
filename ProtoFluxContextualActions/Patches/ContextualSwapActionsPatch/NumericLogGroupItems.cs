using System;
using System.Collections.Generic;
using System.Linq;
using Elements.Core;
namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  internal static IEnumerable<MenuItem> NumericLogGroupItems(Type nodeType, Dictionary<Type, Type[]> numericLogGroup)
  {
    if (numericLogGroup.TryGetValue(nodeType, out var genericTypes))
    {
      var matchingNodes = numericLogGroup.Where(a => genericTypes.SequenceEqual(a.Value)).Select(a => a.Key);
      foreach (var match in matchingNodes)
      {
        yield return new MenuItem(match);
      }
    }
  }
}