using ProtoFlux.Core;
using ProtoFlux.Runtimes.Execution.Nodes.Math;

using ProtoFluxContextualActions.Types;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  [NodeGroup<MultiNodeFormatter>(ConnectionTransferType.ByIndexLossy)]
  static TypeSet AvgGroup = [
    PsuedoTypeDefinition.Avg,
    PsuedoTypeDefinition.AvgMulti,
  ];

  [NodeGroup(ConnectionTransferType.ByIndexLossy)]
  static TypeSet AvgMinMaxGroup = [
    PsuedoTypeDefinition.Avg,
    new SystemTypeDefinition(typeof(ValueMin<>)),
    new SystemTypeDefinition(typeof(ValueMax<>)),
  ];

  [NodeGroup<MultiNodeFormatter>(ConnectionTransferType.ByIndexLossy)]
  static TypeSet AvgMultiMinMaxGroup = [
    PsuedoTypeDefinition.AvgMulti,
    new SystemTypeDefinition(typeof(ValueMinMulti<>)),
    new SystemTypeDefinition(typeof(ValueMaxMulti<>)),
  ];
}
