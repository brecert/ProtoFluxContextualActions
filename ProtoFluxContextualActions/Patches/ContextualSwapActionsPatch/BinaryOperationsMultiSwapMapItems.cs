using System;
using System.Collections.Generic;
using System.Linq;
using Elements.Core;
using ProtoFluxContextualActions.Extensions;
using ProtoFluxContextualActions.Utils;
namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  internal static IEnumerable<MenuItem> BinaryOperationsMultiSwapMapItems(ContextualContext context)
  {
    var psuedoGenerics = context.World.GetPsuedoGenericTypesForWorld();
    var binaryOperationsMultiSwapMap =
      psuedoGenerics.BinaryOperations().Select(a => a.Node)
        .Zip(psuedoGenerics.BinaryOperations().Select(a => a.Node))
        .ToBiDictionary();

    if (binaryOperationsMultiSwapMap.TryGetFirst(context.NodeType, out var matched))
    {
      yield return new MenuItem(
        node: matched,
        connectionTransferType: ConnectionTransferType.ByIndexLossy
      );
    }
    else if (binaryOperationsMultiSwapMap.TryGetSecond(context.NodeType, out matched))
    {
      yield return new MenuItem(
        node: matched,
        name: FormatMultiName(matched),
        connectionTransferType: ConnectionTransferType.ByIndexLossy
      );
    }
  }
}