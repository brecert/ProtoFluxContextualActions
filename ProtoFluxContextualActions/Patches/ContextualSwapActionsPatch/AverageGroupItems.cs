using System;
using System.Collections.Generic;
using System.Linq;
using Elements.Core;
using ProtoFlux.Core;
using ProtoFluxContextualActions.Utils;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  internal static IEnumerable<MenuItem> AverageGroupItems(ContextualContext context)
  {
    var psuedoGenericTypes = context.World.GetPsuedoGenericTypesForWorld();
    var avgGroup = psuedoGenericTypes.AvgGroup().ToDictionary();

    if (avgGroup.TryGetValue(context.NodeType, out var genericTypes))
    {
      var matchingNodes = avgGroup.Where(a => genericTypes.SequenceEqual(a.Value)).Select(a => a.Key);
      foreach (var match in matchingNodes)
      {
        yield return new MenuItem(
          node: match,
          name: match.GetNiceTypeName().Contains("Multi_") ? FormatMultiName(match) : null,
          connectionTransferType: ConnectionTransferType.ByIndexLossy
        );
      }
      if (context.NodeType.GetNiceTypeName().Contains("Multi_"))
      {
        foreach (var match in MinMaxMultiGroup)
        {
          yield return new MenuItem(
            node: match.MakeGenericType([.. genericTypes]),
            name: FormatMultiName(match),
            connectionTransferType: ConnectionTransferType.ByIndexLossy
          );
        }
      }
      else
      {
        foreach (var match in MinMaxGroup)
        {
          yield return new MenuItem(
            node: match.MakeGenericType([.. genericTypes]),
            connectionTransferType: ConnectionTransferType.ByIndexLossy
          );
        }
      }
    }
  }
}