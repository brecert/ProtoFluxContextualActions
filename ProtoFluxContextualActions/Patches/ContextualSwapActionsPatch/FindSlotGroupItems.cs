using System;
using System.Collections.Generic;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Slots;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> FindSlotGroup = [
    typeof(FindChildByName),
    typeof(FindChildByTag),
  ];

  internal static IEnumerable<MenuItem> FindSlotGroupItems(Type nodeType)
  {
    if (FindSlotGroup.Contains(nodeType))
    {
      foreach (var match in FindSlotGroup)
      {
        yield return new MenuItem(match);
      }
    }
  }
}