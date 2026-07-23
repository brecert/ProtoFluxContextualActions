using System;
using System.Collections.Generic;
using System.Linq;
using Elements.Core;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Transform;
using ProtoFluxContextualActions.Extensions;
using ProtoFluxContextualActions.Tagging;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly BiDictionary<Type, Type> SetGlobalLocalEquivilents = new()
  {
    {typeof(SetGlobalPosition), typeof(SetLocalPosition)},
    {typeof(SetGlobalPositionRotation), typeof(SetLocalPositionRotation)},
    {typeof(SetGlobalRotation), typeof(SetLocalRotation)},
    {typeof(SetGlobalScale), typeof(SetLocalScale)},
    {typeof(SetGlobalTransform), typeof(SetLocalTransform)},
  };

  // For some reason, this seems to have the order be shuffled.
  // static readonly BiDictionary<Type, Type> SetGlobalLocalEquivilents =
  //   Groups.SetSlotTranformGlobalOperationGroup.Zip(Groups.SetSlotTranformLocalOperationGroup).ToBiDictionary();

  static readonly BiDictionary<Type, Type> GetGlobalLocalEquivilents = new()
  {
    {typeof(GlobalTransform), typeof(LocalTransform)}
  };

  static readonly BiDictionary<Type, Type> GetSetTransformEquivilents = new()
  {
    {typeof(GlobalTransform), typeof(SetGlobalTransform)},
    {typeof(LocalTransform), typeof(SetLocalTransform)}
  };

  internal static IEnumerable<MenuItem> GlobalLocalEquivilentSwapGroups(Type nodeType)
  {
    if (TryGetSwap(SetGlobalLocalEquivilents, nodeType, out Type match)) yield return new(match);
    if (TryGetSwap(GetGlobalLocalEquivilents, nodeType, out match)) yield return new(match, connectionTransferType: ConnectionTransferType.ByIndexLossy);
    if (TryGetSwap(GetSetTransformEquivilents, nodeType, out match)) yield return new(match);
  }
}