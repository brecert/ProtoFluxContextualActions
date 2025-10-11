using System;
using System.Collections.Generic;
using System.Linq;
using Elements.Core;
using ProtoFluxContextualActions.Extensions;
using ProtoFluxContextualActions.Utils;
namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  internal static IEnumerable<MenuItem> SinCosSwapGroup(ContextualContext context)
  {
    var psuedoGenerics = context.World.GetPsuedoGenericTypesForWorld();
    var sinCosSwapMap =
      psuedoGenerics.Sin.Select(a => a.Node)
        .Zip(psuedoGenerics.Cos.Select(a => a.Node))
        .ToBiDictionary();

    if (sinCosSwapMap.TryGetFirst(context.NodeType, out var sinType))
    {
      yield return new MenuItem(sinType);
    }
    else if (sinCosSwapMap.TryGetSecond(context.NodeType, out var cosType))
    {
      yield return new MenuItem(cosType);
    }
  }
}