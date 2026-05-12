using System;
using System.Collections.Generic;
using Elements.Core;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Slots;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Users;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Users.Roots;
using ProtoFlux.Runtimes.Execution.Nodes.Math;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly BiDictionary<Type, Type> RemapGroup = new() {
    { typeof(Remap_Float), typeof(Remap11_01_Float) },
    { typeof(Remap_Double), typeof(Remap11_01_Double) },
  };

  internal static IEnumerable<MenuItem> RemapGroupItems(ContextualContext context)
  {
    if (TryGetSwap(RemapGroup, context.NodeType, out Type match)) yield return new MenuItem(match);
  }
}