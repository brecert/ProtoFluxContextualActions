using System;
using System.Collections.Generic;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Users;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> UserInfoGroup = [
    typeof(UserVR_Active),
    typeof(UserFPS),
    typeof(UserTime),
    typeof(UserVoiceMode),
    typeof(UserHeadOutputDevice),
    typeof(UserActiveViewTargettingController),
    typeof(UserPrimaryHand),
  ];

  internal static IEnumerable<MenuItem> UserInfoGroupItems(Type nodeType)
  {
    if (UserInfoGroup.Contains(nodeType))
    {
      foreach (var match in UserInfoGroup)
      {
        yield return new MenuItem(match);
      }
    }
  }
}