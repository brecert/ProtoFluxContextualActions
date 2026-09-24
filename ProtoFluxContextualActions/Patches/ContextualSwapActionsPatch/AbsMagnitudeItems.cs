using Elements.Core;

using ProtoFlux.Runtimes.Execution.Nodes.Math;

using ProtoFluxContextualActions.Types;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  internal static TypeSet AbsMagnitudeItemsGroup = [
    PsuedoTypeDefinition.Magnitude,
    PsuedoTypeDefinition.SqrMagnitude,
    new SystemTypeDefinition(typeof(ValueAbs<>)),
  ];

  internal static IEnumerable<MenuItem> AbsMagnitudeItems(ContextualContext context) =>
    AbsMagnitudeItemsGroup.MakeGenericTypesFrom(context).Select(t => new MenuItem(t, connectionTransferType: ConnectionTransferType.ByIndexLossy));
}
