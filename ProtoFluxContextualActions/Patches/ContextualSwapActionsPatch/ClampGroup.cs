using ProtoFlux.Runtimes.Execution.Nodes.Math;

using ProtoFluxContextualActions.Types;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  [NodeGroup]
  static readonly TypeSet ClampGroup = [
    PsuedoTypeDefinition.Clamp01,
    new SystemTypeDefinition(typeof(ValueClamp<>)),
  ];
}
