using System;
using System.Collections.Generic;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Transform;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> GetDirectionGroup = [
    typeof(GetForward),
    typeof(GetBackward),
    typeof(GetUp),
    typeof(GetDown),
    typeof(GetLeft),
    typeof(GetRight)
  ];

  private static IEnumerable<MenuItem> GetDirectionGroupItems(Type nodeType)
  {
    if (GetDirectionGroup.Contains(nodeType))
    {
      foreach (var match in GetDirectionGroup)
      {
        yield return new(match);
      }
    }
  }
}