using System;
using System.Collections.Generic;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Slots;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> OrderOffsetGroup = [
    typeof(GetSlotOrderOffset),
    typeof(SetSlotOrderOffset),
  ];

  internal static IEnumerable<MenuItem> OrderOffsetGroupItems(ContextualContext context)
  {
    if (OrderOffsetGroup.Contains(context.NodeType))
    {
      foreach (var match in OrderOffsetGroup)
      {
        yield return new MenuItem(match);
      }
    }
  }
}