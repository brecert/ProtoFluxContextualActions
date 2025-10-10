using System;
using System.Collections.Generic;
using ProtoFlux.Runtimes.Execution.Nodes;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  // todo: async
  static readonly HashSet<Type> ForLoopGroup = [
    typeof(For),
    typeof(RangeLoopInt),
  ];

  internal static IEnumerable<MenuItem> ForLoopGroupItems(Type nodeType)
  {
    if (ForLoopGroup.Contains(nodeType))
    {
      foreach (var match in ForLoopGroup)
      {
        yield return new MenuItem(match, connectionTransferType: ConnectionTransferType.ByMappingsLossy);
      }
    }
  }
}