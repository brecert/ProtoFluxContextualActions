using System;
using System.Collections.Generic;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Users.Roots;
using ProtoFluxContextualActions.Tagging;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  internal static IEnumerable<MenuItem> UserRootPositionGroupItems(Type nodeType)
  {
    if (Groups.UserRootPositionGroup.Contains(nodeType))
    {
      foreach (var match in Groups.UserRootPositionGroup)
      {
        yield return new MenuItem(match);
      }
    }
  }
}