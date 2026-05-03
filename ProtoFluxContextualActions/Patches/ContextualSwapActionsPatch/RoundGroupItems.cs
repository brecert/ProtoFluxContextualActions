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
    var roundToIntSwapGroup = roundToIntGroup.Keys.Zip(roundGroup.Keys).ToBiDictionary();

    if (roundGroup.TryGetValue(context.NodeType, out var genericTypes))
    {
      var matchingNodes = roundGroup.Where(a => genericTypes.SequenceEqual(a.Value));
      foreach (var match in matchingNodes)
      {
        yield return new MenuItem(match.Key);
      }
    }
    if (roundToIntGroup.TryGetValue(context.NodeType, out var genericTypes2))
    {
      var matchingNodes = roundGroup.Where(a => genericTypes2.SequenceEqual(a.Value));
      foreach (var match in matchingNodes)
      {
        yield return new MenuItem(match.Key);
      }
    }
    if (TryGetSwap(roundToIntSwapGroup, context.NodeType, out Type swap))
    {
      yield return new MenuItem(swap);
    }
  }
}