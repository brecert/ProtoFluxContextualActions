using System;
using System.Collections.Generic;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Avatar;
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
    typeof(GetActiveUser),
  ];

  static readonly HashSet<Type> UserSlotReferenceGroup = [
    typeof(LocalUserSlot),
    typeof(UserRootSlot)
  ];

  static readonly HashSet<Type> NearestUserReferenceGroup = [
    typeof(NearestUserHead),
    typeof(NearestUserHand),
    typeof(NearestUserFoot),
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
    if (UserSlotReferenceGroup.Contains(context.NodeType))
    {
      foreach (var match in UserSlotReferenceGroup)
      {
        yield return new MenuItem(match);
      }
    }
    if (NearestUserReferenceGroup.Contains(context.NodeType))
    {
      foreach (var match in NearestUserReferenceGroup)
      {
        yield return new MenuItem(match);
      }
    }
  }
}