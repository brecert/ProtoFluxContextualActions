using ProtoFluxContextualActions.Tagging;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  internal static IEnumerable<MenuItem> EasingOfSameKindFloatItems(ContextualContext context)
  {
    if (EasingGroups.ContainsNodeFloat(context.NodeType))
    {
      // groups now exist, could possibly be changed to have multiple groups for in/out/inout?
      // may also be able to add other easing/shaping nodes, like SmoothStep/SmootherStep, SymmetricPowShape, Sigmoid, and any other similar functions.
      foreach (var match in EasingGroups.GetEasingOfSameKindFloat(context.NodeType))
      {
        yield return new MenuItem(match);
      }
    }
  }
}