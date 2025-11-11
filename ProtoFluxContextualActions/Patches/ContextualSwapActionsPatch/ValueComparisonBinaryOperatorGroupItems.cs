using System;
using System.Collections.Generic;
using Elements.Core;
using ProtoFlux.Runtimes.Execution.Nodes;
using ProtoFlux.Runtimes.Execution.Nodes.Operators;
using ProtoFluxContextualActions.Utils;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> ValueComparisonBinaryOperatorGroup = [
    typeof(ValueEquals<>),
    typeof(ValueNotEquals<>),
    typeof(ValueLessThan<>),
    typeof(ValueLessOrEqual<>),
    typeof(ValueGreaterThan<>),
    typeof(ValueGreaterOrEqual<>),
  ];

  internal static IEnumerable<MenuItem> ValueComparisonBinaryOperatorGroupItems(ContextualContext context)
  {
    if (context.NodeType.TryGetGenericTypeDefinition(out var genericType) && ValueComparisonBinaryOperatorGroup.Contains(genericType))
    {
      foreach (var match in ValueComparisonBinaryOperatorGroup)
      {
        var nodeType = match.MakeGenericType(context.NodeType.GenericTypeArguments);
        if (nodeType.IsValidGenericType(false))
        {
          yield return new MenuItem(nodeType);
        }
      }
    }
  }
}