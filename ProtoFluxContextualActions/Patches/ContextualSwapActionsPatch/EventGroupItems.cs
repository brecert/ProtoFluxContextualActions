using System;
using System.Collections.Generic;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Slots;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Users;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> EventGroup = [
    typeof(OnLoaded),
    typeof(OnStart),
    typeof(OnPaste),
    typeof(OnPackageImported),
    typeof(OnDestroy),
    typeof(OnDestroying),
    typeof(OnDuplicate),
    typeof(OnDeactivated),
  ];

  internal static IEnumerable<MenuItem> EventGroupItems(ContextualContext context)
  {
    if (UserInfoGroup.Contains(context.NodeType))
    {
      foreach (var match in UserInfoGroup)
      {
        yield return new MenuItem(match);
      }
    }
  }
}