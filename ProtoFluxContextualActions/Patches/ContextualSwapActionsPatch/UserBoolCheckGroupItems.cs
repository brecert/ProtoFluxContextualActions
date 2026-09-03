using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Users;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> UserBoolCheckGroup = [
    typeof(IsLocalUser),
    typeof(IsUserHost),
  ];

  internal static IEnumerable<MenuItem> UserBoolCheckGroupItems(ContextualContext context)
  {
    if (UserBoolCheckGroup.Contains(context.NodeType))
    {
      foreach (var match in UserBoolCheckGroup)
      {
        yield return new MenuItem(match);
      }
    }
  }
}