using System;
using ProtoFlux.Core;

namespace ProtoFluxContextualActions.Utils.ProtoFlux;

public readonly struct OutputElement(INode node, int elementIndex, int? elementListIndex = null) : IElementIndex
{
  public readonly INode OwnerNode = node;

  public readonly int ElementIndex = elementIndex;

  public readonly int? ElementListIndex = elementListIndex;

  public readonly IOutput? Target
  {
    get => OwnerNode.GetOutput(ElementIndex);
  }

  public readonly string Name => OwnerNode.GetOutputName(ElementIndex);

  public readonly DataClass DataClass => OwnerNode.GetOutputTypeClass(ElementIndex);

  public readonly Type ValueType => OwnerNode.GetOutputType(ElementIndex);

  int IElementIndex.ElementIndex => ElementIndex;

  int? IElementIndex.ElementListIndex => ElementListIndex;

  public override string ToString() =>
    $"OperationSource.{DataClass} [{ElementIndex}, {ElementListIndex}] '{Name}' -> {Target}";
}
