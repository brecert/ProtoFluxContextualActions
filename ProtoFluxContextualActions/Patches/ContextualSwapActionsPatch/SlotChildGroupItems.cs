using System.Collections.Frozen;

using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Slots;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly FrozenSet<Type> SlotChildGroup = [
    typeof(GetChild),
    typeof(GetObjectRoot),
    typeof(GetParentSlot),
    typeof(IsChildOf), // doesn't really fit with the rest but the issue requests this.
    typeof(SetParent), // this allows for GetParent<->SetParent, but could probably be split into a seperate group entirely.
  ];

  internal static IEnumerable<MenuItem> SlotChildGroupItems(ContextualContext context) =>
    MatchNonGenericTypes(SlotChildGroup, context.NodeType);
}