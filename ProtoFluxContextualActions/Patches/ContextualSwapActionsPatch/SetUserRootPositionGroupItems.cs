using System;
using System.Collections.Generic;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Users.Roots;
using ProtoFluxContextualActions.Tagging;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  internal static IEnumerable<MenuItem> SetUserRootPositionGroupItems(ContextualContext context)
  {
    if (Groups.SetUserRootPositionGroup.Contains(context.NodeType))
    {
      foreach (var match in Groups.SetUserRootPositionGroup)
      {
        yield return new MenuItem(match);
      }
    }
  }
}