using System;
using System.Collections.Generic;
using System.Linq;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Playback;
using ProtoFlux.Runtimes.Execution.Nodes.TimeAndDate;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> PlayablePositionGroup = [
    typeof(ShiftPosition),
    typeof(SetPosition),
    typeof(Position),
    typeof(NormalizedPosition),
    typeof(SetNormalizedPosition),
  ];
  static readonly HashSet<Type> PlayableSpeedGroup = [
    typeof(SetSpeed),
    typeof(Speed),
  ];

  static readonly HashSet<Type> PlayableMetaGroup = [
    typeof(ClipLengthFloat),
    typeof(ClipLengthDouble),
    typeof(SetLoop),
    typeof(IsLooped),
    typeof(IsPlaying),
    typeof(PlaybackState),
  ];

  static readonly HashSet<Type> PlayableActionsGroup = [
    typeof(Play),
    typeof(Pause),
    typeof(Resume),
    typeof(Toggle),
    typeof(Stop),
    typeof(PlayAndWait),
  ];

  internal static IEnumerable<MenuItem> IPlayableGroupItems(ContextualContext context)
  {
    if (PlayablePositionGroup.Contains(context.NodeType))
    {
      foreach (var match in PlayablePositionGroup)
      {
        yield return new MenuItem(match);
      }
    }
    if (PlayableSpeedGroup.Contains(context.NodeType))
    {
      foreach (var match in PlayableSpeedGroup)
      {
        yield return new MenuItem(match);
      }
    }
    if (PlayableMetaGroup.Contains(context.NodeType))
    {
      foreach (var match in PlayableMetaGroup)
      {
        yield return new MenuItem(match);
      }
    }
    if (PlayableActionsGroup.Contains(context.NodeType))
    {
      foreach (var match in PlayableActionsGroup)
      {
        yield return new MenuItem(match);
      }
    }
  }
}