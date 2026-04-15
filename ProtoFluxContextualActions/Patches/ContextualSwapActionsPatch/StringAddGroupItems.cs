using System;
using System.Collections.Generic;
using ProtoFlux.Runtimes.Execution.Nodes;
using ProtoFlux.Runtimes.Execution.Nodes.Strings;
using ProtoFluxContextualActions.Utils;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> StringAddGroup = [
    typeof(ConcatenateString),
    typeof(ConcatenateMultiString),
  ];

  internal static IEnumerable<MenuItem> StringAddGroupItems(ContextualContext context)
  {
    if (StringAddGroup.Contains(context.NodeType))
    {
      foreach (var match in StringAddGroup)
      {
        yield return new MenuItem(match, connectionTransferType: ConnectionTransferType.ByIndexLossy);
      }
    }
  }
}