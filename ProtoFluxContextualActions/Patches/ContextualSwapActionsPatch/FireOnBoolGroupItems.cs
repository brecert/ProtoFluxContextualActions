using System;
using System.Collections.Generic;
using ProtoFlux.Runtimes.Execution.Nodes.Actions;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Slots;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> FireOnBoolGroup = [
    typeof(FireOnTrue),
    typeof(FireOnFalse),
  ];

  internal static IEnumerable<MenuItem> FireOnBoolGroupItems(ContextualContext context)
  {
    if (FireOnBoolGroup.Contains(context.NodeType))
    {
      foreach (var match in FireOnBoolGroup)
      {
        yield return new MenuItem(match);
      }
    }
  }
}