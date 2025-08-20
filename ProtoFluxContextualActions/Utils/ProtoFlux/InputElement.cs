using System;
using ProtoFlux.Core;

namespace ProtoFluxContextualActions.Utils.ProtoFlux;

public readonly struct InputElement(INode owner, int index, int? listIndex = null) : IElementIndex
{
  public readonly INode OwnerNode = owner;

  public readonly int ElementIndex = index;
  public readonly int? ElementListIndex = listIndex;

  public readonly IOutput? Source
  {
    get => GetInputSource();
    set => SetInputSource(value);
  }

  public OutputElement? SourceElement()
  {
    if (Source == null) return null;
    Source.FindOutputIndex(out var index, out var listIndex);
    if (listIndex >= 0)
    {
      return new(Source.OwnerNode, index, listIndex);
    }
    else
    {
      return new(Source.OwnerNode, index, null);
    }
  }

  internal IOutput? GetInputSource() =>
      ElementListIndex is int listIndex
        ? OwnerNode.GetInputList(listIndex).GetInputSource(ElementIndex)
        : OwnerNode.GetInputSource(ElementIndex);

  internal void SetInputSource(IOutput? value)
  {
    if (ElementListIndex is int listIndex)
    {
      OwnerNode.GetInputList(listIndex).SetInputSource(ElementIndex, value);
    }
    else
    {
      OwnerNode.SetInputSource(ElementIndex, value);
    }
  }

  public readonly string DisplayName =>
    ElementListIndex is int listIndex
      ? $"{OwnerNode.GetInputListName(listIndex)}[{ElementIndex}]"
      : OwnerNode.GetInputName(ElementIndex);

  public readonly Type ValueType => OwnerNode.GetInputType(ElementIndex);

  int IElementIndex.ElementIndex => ElementIndex;

  int? IElementIndex.ElementListIndex => ElementListIndex;

  public override string ToString() =>
    $"InputElement.{ValueType} [{ElementIndex}, {ElementListIndex}] '{DisplayName}' <- {Source}";
}
