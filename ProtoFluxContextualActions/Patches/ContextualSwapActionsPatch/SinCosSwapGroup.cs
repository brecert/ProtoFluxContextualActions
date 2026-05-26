using System;
using System.Collections.Generic;
using System.Linq;
using Elements.Core;
using ProtoFluxContextualActions.Utils;
namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  internal static IEnumerable<MenuItem> SinCosSwapGroup(ContextualContext context)
  {
    var psuedoGenerics = context.World.GetPsuedoGenericTypesForWorld();

    // tried a bit to find a way to not hardcode this.
    // couldnt find anything however, so hardcoding it is.

    // Normal
    if (psuedoGenerics.Sin.Any(n => n.Node == context.NodeType))
    {
      var types = psuedoGenerics.Sin.First(n => n.Node == context.NodeType).Types;
      yield return new MenuItem(psuedoGenerics.Cos.First(n => n.Types.SequenceEqual(types)).Node);
      yield return new MenuItem(psuedoGenerics.Tan.First(n => n.Types.SequenceEqual(types)).Node);
      yield return new MenuItem(psuedoGenerics.Asin.First(n => n.Types.SequenceEqual(types)).Node);
    }
    if (psuedoGenerics.Cos.Any(n => n.Node == context.NodeType))
    {
      var types = psuedoGenerics.Cos.First(n => n.Node == context.NodeType).Types;
      yield return new MenuItem(psuedoGenerics.Sin.First(n => n.Types.SequenceEqual(types)).Node);
      yield return new MenuItem(psuedoGenerics.Tan.First(n => n.Types.SequenceEqual(types)).Node);
      yield return new MenuItem(psuedoGenerics.Acos.First(n => n.Types.SequenceEqual(types)).Node);
    }
    if (psuedoGenerics.Tan.Any(n => n.Node == context.NodeType))
    {
      var types = psuedoGenerics.Tan.First(n => n.Node == context.NodeType).Types;
      yield return new MenuItem(psuedoGenerics.Sin.First(n => n.Types.SequenceEqual(types)).Node);
      yield return new MenuItem(psuedoGenerics.Cos.First(n => n.Types.SequenceEqual(types)).Node);
      yield return new MenuItem(psuedoGenerics.Atan.First(n => n.Types.SequenceEqual(types)).Node);
      yield return new MenuItem(psuedoGenerics.Atan2.First(n => n.Types.SequenceEqual(types)).Node);
    }

    // Inverse
    if (psuedoGenerics.Asin.Any(n => n.Node == context.NodeType))
    {
      var types = psuedoGenerics.Asin.First(n => n.Node == context.NodeType).Types;
      yield return new MenuItem(psuedoGenerics.Sin.First(n => n.Types.SequenceEqual(types)).Node);
      yield return new MenuItem(psuedoGenerics.Acos.First(n => n.Types.SequenceEqual(types)).Node);
      yield return new MenuItem(psuedoGenerics.Atan.First(n => n.Types.SequenceEqual(types)).Node);
      yield return new MenuItem(psuedoGenerics.Atan2.First(n => n.Types.SequenceEqual(types)).Node);
    }
    if (psuedoGenerics.Acos.Any(n => n.Node == context.NodeType))
    {
      var types = psuedoGenerics.Acos.First(n => n.Node == context.NodeType).Types;
      yield return new MenuItem(psuedoGenerics.Cos.First(n => n.Types.SequenceEqual(types)).Node);
      yield return new MenuItem(psuedoGenerics.Asin.First(n => n.Types.SequenceEqual(types)).Node);
      yield return new MenuItem(psuedoGenerics.Atan.First(n => n.Types.SequenceEqual(types)).Node);
      yield return new MenuItem(psuedoGenerics.Atan2.First(n => n.Types.SequenceEqual(types)).Node);
    }
    if (psuedoGenerics.Atan.Any(n => n.Node == context.NodeType))
    {
      var types = psuedoGenerics.Atan.First(n => n.Node == context.NodeType).Types;
      yield return new MenuItem(psuedoGenerics.Atan2.First(n => n.Types.SequenceEqual(types)).Node);
      yield return new MenuItem(psuedoGenerics.Tan.First(n => n.Types.SequenceEqual(types)).Node);
      yield return new MenuItem(psuedoGenerics.Asin.First(n => n.Types.SequenceEqual(types)).Node);
      yield return new MenuItem(psuedoGenerics.Acos.First(n => n.Types.SequenceEqual(types)).Node);
    }
    if (psuedoGenerics.Atan2.Any(n => n.Node == context.NodeType))
    {
      var types = psuedoGenerics.Atan2.First(n => n.Node == context.NodeType).Types;
      yield return new MenuItem(psuedoGenerics.Atan.First(n => n.Types.SequenceEqual(types)).Node);
      yield return new MenuItem(psuedoGenerics.Tan.First(n => n.Types.SequenceEqual(types)).Node);
      yield return new MenuItem(psuedoGenerics.Asin.First(n => n.Types.SequenceEqual(types)).Node);
      yield return new MenuItem(psuedoGenerics.Acos.First(n => n.Types.SequenceEqual(types)).Node);
    }
  }
}