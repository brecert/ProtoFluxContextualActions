using ProtoFluxContextualActions.Tagging;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  internal static IEnumerable<MenuItem> UserRootPositionGroupItems(ContextualContext context)
  {
    if (Groups.UserRootPositionGroup.Contains(context.NodeType))
    {
      foreach (var match in Groups.UserRootPositionGroup)
      {
        yield return new MenuItem(match);
      }
    }
  }
}