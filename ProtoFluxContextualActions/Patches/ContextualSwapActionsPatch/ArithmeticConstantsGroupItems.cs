using ProtoFlux.Runtimes.Execution.Nodes.Math.Constants;

using ProtoFluxContextualActions.Types;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  [NodeGroup]
  static readonly SystemTypeSet ArithmeticConstantsGroup = [
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

  [NodeGroup]
  static readonly SystemTypeSet ConversionsConstantsGroup = [
    typeof(RadToDeg),
    typeof(DegToRad),
  ];
}
