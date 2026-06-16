using System;
using System.Collections.Generic;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Physics;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Slots;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> CharacterControllerGroup = [
    typeof(ApplyCharacterImpulse),
    typeof(ApplyCharacterForce),
    typeof(SetCharacterVelocity)
  ];

  static readonly HashSet<Type> FindCharacterControllerGroup = [
    typeof(FindCharacterControllerFromSlot),
    typeof(FindCharacterControllerFromUser),
  ];

  static readonly HashSet<Type> CharacterControllerGravityGroup = [
    typeof(CharacterGravity),
    typeof(SetCharacterGravity),
  ];

  internal static IEnumerable<MenuItem> CharacterControllerGroupItems(ContextualContext context)
  {
    if (CharacterControllerGroup.Contains(context.NodeType))
    {
      foreach (var match in CharacterControllerGroup)
      {
        yield return new MenuItem(match);
      }
    }

    if (FindCharacterControllerGroup.Contains(context.NodeType))
    {
      foreach (var match in FindCharacterControllerGroup)
      {
        yield return new MenuItem(match);
      }
    }

    if (CharacterControllerGravityGroup.Contains(context.NodeType))
    {
      foreach (var match in CharacterControllerGravityGroup)
      {
        yield return new MenuItem(match);
      }
    }
  }
}