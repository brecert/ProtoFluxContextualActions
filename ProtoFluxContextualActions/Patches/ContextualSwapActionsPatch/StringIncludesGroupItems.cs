using System;
using System.Collections.Generic;
using ProtoFlux.Runtimes.Execution.Nodes;
using ProtoFlux.Runtimes.Execution.Nodes.Strings;
using ProtoFluxContextualActions.Utils;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> StringIncludesGroup = [
    typeof(Contains),
    typeof(StartsWith),
    typeof(EndsWith),
  ];

  internal static IEnumerable<MenuItem> StringIncludesGroupItems(ContextualContext context)
  {
    if (StringIncludesGroup.Contains(context.NodeType))
    {
      foreach (var match in StringIncludesGroup)
      {
        yield return new MenuItem(match, connectionTransferType: ConnectionTransferType.ByIndexLossy);
      }
    }
  }
}