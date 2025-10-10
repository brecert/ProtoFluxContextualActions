using System;
using System.Collections.Generic;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Transform;
using ProtoFluxContextualActions.Tagging;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  internal static IEnumerable<MenuItem> SetSlotTranformGlobalOperationGroupItems(ContextualContext context)
  {
    if (Groups.SetSlotTranformGlobalOperationGroup.Contains(context.NodeType))
    {
      foreach (var match in Groups.SetSlotTranformGlobalOperationGroup)
      {
        yield return new MenuItem(match);
      }
    }
  }
}