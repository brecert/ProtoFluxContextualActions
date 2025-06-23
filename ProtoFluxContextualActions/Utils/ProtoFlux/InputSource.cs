using System;
using ProtoFlux.Core;

namespace ProtoFluxContextualActions.Utils.ProtoFlux;

public readonly struct InputSource(INode owner, int index, int? listIndex = null) : IElementIndex
{
  public readonly INode OwnerNode = owner;

  public readonly int ElementIndex = index;
  public readonly int? ElementListIndex = listIndex;

  public readonly IOutput? Source
  {
    get => OwnerNode.GetInputSource(ElementIndex);
    set => OwnerNode.SetInputSource(ElementIndex, value);
  }

  public OutputSource? OutputSource()
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


  public readonly string Name => OwnerNode.GetInputName(ElementIndex);

  public readonly Type ValueType => OwnerNode.GetInputType(ElementIndex);

  int IElementIndex.ElementIndex => ElementIndex;

  int? IElementIndex.ElementListIndex => ElementListIndex;

  public override string ToString() =>
    $"ImpulseSource.{ValueType} [{ElementIndex}, {ElementListIndex}] '{Name}' <- {Source}";
}
