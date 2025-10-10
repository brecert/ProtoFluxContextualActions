using System;
using System.Collections.Generic;
using System.Linq;
using Elements.Core;
using ProtoFlux.Runtimes.Execution.Nodes;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  internal static IEnumerable<MenuItem> ApproximatelyGroupItems(ContextualContext context)
  {
    if (ApproximatelyGroup.TryGetValue(context.NodeType, out var typeArgument))
    {
      var matchingNodes = ApproximatelyGroup.Where(a => a.Value == typeArgument).Select(a => a.Key);
      foreach (var match in matchingNodes)
      {
        yield return new MenuItem(match);
      }

      // todo: invert so context.NodeType.IsApproxamatelyNotNode()
      if (ApproximatelyNodes.ContainsFirst(context.NodeType))
      {
        yield return new MenuItem(typeof(ValueEquals<>).MakeGenericType(typeArgument));
      }
      else if (ApproximatelyNotNodes.ContainsFirst(context.NodeType))
      {
        yield return new MenuItem(typeof(ValueNotEquals<>).MakeGenericType(typeArgument));
      }
    }
  }
}