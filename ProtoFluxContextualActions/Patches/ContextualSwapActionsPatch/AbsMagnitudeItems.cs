using Elements.Core;

using HarmonyLib;

using ProtoFlux.Runtimes.Execution.Nodes.Math;
using ProtoFlux.Runtimes.Execution.Nodes.Operators;

using ProtoFluxContextualActions.Extensions;
using ProtoFluxContextualActions.Types;
using ProtoFluxContextualActions.Utils;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  private static TypeSet Items = [
    new PsuedoTypeDefinition("Magnitude_"),
    new PsuedoTypeDefinition("SqrMagnitude_"),
    new SystemTypeDefinition(typeof(ValueAbs<>)),
  ];

  internal static IEnumerable<MenuItem> AbsMagnitudeItems(ContextualContext context) =>
    Items.ValidTypesFrom(context.NodeType, context.World.Types).Select(t => new MenuItem(t));

}
