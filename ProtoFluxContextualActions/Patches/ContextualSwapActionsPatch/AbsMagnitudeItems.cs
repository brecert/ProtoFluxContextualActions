using ProtoFlux.Runtimes.Execution.Nodes.Math;

using ProtoFluxContextualActions.Types;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  [NodeGroup(ConnectionTransferType.ByIndexLossy)]
  static readonly TypeSet AbsMagnitudeItemsGroup = [
    PsuedoTypeDefinition.Magnitude,
    PsuedoTypeDefinition.SqrMagnitude,
    new SystemTypeDefinition(typeof(ValueAbs<>)),
  ];
}
