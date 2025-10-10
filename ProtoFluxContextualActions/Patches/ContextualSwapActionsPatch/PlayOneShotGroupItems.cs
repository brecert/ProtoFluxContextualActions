using System;
using System.Collections.Generic;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Audio;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> PlayOneShotGroup = [
    typeof(PlayOneShot),
    typeof(PlayOneShotAndWait),
  ];

  internal static IEnumerable<MenuItem> PlayOneShotGroupItems(Type nodeType)
  {
    if (PlayOneShotGroup.Contains(nodeType))
    {
      foreach (var match in PlayOneShotGroup)
      {
        yield return new MenuItem(match);
      }
    }
  }
}