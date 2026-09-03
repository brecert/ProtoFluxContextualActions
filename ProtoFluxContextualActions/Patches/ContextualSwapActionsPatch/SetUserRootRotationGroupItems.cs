using ProtoFluxContextualActions.Tagging;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  internal static IEnumerable<MenuItem> SetUserRootRotationGroupItems(ContextualContext context)
  {
    if (Groups.SetUserRootRotationGroup.Contains(context.NodeType))
    {
      foreach (var match in Groups.SetUserRootRotationGroup)
      {
        yield return new MenuItem(match);
      }
    }
  }
}