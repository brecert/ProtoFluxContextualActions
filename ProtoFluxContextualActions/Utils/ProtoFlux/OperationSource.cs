using ProtoFlux.Core;

namespace ProtoFluxContextualActions.Utils.ProtoFlux;

public readonly struct OperationSource(INode node, int elementIndex, int? elementListIndex = null) : IElementIndex
{
  public readonly INode OwnerNode = node;

  public readonly int ElementIndex = elementIndex;

  public readonly int? ElementListIndex = elementListIndex;

  public readonly IOperation? Target
  {
    get => OwnerNode.GetOperation(ElementIndex);
  }

  public readonly string Name => OwnerNode.GetImpulseName(ElementIndex);

  public readonly ImpulseType TargetType => OwnerNode.GetImpulseType(ElementIndex);

  int IElementIndex.ElementIndex => ElementIndex;

  int? IElementIndex.ElementListIndex => ElementListIndex;

  public override string ToString() =>
    $"OperationSource.{TargetType} [{ElementIndex}, {ElementListIndex}] '{Name}' -> {Target}";
}
