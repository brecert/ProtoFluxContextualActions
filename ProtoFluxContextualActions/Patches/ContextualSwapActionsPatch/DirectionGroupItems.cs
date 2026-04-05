using System;
using System.Collections.Generic;
using System.Linq;
using Elements.Core;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Transform;
using ProtoFluxContextualActions.Extensions;

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

  static readonly HashSet<Type> SetDirectionGroup = [
    typeof(SetForward),
    typeof(SetBackward),
    typeof(SetUp),
    typeof(SetDown),
    typeof(SetLeft),
    typeof(SetRight)
  ];

  static readonly BiDictionary<Type, Type> GetSetDirectionEquivilents =
    GetDirectionGroup.Zip(SetDirectionGroup).ToBiDictionary();

  private static IEnumerable<MenuItem> DirectionGroupItems(ContextualContext context)
  {
    if (GetDirectionGroup.Contains(context.NodeType))
    {
      foreach (var getMatch in GetDirectionGroup)
      {
        yield return new(getMatch);
      }
    }
    if (SetDirectionGroup.Contains(context.NodeType))
    {
      foreach (var setMatch in SetDirectionGroup)
      {
        yield return new(setMatch);
      }
    }
    if (TryGetSwap(GetSetDirectionEquivilents, context.NodeType, out Type match)) yield return new(match);
  }
}