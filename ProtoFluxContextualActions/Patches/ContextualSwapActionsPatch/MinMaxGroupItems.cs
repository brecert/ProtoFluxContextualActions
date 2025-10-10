using System;
using System.Collections.Generic;
using System.Linq;
using Elements.Core;
using ProtoFlux.Core;
using ProtoFlux.Runtimes.Execution.Nodes.Math;
using ProtoFluxContextualActions.Utils;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> MinMaxGroup = [
    typeof(ValueMin<>),
    typeof(ValueMax<>),
  ];

  internal static IEnumerable<MenuItem> MinMaxGroupItems(ContextualContext context)
  {
    if (TypeUtils.TryGetGenericTypeDefinition(context.NodeType, out var genericType) && MinMaxGroup.Contains(genericType))
    {
      var innerType = context.NodeType.GenericTypeArguments[0];
      foreach (var match in MinMaxGroup)
      {
        yield return new MenuItem(match.MakeGenericType(innerType));
      }

      var matchingNodes = AvgGroup
        .Where(a => a.Value.FirstOrDefault() == innerType)
        .Select(a => a.Key)
        .Where(a => !a.GetNiceTypeName().Contains("Multi_"));

      foreach (var match in matchingNodes)
      {
        yield return new MenuItem(
          node: match,
          connectionTransferType: ConnectionTransferType.ByIndexLossy
        );
      }
    }
  }
}