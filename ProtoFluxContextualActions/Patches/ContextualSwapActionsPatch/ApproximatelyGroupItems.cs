using System;
using System.Collections.Generic;
using System.Linq;
using Elements.Core;
using ProtoFlux.Runtimes.Execution.Nodes;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  internal static IEnumerable<MenuItem> ApproximatelyGroupItems(Type nodeType, BiDictionary<Type, Type> approximatelyNodes, BiDictionary<Type, Type> approximatelyNotNodes, Dictionary<Type, Type> approximatelyGroup)
  {
    if (approximatelyGroup.TryGetValue(nodeType, out var typeArgument))
    {
      var matchingNodes = approximatelyGroup.Where(a => a.Value == typeArgument).Select(a => a.Key);
      foreach (var match in matchingNodes)
      {
        yield return new MenuItem(match);
      }

      // todo: invert so nodeType.IsApproxamatelyNotNode()
      if (approximatelyNodes.ContainsFirst(nodeType))
      {
        yield return new MenuItem(typeof(ValueEquals<>).MakeGenericType(typeArgument));
      }
      else if (approximatelyNotNodes.ContainsFirst(nodeType))
      {
        yield return new MenuItem(typeof(ValueNotEquals<>).MakeGenericType(typeArgument));
      }
    }
  }
}