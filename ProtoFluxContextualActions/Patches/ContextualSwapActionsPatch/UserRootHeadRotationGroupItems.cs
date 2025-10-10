using System;
using System.Collections.Generic;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Users.Roots;
using ProtoFluxContextualActions.Tagging;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  internal static IEnumerable<MenuItem> UserRootHeadRotationGroupItems(Type nodeType)
  {
    if (Groups.UserRootHeadRotationGroup.Contains(nodeType))
    {
      foreach (var match in Groups.UserRootHeadRotationGroup)
      {
        yield return new MenuItem(match);
      }
    }
  }
}