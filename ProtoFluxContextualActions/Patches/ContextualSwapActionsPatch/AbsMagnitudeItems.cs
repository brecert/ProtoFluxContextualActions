using Elements.Core;

using ProtoFlux.Runtimes.Execution.Nodes.Math;

using ProtoFluxContextualActions.Types;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  private static TypeSet Items = [
    new PsuedoTypeDefinition("Magnitude_"),
    new PsuedoTypeDefinition("SqrMagnitude_"),
    new SystemTypeDefinition(typeof(ValueAbs<>)),
  ];

  internal static IEnumerable<MenuItem> AbsMagnitudeItems(ContextualContext context) =>
    Items.MakeGenericTypesFrom(context).Select(t => new MenuItem(t, connectionTransferType: ConnectionTransferType.ByIndexLossy));
}
