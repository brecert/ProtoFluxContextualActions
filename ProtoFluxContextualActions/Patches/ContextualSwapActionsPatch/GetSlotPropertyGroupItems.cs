using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using Elements.Core;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Slots;
using ProtoFluxContextualActions.Extensions;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly FrozenSet<Type> GetSlotActiveGroup = [
    typeof(GetSlotActive),
    typeof(GetSlotActiveSelf),
    typeof(SetSlotActiveSelf),
  ];

  static readonly FrozenSet<Type> GetSlotPersistentGroup = [
    typeof(GetSlotPersistent),
    typeof(GetSlotPersistentSelf),
    typeof(SetSlotPersistentSelf),
  ];

  static readonly FrozenSet<Type> ActivePersistentGroup = [
    .. GetSlotActiveGroup,
    .. GetSlotPersistentGroup
  ];

  internal static IEnumerable<MenuItem> GetSlotActiveGroupItems(ContextualContext context) => MatchNonGenericTypes(ActivePersistentGroup, context.NodeType);
}