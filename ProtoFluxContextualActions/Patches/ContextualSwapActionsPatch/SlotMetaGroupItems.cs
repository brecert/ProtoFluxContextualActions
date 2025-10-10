using System;
using System.Collections.Generic;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Slots;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> SlotMetaGroup = [
    typeof(GetSlotName),
    typeof(GetTag),
  ];

  internal static IEnumerable<MenuItem> SlotMetaGroupItems(ContextualContext context)
  {
    if (SlotMetaGroup.Contains(context.NodeType))
    {
      foreach (var match in SlotMetaGroup)
      {
        yield return new MenuItem(match);
      }
    }
  }
}