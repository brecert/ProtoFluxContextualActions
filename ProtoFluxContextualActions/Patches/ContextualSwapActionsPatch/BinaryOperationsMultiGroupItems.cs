using System;
using System.Collections.Generic;
using System.Linq;
using Elements.Core;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  internal static IEnumerable<MenuItem> BinaryOperationsMultiGroupItems(Type nodeType, Dictionary<Type, Type[]> binaryOperationsMultiGroup)
  {
    if (binaryOperationsMultiGroup.TryGetValue(nodeType, out var genericTypes))
    {
      var matchingNodes = binaryOperationsMultiGroup.Where(a => genericTypes.SequenceEqual(a.Value)).Select(a => a.Key);
      foreach (var match in matchingNodes)
      {
        yield return new MenuItem(
          node: match,
          name: FormatMultiName(match),
          connectionTransferType: ConnectionTransferType.ByIndexLossy
        );
      }
    }
  }
}