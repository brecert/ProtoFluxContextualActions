using System;
using System.Collections.Frozen;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Input.Mouse;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Playback;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Time;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Transform;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Users.LocalScreen;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Users.Roots;

namespace ProtoFluxContextualActions.Tagging;

static class Groups
{
  public static FrozenSet<Type> WorldTimeFloatGroup = [
    typeof(WorldTimeFloat),
    typeof(WorldTime2Float),
    typeof(WorldTime10Float),
    typeof(WorldTimeTenthFloat),
  ];

  public static FrozenSet<Type> WorldTimeDoubleGroup = [
    typeof(WorldTimeDouble),
  ];

  public static FrozenSet<Type> WorldTimeSwapGroup = [
    typeof(WorldTimeFloat),
    typeof(WorldTimeDouble),
  ];

  public static FrozenSet<Type> ScreenPointGroup = [
    typeof(LocalScreenPointToDirection),
    typeof(LocalScreenPointToWorld),
  ];

  public static FrozenSet<Type> MousePositionGroup = [
    typeof(NormalizedMousePosition),
    typeof(DesktopMousePosition),
    typeof(MousePosition),
  ];

  public static readonly FrozenSet<Type> UserRootPositionGroup = [
    typeof(HeadPosition),
    typeof(HipsPosition),
    typeof(LeftHandPosition),
    typeof(RightHandPosition),
    typeof(FeetPosition),
  ];

  public static readonly FrozenSet<Type> UserRootRotationGroup = [
    typeof(HeadRotation),
    typeof(HipsRotation),
    typeof(LeftHandRotation),
    typeof(RightHandRotation),
    typeof(FeetRotation),
  ];

  public static readonly FrozenSet<Type> SetUserRootPositionGroup = [
    typeof(SetHeadPosition),
    typeof(SetHipsPosition),
    typeof(SetFeetPosition),
  ];

  public static readonly FrozenSet<Type> SetUserRootRotationGroup = [
    typeof(SetHeadRotation),
    typeof(SetHipsRotation),
    typeof(SetFeetRotation),
  ];

  public static readonly FrozenSet<Type> UserRootHeadRotationGroup = [
    typeof(HeadRotation),
    typeof(HeadFacingRotation),
    typeof(HeadFacingDirection),
  ];

  public static readonly FrozenSet<Type> SetUserRootHeadRotationGroup = [
    typeof(SetHeadRotation),
    typeof(SetHeadFacingRotation),
    typeof(SetHeadFacingDirection),
  ];

  public static readonly FrozenSet<Type> SetSlotTranformGlobalOperationGroup = [
    typeof(SetGlobalPosition),
    typeof(SetGlobalPositionRotation),
    typeof(SetGlobalRotation),
    typeof(SetGlobalScale),
    typeof(SetGlobalTransform),
  ];

  public static readonly FrozenSet<Type> SetSlotTranformLocalOperationGroup = [
    typeof(SetLocalPosition),
    typeof(SetLocalPositionRotation),
    typeof(SetLocalRotation),
    typeof(SetLocalScale),
    typeof(SetLocalTransform),
  ];

  public static readonly FrozenSet<Type> CommonPlaybackPropertyGroup = [
    typeof(IsLooped),
    typeof(Position),
    typeof(NormalizedPosition),
    typeof(Speed),
    typeof(IsPlaying),
  ];

  public static readonly FrozenSet<Type> CommonSetPlaybackPropertyGroup = [
    typeof(SetLoop),
    typeof(SetPosition),
    typeof(SetNormalizedPosition),
    typeof(SetSpeed),
  ];
  
  public static readonly FrozenSet<Type> CommonPlaybackActionsGroup = [
    typeof(Play),
    typeof(Pause),
    typeof(Resume),
    typeof(Stop),
  ];

  public static readonly FrozenSet<Type> PlaybackActionsGroup = [
    ..CommonPlaybackActionsGroup,
    typeof(Toggle),
  ];
}