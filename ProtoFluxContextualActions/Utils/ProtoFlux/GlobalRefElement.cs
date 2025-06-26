using System;
using ProtoFlux.Core;

namespace ProtoFluxContextualActions.Utils.ProtoFlux;

public readonly struct GlobalRefElement(INode owner, int index, int? elementListIndex = null) : IElementIndex
{
  public readonly INode OwnerNode = owner;

  public readonly int ElementIndex = index;

  public readonly int? ElementListIndex = elementListIndex;

  public readonly Global? Target
  {
    get => OwnerNode.GetGlobalRefBinding(ElementIndex);
    set => OwnerNode.SetGlobalRefBinding(ElementIndex, value);
  }

  public readonly string DisplayName =>
    ElementListIndex is int listIndex
      ? throw new NotImplementedException()
      : OwnerNode.GetGlobalRefName(ElementIndex);


  public readonly Type ValueType => OwnerNode.GetGlobalRefValueType(ElementIndex);

  int IElementIndex.ElementIndex => ElementIndex;

  int? IElementIndex.ElementListIndex => ElementListIndex;
}
