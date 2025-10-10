using System;
using System.Collections.Generic;
using ProtoFlux.Runtimes.Execution.Nodes;
using ProtoFluxContextualActions.Utils;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> ValueRelayGroup = [
    typeof(ValueRelay<>),
    typeof(ContinuouslyChangingValueRelay<>)
  ];

  internal static IEnumerable<MenuItem> ValueRelayGroupItems(Type nodeType)
  {
    if (nodeType.TryGetGenericTypeDefinition(out var genericType) && ValueRelayGroup.Contains(genericType))
    {
      foreach (var match in ValueRelayGroup)
      {
        yield return new MenuItem(match.MakeGenericType(nodeType.GenericTypeArguments));
      }
    }
  }
}