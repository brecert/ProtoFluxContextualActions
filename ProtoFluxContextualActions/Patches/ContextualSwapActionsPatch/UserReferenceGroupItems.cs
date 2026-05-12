using System;
using System.Collections.Generic;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Slots;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Users;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Users.Roots;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> UserReferenceGroup = [
    typeof(LocalUser),
    typeof(HostUser),
    typeof(GetActiveUserSelf),
  ];

  internal static IEnumerable<MenuItem> UserReferenceGroupItems(ContextualContext context)
  {
    if (UserReferenceGroup.Contains(context.NodeType))
    {
      foreach (var match in UserReferenceGroup)
      {
        yield return new MenuItem(match);
      }
    }
  }
}