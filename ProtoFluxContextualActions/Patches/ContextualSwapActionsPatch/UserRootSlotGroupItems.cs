using System;
using System.Collections.Generic;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Users.Roots;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> UserRootSlotGroup = [
    typeof(HeadSlot),
    typeof(HandSlot),
    typeof(ControllerSlot),
  ];

  internal static IEnumerable<MenuItem> UserRootSlotGroupItems(ContextualContext context)
  {
    if (UserRootSlotGroup.Contains(context.NodeType))
    {
      foreach (var match in UserRootSlotGroup)
      {
        yield return new MenuItem(match);
      }
    }
  }
}