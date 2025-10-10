using System;
using System.Collections.Generic;
using Elements.Core;
using ProtoFlux.Runtimes.Execution.Nodes;
using ProtoFluxContextualActions.Utils;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  internal static IEnumerable<MenuItem> ApproximatelyNodesGroupItems(Type nodeType, BiDictionary<Type, Type> approximatelyNodes, BiDictionary<Type, Type> approximatelyNotNodes)
  {
    if (TypeUtils.TryGetGenericTypeDefinition(nodeType, out var genericType))
    {
      if (genericType == typeof(ValueEquals<>) && approximatelyNodes.TryGetFirst(nodeType.GenericTypeArguments[0], out var first))
      {
        yield return new MenuItem(first);
      }
      else if (genericType == typeof(ValueNotEquals<>) && approximatelyNotNodes.TryGetFirst(nodeType.GenericTypeArguments[0], out first))
      {
        yield return new MenuItem(first);
      }
    }
  }
}