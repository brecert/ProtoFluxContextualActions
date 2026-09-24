using Elements.Core;

using ProtoFlux.Runtimes.Execution.Nodes;

using ProtoFluxContextualActions.Extensions;
using ProtoFluxContextualActions.Types;
using ProtoFluxContextualActions.Utils;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  private static TypeSet ItemsA = [
    PsuedoTypeDefinition.Approximately,
    PsuedoTypeDefinition.ApproximatelyNot,
  ];

  private static TypeSet ItemsB = [
    PsuedoTypeDefinition.Approximately,
    new SystemTypeDefinition(typeof(ValueEquals<>)),
  ];

  private static TypeSet ItemsC = [
    PsuedoTypeDefinition.ApproximatelyNot,
    new SystemTypeDefinition(typeof(ValueNotEquals<>)),
  ];

  internal static IEnumerable<MenuItem> ApproximatelyGroupItems(ContextualContext context) =>
    ((IEnumerable<Type>)[
      ..ItemsA.MakeGenericTypesFrom(context),
      ..ItemsB.MakeGenericTypesFrom(context),
      ..ItemsC.MakeGenericTypesFrom(context),
    ])
      .Select(t => new MenuItem(t, connectionTransferType: ConnectionTransferType.ByIndexLossy));
}
