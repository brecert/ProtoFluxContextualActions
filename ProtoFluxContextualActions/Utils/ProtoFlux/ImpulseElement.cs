using ProtoFlux.Core;

namespace ProtoFluxContextualActions.Utils.ProtoFlux;

public readonly struct ImpulseSource(INode node, int elementIndex, int? elementListIndex = null) : IElementIndex
{
  public readonly INode OwnerNode = node;

  public readonly int ElementIndex = elementIndex;

  public readonly int? ElementListIndex = elementListIndex;

  public readonly IOperation? Target
  {
    get => OwnerNode.GetImpulseTarget(ElementIndex);
    set => OwnerNode.SetImpulseTarget(ElementIndex, value);
  }

  public OperationElement? TargetSource()
  {
    if (Target == null) return null;
    Target.FindOperationIndex(out var index, out var listIndex);
    if (listIndex >= 0)
    {
      return new(Target.OwnerNode, index, listIndex);
    }
    else
    {
      return new(Target.OwnerNode, index, null);
    }
  }

  public readonly string Name => OwnerNode.GetImpulseName(ElementIndex);
  public readonly ImpulseType TargetType => OwnerNode.GetImpulseType(ElementIndex);

  int IElementIndex.ElementIndex => ElementIndex;

  int? IElementIndex.ElementListIndex => ElementListIndex;

  public override string ToString() =>
    $"ImpulseSource.{TargetType} [{ElementIndex}, {ElementListIndex}] '{Name}' -> {Target}";
}
