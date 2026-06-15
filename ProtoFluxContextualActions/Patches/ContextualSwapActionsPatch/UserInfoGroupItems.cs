using System;
using System.Collections.Generic;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Interaction;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Users;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> UserInfoGroup = [
    typeof(UserUserID),
    typeof(UserUsername),
    typeof(UserVR_Active),
    typeof(IsLocalUser),
    typeof(IsContextMenuOpen),
    typeof(UserFPS),
    typeof(UserTime),
    typeof(UserVoiceMode),
    typeof(UserHeadOutputDevice),
    typeof(UserPrimaryHand),
    typeof(UserTimeOffset),
    typeof(UserMachineID),
    typeof(UserActiveViewTargettingController),
    typeof(UserEngineVersion),
    typeof(UserRendererName),
    typeof(UserRuntimeVersion),
  ];

  internal static IEnumerable<MenuItem> UserInfoGroupItems(ContextualContext context)
  {
    if (UserInfoGroup.Contains(context.NodeType))
    {
      foreach (var match in UserInfoGroup)
      {
        yield return new MenuItem(match);
      }
    }
  }
}