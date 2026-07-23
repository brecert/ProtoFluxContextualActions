using System;
using System.Collections.Generic;
using ProtoFlux.Runtimes.Execution.Nodes.Actions;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Debugging;
using ProtoFlux.Runtimes.Execution.Nodes.TimeAndDate;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> DebugGroup = [
    typeof(DebugSphere),
    typeof(DebugVector),
    typeof(DebugBox),
    typeof(DebugText),
    typeof(DebugAxes),
    typeof(DebugLine),
    typeof(DebugTriangle),
  ];
  internal static IEnumerable<MenuItem> DebugGroupItems(ContextualContext context)
  {
    if (DebugGroup.Contains(context.NodeType))
    {
      foreach (var match in DebugGroup)
      {
        yield return new MenuItem(match);
      }
    }
  }
}