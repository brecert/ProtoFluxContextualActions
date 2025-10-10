using System;
using System.Collections.Generic;
using System.Linq;
using Elements.Core;
using ProtoFlux.Runtimes.Execution.Nodes;
using ProtoFluxContextualActions.Extensions;
using ProtoFluxContextualActions.Utils;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  internal static IEnumerable<MenuItem> ApproximatelyNodesGroupItems(ContextualContext context)
  {

    if (TypeUtils.TryGetGenericTypeDefinition(context.NodeType, out var genericType))
    {
      var psuedoGenerics = context.World.GetPsuedoGenericTypesForWorld();
      var aproximatelyNodes = psuedoGenerics.Approximately.ToBiDictionary(a => a.Node, a => a.Types.First());

      if (genericType == typeof(ValueEquals<>) && aproximatelyNodes.TryGetFirst(context.NodeType.GenericTypeArguments[0], out var first))
      {
        yield return new MenuItem(first);
      }
      else if (genericType == typeof(ValueNotEquals<>) && aproximatelyNodes.TryGetFirst(context.NodeType.GenericTypeArguments[0], out first))
      {
        yield return new MenuItem(first);
      }
    }
  }
}