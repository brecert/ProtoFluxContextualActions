using Elements.Core;

using HarmonyLib;

using ProtoFlux.Runtimes.Execution.Nodes.Math;
using ProtoFlux.Runtimes.Execution.Nodes.Operators;

using ProtoFluxContextualActions.Extensions;
using ProtoFluxContextualActions.Utils;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  internal static IEnumerable<MenuItem> AbsMagnitudeItems(ContextualContext context)
  {
    var psuedoGenerics = context.World.GetPsuedoGenericTypesForWorld();
    var magnitudeNodes = psuedoGenerics.Magnitude.ToBiDictionary(a => a.Node, a => a.Types.First());

    if (magnitudeNodes.TryGetSecond(context.NodeType, out var valueType))
    {
      yield return new(typeof(ValueAbs<>).MakeGenericType(valueType));
    }
    else if (TypeUtils.TryGetGenericTypeDefinition(context.NodeType, out var genericType) && genericType == typeof(ValueAbs<>))
    {
      if (magnitudeNodes.TryGetFirst(context.NodeType.GenericTypeArguments[0], out var magnitudeNode))
      {
        yield return new(magnitudeNode);
      }
    }
  }

}
