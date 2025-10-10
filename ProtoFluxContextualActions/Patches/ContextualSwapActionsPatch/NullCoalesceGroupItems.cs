using System;
using System.Collections.Generic;
using ProtoFlux.Runtimes.Execution.Nodes;
using ProtoFluxContextualActions.Utils;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> NullCoalesceGroup = [
    typeof(NullCoalesce<>),
    typeof(MultiNullCoalesce<>),
  ];

  internal static IEnumerable<MenuItem> NullCoalesceGroupItems(Type nodeType)
  {
    if (TypeUtils.TryGetGenericTypeDefinition(nodeType, out var genericType) && NullCoalesceGroup.Contains(genericType))
    {
      foreach (var match in NullCoalesceGroup)
      {
        yield return new MenuItem(match.MakeGenericType(nodeType.GenericTypeArguments[0]), name: match == typeof(MultiNullCoalesce<>) ? FormatMultiName(nodeType) : null);
      }
    }
  }
}