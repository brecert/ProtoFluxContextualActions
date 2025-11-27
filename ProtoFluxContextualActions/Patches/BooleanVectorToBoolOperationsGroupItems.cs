using System;
using System.Collections.Generic;
using System.Linq;
using ProtoFluxContextualActions.Utils;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  internal static IEnumerable<MenuItem> BooleanVectorToBoolOperationsGroupItems(ContextualContext context)
  {
    var psuedoGenericTypes = context.World.GetPsuedoGenericTypesForWorld();
    var group = psuedoGenericTypes.BooleanVectorToBoolOperations().ToDictionary();

    if (group.TryGetValue(context.NodeType, out var genericTypes))
    {
      var matchingNodes = group.Where(a => genericTypes.SequenceEqual(a.Value)).Select(a => a.Key);
      foreach (var match in matchingNodes)
      {
        yield return new MenuItem(match);
      }
    }
  }
}