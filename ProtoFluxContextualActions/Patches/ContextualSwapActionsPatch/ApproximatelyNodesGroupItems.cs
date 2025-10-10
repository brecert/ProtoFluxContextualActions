using System;
using System.Collections.Generic;
using Elements.Core;
using ProtoFlux.Runtimes.Execution.Nodes;
using ProtoFluxContextualActions.Utils;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  internal static IEnumerable<MenuItem> ApproximatelyNodesGroupItems(ContextualContext context)
  {
    if (TypeUtils.TryGetGenericTypeDefinition(context.NodeType, out var genericType))
    {
      if (genericType == typeof(ValueEquals<>) && ApproximatelyNodes.TryGetFirst(context.NodeType.GenericTypeArguments[0], out var first))
      {
        yield return new MenuItem(first);
      }
      else if (genericType == typeof(ValueNotEquals<>) && ApproximatelyNotNodes.TryGetFirst(context.NodeType.GenericTypeArguments[0], out first))
      {
        yield return new MenuItem(first);
      }
    }
  }
}