using ProtoFluxContextualActions.Tagging;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  internal static IEnumerable<MenuItem> EasingOfSameKindDoubleItems(ContextualContext context)
  {
    if (EasingGroups.ContainsNodeDouble(context.NodeType))
    {
      // groups now exist, could possibly be changed to have multiple groups for in/out/inout?
      foreach (var match in EasingGroups.GetEasingOfSameKindDouble(context.NodeType))
      {
        yield return new MenuItem(match);
      }
    }
  }
}