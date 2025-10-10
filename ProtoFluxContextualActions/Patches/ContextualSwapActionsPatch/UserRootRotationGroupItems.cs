using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Users.Roots;
using ProtoFluxContextualActions.Tagging;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  internal static IEnumerable<MenuItem> UserRootRotationGroupItems(ContextualContext context)
  {
    if (Groups.UserRootRotationGroup.Contains(context.NodeType))
    {
      foreach (var match in Groups.UserRootRotationGroup)
      {
        yield return new MenuItem(match);
      }
    }
  }

}