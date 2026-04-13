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
    if (TypeUtils.TryGetGenericTypeDefinition(context.NodeType, out var genericType) && StringAddGroup.Contains(genericType))
    {
      foreach (var match in StringAddGroup)
      {
        yield return new MenuItem(
          match.MakeGenericType(context.NodeType.GenericTypeArguments[0]),
          name: match == typeof(ConcatenateMultiString) ? FormatMultiName(context.NodeType) : null,
          connectionTransferType: ConnectionTransferType.ByIndexLossy);
      }
    }
  }
}