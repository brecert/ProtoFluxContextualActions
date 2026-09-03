using ProtoFluxContextualActions.Tagging;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  internal static IEnumerable<MenuItem> UserRootRotationGroupItems(ContextualContext context)
  {
    if (Groups.UserRootRotationGroup.Contains(context.NodeType))
    {
      foreach (var match in Groups.UserRootRotationGroup)
      {
        yield return new MenuItem(match);
      }
    }
  }

}