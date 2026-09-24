using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Transform;

using ProtoFluxContextualActions.Types;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  [NodeGroup]
  static readonly SystemTypeSet GetDirectionGroup = [
    typeof(GetForward),
    typeof(GetBackward),
    typeof(GetUp),
    typeof(GetDown),
    typeof(GetLeft),
    typeof(GetRight)
  ];

  [NodeGroup]
  static readonly SystemTypeSet SetDirectionGroup = [
    typeof(SetForward),
    typeof(SetBackward),
    typeof(SetUp),
    typeof(SetDown),
    typeof(SetLeft),
    typeof(SetRight)
  ];

  [NodeGroup]
  static readonly TypeMap GetSetDirectionEquivilents =
    GetDirectionGroup.MapTo(SetDirectionGroup, bidirectional: true);
}
