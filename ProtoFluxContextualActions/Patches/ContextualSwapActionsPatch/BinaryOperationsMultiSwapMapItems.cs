using System;
using System.Collections.Generic;
using Elements.Core;
namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  internal static IEnumerable<MenuItem> BinaryOperationsMultiSwapMapItems(Type nodeType, BiDictionary<Type, Type> binaryOperationsMultiSwapMap)
  {
    if (binaryOperationsMultiSwapMap.TryGetFirst(nodeType, out var matched))
    {
      yield return new MenuItem(
        node: matched,
        connectionTransferType: ConnectionTransferType.ByIndexLossy
      );
    }
    else if (binaryOperationsMultiSwapMap.TryGetSecond(nodeType, out matched))
    {
      yield return new MenuItem(
        node: matched,
        name: FormatMultiName(matched),
        connectionTransferType: ConnectionTransferType.ByIndexLossy
      );
    }
  }
}