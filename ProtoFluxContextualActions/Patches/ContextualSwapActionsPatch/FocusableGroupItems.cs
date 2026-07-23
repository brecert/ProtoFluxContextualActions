using System;
using System.Collections.Generic;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Interaction.Focusing;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Locomotion;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Users.Roots;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> FocusableGroup = [
    typeof(FocusFocusable),
    typeof(DefocusFocusable),
  ];

  internal static IEnumerable<MenuItem> FocusableGroupItems(ContextualContext context)
  {
    if (FocusableGroup.Contains(context.NodeType))
    {
      foreach (var match in FocusableGroup)
      {
        yield return new MenuItem(match);
      }
    }
  }
}