using System;
using System.Collections.Generic;
using ProtoFlux.Runtimes.Execution.Nodes;
using ProtoFlux.Runtimes.Execution.Nodes.Operators;
using ProtoFluxContextualActions.Utils;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> ComparisonBinaryOperatorGroup = [
    typeof(ValueLessThan<>),
    typeof(ValueLessOrEqual<>),
    typeof(ValueGreaterThan<>),
    typeof(ValueGreaterOrEqual<>),
    typeof(ValueEquals<>),
    typeof(ValueNotEquals<>),
  ];

  internal static IEnumerable<MenuItem> ComparisonBinaryOperatorGroupItems(ContextualContext context)
  {
    if (context.NodeType.TryGetGenericTypeDefinition(out var genericType) && ComparisonBinaryOperatorGroup.Contains(genericType))
    {
      foreach (var match in ComparisonBinaryOperatorGroup)
      {
        yield return new MenuItem(match.MakeGenericType(context.NodeType.GenericTypeArguments));
      }
    }
  }
}