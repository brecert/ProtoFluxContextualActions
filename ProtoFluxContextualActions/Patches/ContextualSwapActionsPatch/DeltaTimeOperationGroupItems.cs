using System;
using System.Collections.Generic;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Operators;
using ProtoFluxContextualActions.Utils;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  public static readonly HashSet<Type> DeltaTimeOperationGroup = [
    typeof(MulDeltaTime<>),
    typeof(DivDeltaTime<>),
  ];

  internal static IEnumerable<MenuItem> DeltaTimeOperationGroupItems(ContextualContext context)
  {
    if (context.NodeType.TryGetGenericTypeDefinition(out var genericType) && DeltaTimeOperationGroup.Contains(genericType))
    {
      foreach (var match in DeltaTimeOperationGroup)
      {
        yield return new MenuItem(match.MakeGenericType(context.NodeType.GenericTypeArguments));
      }
    }
  }
}