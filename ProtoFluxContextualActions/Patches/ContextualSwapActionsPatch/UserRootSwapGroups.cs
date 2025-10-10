using System;
using System.Collections.Generic;
using System.Linq;
using Elements.Core;
using ProtoFluxContextualActions.Extensions;
using ProtoFluxContextualActions.Tagging;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly BiDictionary<Type, Type> GetUserRootSwapGroup =
    Groups.UserRootPositionGroup.Zip(Groups.UserRootRotationGroup).ToBiDictionary();

  static readonly BiDictionary<Type, Type> UserRootPositionSwapGroup =
    Groups.UserRootPositionGroup.Zip(Groups.SetUserRootPositionGroup).ToBiDictionary();

  static readonly BiDictionary<Type, Type> UserRootRotationSwapGroup =
    Groups.UserRootRotationGroup.Zip(Groups.SetUserRootRotationGroup).ToBiDictionary();

  static readonly BiDictionary<Type, Type> SetUserRootSwapGroup =
    Groups.SetUserRootPositionGroup.Zip(Groups.SetUserRootRotationGroup).ToBiDictionary();

  static readonly BiDictionary<Type, Type> UserRootHeadRotationSwapGroup =
    Groups.UserRootHeadRotationGroup.Zip(Groups.SetUserRootHeadRotationGroup).ToBiDictionary();

  internal static IEnumerable<MenuItem> UserRootSwapGroups(Type nodeType)
  {
    if (TryGetSwap(GetUserRootSwapGroup, nodeType, out Type match)) yield return new(match);
    if (TryGetSwap(UserRootPositionSwapGroup, nodeType, out match)) yield return new(match);
    if (TryGetSwap(UserRootRotationSwapGroup, nodeType, out match)) yield return new(match);
    if (TryGetSwap(SetUserRootSwapGroup, nodeType, out match)) yield return new(match);
    if (TryGetSwap(UserRootHeadRotationSwapGroup, nodeType, out match)) yield return new(match);
  }
}