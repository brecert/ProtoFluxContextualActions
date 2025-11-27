using System;
using System.Collections.Generic;
using System.Linq;
using ProtoFlux.Runtimes.Execution.Nodes.Math;
using ProtoFluxContextualActions.Extensions;
using ProtoFluxContextualActions.Utils;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  internal static IEnumerable<MenuItem> RepeatGroupItems(ContextualContext context)
  {
    var psuedoGenericTypes = context.World.GetPsuedoGenericTypesForWorld();
    var group = psuedoGenericTypes.Repeat01.ToDictionary();

    if (context.NodeType.TryGetGenericTypeDefinition(out var genericTypeDefinition) && genericTypeDefinition == typeof(ValueRepeat<>))
    {
      var matchingNodes = group.Where(a => a.Value.SequenceEqual(context.NodeType.GenericTypeArguments)).Select(a => a.Key);
      foreach (var match in matchingNodes)
      {
        yield return new(match, connectionTransferType: ConnectionTransferType.ByIndexLossy);
      }
    }
    else if (group.TryGetValue(context.NodeType, out var genericTypes))
    {
      yield return new(typeof(ValueRepeat<>).MakeGenericType([.. genericTypes]), connectionTransferType: ConnectionTransferType.ByIndexLossy);
    }

  }
}