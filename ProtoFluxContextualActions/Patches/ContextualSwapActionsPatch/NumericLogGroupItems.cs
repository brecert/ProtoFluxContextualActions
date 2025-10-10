using System;
using System.Collections.Generic;
using System.Linq;
using Elements.Core;
using ProtoFluxContextualActions.Utils;
namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  internal static IEnumerable<MenuItem> NumericLogGroupItems(ContextualContext context)
  {
    var psuedoGenericTypes = context.World.GetPsuedoGenericTypesForWorld();
    var binaryOperationsGroup =
      psuedoGenericTypes.Log
        .Concat(psuedoGenericTypes.Log10)
        .Concat(psuedoGenericTypes.LogN)
        .ToDictionary();
    ;

    if (binaryOperationsGroup.TryGetValue(context.NodeType, out var genericTypes))
    {
      var matchingNodes = binaryOperationsGroup.Where(a => genericTypes.SequenceEqual(a.Value)).Select(a => a.Key);
      foreach (var match in matchingNodes)
      {
        yield return new MenuItem(match);
      }
    }
  }
}