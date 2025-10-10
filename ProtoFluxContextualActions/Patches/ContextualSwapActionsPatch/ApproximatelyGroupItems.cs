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
  internal static IEnumerable<MenuItem> ApproximatelyGroupItems(ContextualContext context)
  {
    var psuedoGenerics = context.World.GetPsuedoGenericTypesForWorld();
    var aproximatelyNodes = psuedoGenerics.Approximately.ToBiDictionary(a => a.Node, a => a.Types.First());
    var aproximatelyNotNodes = psuedoGenerics.ApproximatelyNot.ToBiDictionary(a => a.Node, a => a.Types.First());
    var approximatelyGroup = aproximatelyNodes.Concat(aproximatelyNotNodes).ToDictionary(a => a.First, a => a.Second);

    if (approximatelyGroup.TryGetValue(context.NodeType, out var typeArgument))
    {
      var matchingNodes = approximatelyGroup.Where(a => a.Value == typeArgument).Select(a => a.Key);
      foreach (var match in matchingNodes)
      {
        yield return new MenuItem(match);
      }

      // todo: invert so context.NodeType.IsApproxamatelyNotNode()
      if (aproximatelyNodes.ContainsFirst(context.NodeType))
      {
        yield return new MenuItem(typeof(ValueEquals<>).MakeGenericType(typeArgument));
      }
      else if (aproximatelyNotNodes.ContainsFirst(context.NodeType))
      {
        yield return new MenuItem(typeof(ValueNotEquals<>).MakeGenericType(typeArgument));
      }
    }
  }
}