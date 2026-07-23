using System;
using System.Collections.Generic;
using ProtoFlux.Runtimes.Execution.Nodes.Actions;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Debugging;
using ProtoFlux.Runtimes.Execution.Nodes.Math.Rects;
using ProtoFlux.Runtimes.Execution.Nodes.TimeAndDate;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> RectFromGroup = [
    typeof(RectFromPositionSize),
    typeof(RectFromMinMax),
  ];

  static readonly HashSet<Type> RectToGroup = [
    typeof(RectToPositionSize),
    typeof(RectToMinMax),
  ];

  static readonly HashSet<Type> RectSizeGroup = [
    typeof(ClipRect),
    typeof(EncapsulateRect),
  ];

  internal static IEnumerable<MenuItem> RectGroupItems(ContextualContext context)
  {
    if (RectFromGroup.Contains(context.NodeType))
    {
      foreach (var match in RectFromGroup)
      {
        yield return new MenuItem(match, connectionTransferType: ConnectionTransferType.ByIndexLossy);
      }
    }
    if (RectToGroup.Contains(context.NodeType))
    {
      foreach (var match in RectToGroup)
      {
        yield return new MenuItem(match, connectionTransferType: ConnectionTransferType.ByIndexLossy);
      }
    }
    if (RectSizeGroup.Contains(context.NodeType))
    {
      foreach (var match in RectSizeGroup)
      {
        yield return new MenuItem(match, connectionTransferType: ConnectionTransferType.ByIndexLossy);
      }
    }
  }
}