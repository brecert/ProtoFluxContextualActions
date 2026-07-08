using System;
using System.Collections.Generic;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Slots;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Users;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Worlds;

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

  static readonly HashSet<Type> WorldEventGroup = [
    typeof(WorldFocused),
    typeof(WorldUnFocused),
    typeof(WorldSaved),
    typeof(UserJoined),
    typeof(UserLeft),
    typeof(UserSpawn),
  ];

  internal static IEnumerable<MenuItem> EventGroupItems(ContextualContext context)
  {
    if (EventGroup.Contains(context.NodeType) || WorldEventGroup.Contains(context.NodeType))
    {
      bool isEvent = EventGroup.Contains(context.NodeType);
      foreach (var match in EventGroup)
      {
        yield return new MenuItem(match, group: isEvent ? "" : "Events");
      }
      foreach (var match in WorldEventGroup)
      {
        yield return new MenuItem(match, group: isEvent ? "World Events" : "");
      }
    }
  }
}