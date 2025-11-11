using System;
using System.Collections.Generic;
using Elements.Core;
using ProtoFlux.Runtimes.Execution.Nodes;
using ProtoFlux.Runtimes.Execution.Nodes.Operators;
using ProtoFluxContextualActions.Utils;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> ObjectEqualityBinaryOperatorGroup = [
  ];

  static readonly HashSet<Type> ObjectComparisonBinaryOperatorGroup = [
    typeof(ObjectEquals<>),
    typeof(ObjectNotEquals<>),
    typeof(ObjectLessThan<>),
    typeof(ObjectLessOrEqual<>),
    typeof(ObjectGreaterThan<>),
    typeof(ObjectGreaterOrEqual<>),
  ];

  internal static IEnumerable<MenuItem> ObjectComparisonBinaryOperatorGroupItems(ContextualContext context)
  {
    // var coder = Traverse.Create(typeof(Coder<>).MakeGenericType(opType));

    if (context.NodeType.TryGetGenericTypeDefinition(out var genericType) && ObjectComparisonBinaryOperatorGroup.Contains(genericType))
    {

      foreach (var match in ObjectComparisonBinaryOperatorGroup)
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