using Elements.Core;

using HarmonyLib;

using ProtoFlux.Runtimes.Execution.Nodes.Operators;

using ProtoFluxContextualActions.Types;
using ProtoFluxContextualActions.Utils;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  [NodeGroup]
  static readonly SystemTypeSet DivReciprocalGroup = [
    typeof(ValueDiv<>),
    typeof(ValueReciprocal<>),
  ];
}
