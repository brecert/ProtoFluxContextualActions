using ProtoFlux.Runtimes.Execution.Nodes;

using ProtoFluxContextualActions.Types;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  [NodeGroup(ConnectionTransferType.ByIndexLossy)]
  static TypeSet Approximately = [
    PsuedoTypeDefinition.Approximately,
    PsuedoTypeDefinition.ApproximatelyNot,
  ];

  [NodeGroup(ConnectionTransferType.ByIndexLossy)]
  static TypeSet ApproximatelyEquals = [
      PsuedoTypeDefinition.Approximately,
    new SystemTypeDefinition(typeof(ValueEquals<>)),
  ];

  [NodeGroup(ConnectionTransferType.ByIndexLossy)]
  static TypeSet ApproximatelyNotEquals = [
    PsuedoTypeDefinition.ApproximatelyNot,
    new SystemTypeDefinition(typeof(ValueNotEquals<>)),
  ];
}
