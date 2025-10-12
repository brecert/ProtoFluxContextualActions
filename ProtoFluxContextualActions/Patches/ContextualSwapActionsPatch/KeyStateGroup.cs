using System;
using System.Collections.Generic;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Input.Keyboard;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Slots;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> KeyStateGroup = [
    typeof(KeyHeld),
    typeof(KeyPressed),
    typeof(KeyReleased),
  ];

  internal static IEnumerable<MenuItem> KeyStateGroupItems(ContextualContext context)
  {
    if (KeyStateGroup.Contains(context.NodeType))
    {
      foreach (var match in KeyStateGroup)
      {
        yield return new MenuItem(match);
      }
    }
  }
}