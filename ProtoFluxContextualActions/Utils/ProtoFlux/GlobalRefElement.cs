using System;
using ProtoFlux.Core;

namespace ProtoFluxContextualActions.Utils.ProtoFlux;

public readonly struct GlobalRefElement(INode owner, int index)
{
  public readonly INode OwnerNode = owner;

  public readonly int RefIndex = index;

  public readonly Global? Target
  {
    get => OwnerNode.GetGlobalRefBinding(RefIndex);
    set => OwnerNode.SetGlobalRefBinding(RefIndex, value);
  }

  public readonly string Name => OwnerNode.GetGlobalRefName(RefIndex);

  public readonly Type ValueType => OwnerNode.GetGlobalRefValueType(RefIndex);
}
