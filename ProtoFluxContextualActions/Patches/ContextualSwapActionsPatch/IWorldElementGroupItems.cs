using System;
using System.Collections.Generic;
using ProtoFlux.Runtimes.Execution.Nodes.Actions;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Debugging;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Elements;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.References;
using ProtoFlux.Runtimes.Execution.Nodes.TimeAndDate;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> IWorldElementGroup = [
    typeof(IsDestroyed),
    typeof(IsRemoved),
    typeof(AllocatingUser),
  ];
  internal static IEnumerable<MenuItem> IWorldElementGroupItems(ContextualContext context)
  {
    if (IWorldElementGroup.Contains(context.NodeType))
    {
      foreach (var match in IWorldElementGroup)
      {
        yield return new MenuItem(match);
      }
    }
  }
}