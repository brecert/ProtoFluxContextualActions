using System;
using System.Collections.Generic;
using ProtoFlux.Runtimes.Execution.Nodes;
using ProtoFluxContextualActions.Utils;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> ObjectRelayGroup = [
    typeof(ObjectRelay<>),
    typeof(ContinuouslyChangingObjectRelay<>)
  ];

  internal static IEnumerable<MenuItem> ObjectRelayGroupItems(ContextualContext context)
  {
    if (context.NodeType.TryGetGenericTypeDefinition(out var genericType) && ObjectRelayGroup.Contains(genericType))
    {
      foreach (var match in ObjectRelayGroup)
      {
        yield return new MenuItem(match.MakeGenericType(context.NodeType.GenericTypeArguments));
      }
    }
  }
}