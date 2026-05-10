using System;
using System.Collections.Generic;
using ProtoFlux.Runtimes.Execution.Nodes;
using ProtoFlux.Runtimes.Execution.Nodes.Strings;
using ProtoFluxContextualActions.Utils;
using ProtoFlux.Runtimes.Execution.Nodes.Math.Constants;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> ArithmeticConstantsGroup = [
    typeof(Pi),
    typeof(Tau),
    typeof(e),
    typeof(Phi),
    typeof(HalfPi),
    typeof(QuarterPi),
    typeof(InvertedPi),
    typeof(InvertedHalfPi),
    typeof(InvertedQuarterPi),
  ];

  static readonly HashSet<Type> ConversionsConstantsGroup = [
    typeof(RadToDeg),
    typeof(DegToRad),
  ];

  internal static IEnumerable<MenuItem> ArithmeticConstantsGroupItems(ContextualContext context)
  {
    if (ArithmeticConstantsGroup.Contains(context.NodeType))
    {
      foreach (var match in ArithmeticConstantsGroup)
      {
        yield return new MenuItem(match, connectionTransferType: ConnectionTransferType.ByIndexLossy);
      }
    }
    if (ConversionsConstantsGroup.Contains(context.NodeType))
    {
      foreach (var match in ConversionsConstantsGroup)
      {
        yield return new MenuItem(match, connectionTransferType: ConnectionTransferType.ByIndexLossy);
      }
    }
  }
}