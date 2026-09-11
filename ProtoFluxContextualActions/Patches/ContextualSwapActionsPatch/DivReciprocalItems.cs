using Elements.Core;

using HarmonyLib;

using ProtoFlux.Runtimes.Execution.Nodes.Operators;

using ProtoFluxContextualActions.Utils;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> DivReciprocalGroup = [
    typeof(ValueDiv<>),
    typeof(ValueReciprocal<>),
  ];

  internal static IEnumerable<MenuItem> DivReciprocalItems(ContextualContext context)
  {
    if (TypeUtils.TryGetGenericTypeDefinition(context.NodeType, out var genericType) && DivReciprocalGroup.Contains(genericType))
    {
      var opType = context.NodeType.GenericTypeArguments[0];
      yield return new MenuItem(typeof(ValueDiv<>).MakeGenericType(opType), connectionTransferType: ConnectionTransferType.ByIndexLossy);
      yield return new MenuItem(typeof(ValueReciprocal<>).MakeGenericType(opType), connectionTransferType: ConnectionTransferType.ByIndexLossy);
    }
  }

}
