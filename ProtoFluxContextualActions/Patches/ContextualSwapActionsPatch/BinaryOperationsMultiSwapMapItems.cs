using System;
using System.Collections.Generic;
using Elements.Core;
namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  internal static IEnumerable<MenuItem> BinaryOperationsMultiSwapMapItems(ContextualContext context)
  {
    if (BinaryOperationsMultiSwapMap.TryGetFirst(context.NodeType, out var matched))
    {
      yield return new MenuItem(
        node: matched,
        connectionTransferType: ConnectionTransferType.ByIndexLossy
      );
    }
    else if (BinaryOperationsMultiSwapMap.TryGetSecond(context.NodeType, out matched))
    {
      yield return new MenuItem(
        node: matched,
        name: FormatMultiName(matched),
        connectionTransferType: ConnectionTransferType.ByIndexLossy
      );
    }
  }
}