using System;
using System.Collections.Generic;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Locomotion;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Users.Roots;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> UserLocomotionGroup = [
    typeof(SwitchLocomotionModule),
    typeof(InstallLocomotionModules),
  ];

  internal static IEnumerable<MenuItem> UserLocomotionGroupItems(ContextualContext context)
  {
    if (UserLocomotionGroup.Contains(context.NodeType))
    {
      foreach (var match in UserLocomotionGroup)
      {
        yield return new MenuItem(match);
      }
    }
  }
}