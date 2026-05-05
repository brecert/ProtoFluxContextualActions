using System;
using System.Collections.Generic;
using System.Linq;
using Elements.Core;
using ProtoFlux.Core;
using ProtoFluxContextualActions.Extensions;
using ProtoFluxContextualActions.Utils;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  internal static IEnumerable<MenuItem> RoundGroupItems(ContextualContext context)
  {
    var psuedoGenericTypes = context.World.GetPsuedoGenericTypesForWorld();
    var roundItems = psuedoGenericTypes.Round.ToDictionary();
    var floorItems = psuedoGenericTypes.Floor.ToDictionary();
    var ceilItems = psuedoGenericTypes.Ceil.ToDictionary();
    var roundGroup = roundItems.Concat(floorItems).Concat(ceilItems).ToDictionary();
    var roundIntItems = psuedoGenericTypes.RoundToInt.ToDictionary();
    var floorIntItems = psuedoGenericTypes.FloorToInt.ToDictionary();
    var ceilIntItems = psuedoGenericTypes.CeilToInt.ToDictionary();
    var roundToIntGroup = roundIntItems.Concat(floorIntItems).Concat(ceilIntItems).ToDictionary();

    if (roundGroup.TryGetValue(context.NodeType, out var genericTypes))
    {
      var matchingNodes = roundGroup.Where(a => genericTypes.SequenceEqual(a.Value));
      foreach (var match in matchingNodes)
      {
        yield return new MenuItem(match.Key);
      }
      if (roundItems.TryGetValue(context.NodeType, out var g2)) yield return new MenuItem(roundIntItems.First(v=>g2.SequenceEqual(v.Value)).Key);
      if (floorItems.TryGetValue(context.NodeType, out g2)) yield return new MenuItem(floorIntItems.First(v=>g2.SequenceEqual(v.Value)).Key);
      if (ceilItems.TryGetValue(context.NodeType, out g2)) yield return new MenuItem(ceilIntItems.First(v=>g2.SequenceEqual(v.Value)).Key);
    }
    if (roundToIntGroup.TryGetValue(context.NodeType, out var genericTypes2))
    {
      var matchingNodes = roundToIntGroup.Where(a => genericTypes2.SequenceEqual(a.Value));
      foreach (var match in matchingNodes)
      {
        yield return new MenuItem(match.Key);
      }
      if (roundIntItems.TryGetValue(context.NodeType, out var g2)) yield return new MenuItem(roundItems.First(v=>g2.SequenceEqual(v.Value)).Key);
      if (floorIntItems.TryGetValue(context.NodeType, out g2)) yield return new MenuItem(floorItems.First(v=>g2.SequenceEqual(v.Value)).Key);
      if (ceilIntItems.TryGetValue(context.NodeType, out g2)) yield return new MenuItem(ceilItems.First(v=>g2.SequenceEqual(v.Value)).Key);
    }
  }
}