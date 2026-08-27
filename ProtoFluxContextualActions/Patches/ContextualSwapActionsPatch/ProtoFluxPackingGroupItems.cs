using System;
using System.Collections.Generic;
using ProtoFlux.Runtimes.Execution.Nodes.Actions;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Debugging;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Network;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Nodes;
using ProtoFlux.Runtimes.Execution.Nodes.TimeAndDate;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> ProtoFluxPackingGroup = [
    typeof(UnpackProtoFlux),
    typeof(PackProtoFluxFromNode),
    typeof(PackProtoFluxInPlace),
    typeof(PackProtoFluxNodes),
  ];
  internal static IEnumerable<MenuItem> ProtoFluxPackingGroupItems(ContextualContext context)
  {
    if (ProtoFluxPackingGroup.Contains(context.NodeType))
    {
      foreach (var match in ProtoFluxPackingGroup)
      {
        yield return new MenuItem(match);
      }
    }
  }
}