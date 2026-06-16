using System;
using System.Collections.Generic;
using ProtoFlux.Runtimes.Execution.Nodes;
using ProtoFlux.Runtimes.Execution.Nodes.Strings;
using ProtoFlux.Runtimes.Execution.Nodes.Utility.Uris;
using ProtoFluxContextualActions.Utils;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> StringUriGroup = [
    typeof(EscapeUriDataString),
    typeof(UnescapeUriDataString),
    typeof(StringToAbsoluteURI),
  ];

  static readonly HashSet<Type> StringEscapeGroup = [
    typeof(EscapeString),
    typeof(UnescapeString),
  ];

  internal static IEnumerable<MenuItem> StringConvertGroupItems(ContextualContext context)
  {
    if (StringUriGroup.Contains(context.NodeType))
    {
      foreach (var match in StringUriGroup)
      {
        yield return new MenuItem(match, connectionTransferType: ConnectionTransferType.ByIndexLossy);
      }
    }
    if (StringEscapeGroup.Contains(context.NodeType))
    {
      foreach (var match in StringEscapeGroup)
      {
        yield return new MenuItem(match, connectionTransferType: ConnectionTransferType.ByIndexLossy);
      }
    }
  }
}