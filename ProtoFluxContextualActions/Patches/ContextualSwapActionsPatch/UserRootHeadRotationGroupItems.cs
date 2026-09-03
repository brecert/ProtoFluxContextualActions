using ProtoFluxContextualActions.Tagging;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  internal static IEnumerable<MenuItem> UserRootHeadRotationGroupItems(ContextualContext context)
  {
    if (Groups.UserRootHeadRotationGroup.Contains(context.NodeType))
    {
      foreach (var match in Groups.UserRootHeadRotationGroup)
      {
        yield return new MenuItem(match);
      }
    }
  }
}