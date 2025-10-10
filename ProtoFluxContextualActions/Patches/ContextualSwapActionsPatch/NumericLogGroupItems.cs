using System;
using System.Collections.Generic;
using System.Linq;
using Elements.Core;
namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  internal static IEnumerable<MenuItem> NumericLogGroupItems(ContextualContext context)
  {
    if (NumericLogGroup.TryGetValue(context.NodeType, out var genericTypes))
    {
      var matchingNodes = NumericLogGroup.Where(a => genericTypes.SequenceEqual(a.Value)).Select(a => a.Key);
      foreach (var match in matchingNodes)
      {
        yield return new MenuItem(match);
      }
    }
  }
}