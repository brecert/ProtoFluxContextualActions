using System;
using System.Collections.Generic;
using ProtoFlux.Runtimes.Execution.Nodes;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Utility;
using ProtoFluxContextualActions.Utils;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> ValueRelayGroup = [
    typeof(ValueRelay<>),
    typeof(ContinuouslyChangingValueRelay<>),
    typeof(DelayValue<>)
  ];

  internal static IEnumerable<MenuItem> ValueRelayGroupItems(ContextualContext context)
  {
    if (context.NodeType.TryGetGenericTypeDefinition(out var genericType) && ValueRelayGroup.Contains(genericType))
    {
      foreach (var match in ValueRelayGroup)
      {
        yield return new MenuItem(match.MakeGenericType(context.NodeType.GenericTypeArguments));
      }
    }
  }
}