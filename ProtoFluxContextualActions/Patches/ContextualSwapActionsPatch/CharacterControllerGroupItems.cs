using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Physics;

using ProtoFluxContextualActions.Types;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  [NodeGroup]
  static readonly SystemTypeSet CharacterControllerGroup = [
    typeof(ApplyCharacterImpulse),
    typeof(ApplyCharacterForce),
    typeof(SetCharacterVelocity)
  ];

  [NodeGroup]
  static readonly SystemTypeSet FindCharacterControllerGroup = [
    typeof(FindCharacterControllerFromSlot),
    typeof(FindCharacterControllerFromUser),
  ];

  [NodeGroup]
  static readonly SystemTypeSet CharacterControllerGravityGroup = [
    typeof(CharacterGravity),
    typeof(SetCharacterGravity),
  ];
}
