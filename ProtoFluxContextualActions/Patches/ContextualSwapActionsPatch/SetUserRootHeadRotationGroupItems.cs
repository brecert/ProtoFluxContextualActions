using System;
using System.Collections.Generic;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Users.Roots;
using ProtoFluxContextualActions.Tagging;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  internal static IEnumerable<MenuItem> SetUserRootHeadRotationGroupItems(Type nodeType)
  {
    if (Groups.SetUserRootHeadRotationGroup.Contains(nodeType))
    {
      foreach (var match in Groups.SetUserRootHeadRotationGroup)
      {
        yield return new MenuItem(match);
      }
    }
  }
}