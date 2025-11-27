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
    var binaryOperationsGroup = psuedoGenericTypes.Repeat01.ToDictionary();

    if (context.NodeType.TryGetGenericTypeDefinition(out var genericTypeDefinition))
    {
      if (binaryOperationsGroup.Where(o => o.Value.SequenceEqual(context.NodeType.GenericTypeArguments)).Select(i => i.Key).SingleOrDefault() is Type match)
      {
        yield return new(match, connectionTransferType: ConnectionTransferType.ByIndexLossy);
      }
    }
    else if (binaryOperationsGroup.TryGetValue(context.NodeType, out var genericTypes))
    {
      yield return new(typeof(ValueRepeat<>).MakeGenericType([.. genericTypes]), connectionTransferType: ConnectionTransferType.ByIndexLossy);
    }

  }
}