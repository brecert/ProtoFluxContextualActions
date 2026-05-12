using System;
using System.Collections.Generic;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Playback;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> PlaybackGroup = [
    typeof(Play),
    typeof(Pause),
    typeof(Resume),
    typeof(Stop),
    typeof(Toggle),
  ];

  internal static IEnumerable<MenuItem> PlaybackGroupItems(ContextualContext context)
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