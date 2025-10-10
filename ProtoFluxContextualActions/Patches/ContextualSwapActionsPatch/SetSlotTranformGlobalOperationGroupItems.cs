using System;
using System.Collections.Generic;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Transform;
using ProtoFluxContextualActions.Tagging;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  internal static IEnumerable<MenuItem> SetSlotTranformGlobalOperationGroupItems(Type nodeType)
  {
    if (Groups.SetSlotTranformGlobalOperationGroup.Contains(nodeType))
    {
      foreach (var match in Groups.SetSlotTranformGlobalOperationGroup)
      {
        yield return new MenuItem(match);
      }
    }
  }
}