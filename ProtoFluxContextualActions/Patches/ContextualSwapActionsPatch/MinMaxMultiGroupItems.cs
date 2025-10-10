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
  static readonly HashSet<Type> MinMaxMultiGroup = [
    typeof(ValueMinMulti<>),
    typeof(ValueMaxMulti<>),
  ];

  internal static IEnumerable<MenuItem> MinMaxMultiGroupItems(Type nodeType, Dictionary<Type, IEnumerable<Type>> avgGroup)
  {
    if (TypeUtils.TryGetGenericTypeDefinition(nodeType, out var genericType) && MinMaxMultiGroup.Contains(genericType))
    {
      var innerType = nodeType.GenericTypeArguments[0];
      foreach (var match in MinMaxMultiGroup)
      {
        yield return new MenuItem(match.MakeGenericType(innerType));
      }

      var matchingNodes = avgGroup
        .Where(a => a.Value.FirstOrDefault() == innerType)
        .Select(a => a.Key)
        .Where(a => a.GetNiceTypeName().Contains("Multi_"));

      foreach (var match in matchingNodes)
      {
        yield return new MenuItem(
          node: match,
          name: FormatMultiName(match),
          connectionTransferType: ConnectionTransferType.ByIndexLossy
        );
      }
    }
  }
}