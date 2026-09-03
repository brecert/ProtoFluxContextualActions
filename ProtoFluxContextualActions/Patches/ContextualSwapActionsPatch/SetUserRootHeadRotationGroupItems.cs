using ProtoFluxContextualActions.Tagging;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  internal static IEnumerable<MenuItem> SetUserRootHeadRotationGroupItems(ContextualContext context)
  {
    if (Groups.SetUserRootHeadRotationGroup.Contains(context.NodeType))
    {
      foreach (var match in Groups.SetUserRootHeadRotationGroup)
      {
        yield return new MenuItem(match);
      }
    }
  }
}