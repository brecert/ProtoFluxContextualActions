using System;
using System.Collections.Generic;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Input.Controllers;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Input.Keyboard;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Slots;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> InputGroup = [
    typeof(StandardController),
    typeof(TouchController),
    typeof(IndexController),
    typeof(ViveController),
    typeof(HPReverbController),
    typeof(CosmosController),
    typeof(WindowsMRController)
  ];

  internal static IEnumerable<MenuItem> InputGroupItems(ContextualContext context)
  {
    if (InputGroup.Contains(context.NodeType))
    {
      foreach (var match in InputGroup)
      {
        yield return new MenuItem(match);
      }
    }
  }
}