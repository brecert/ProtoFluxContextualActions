using HarmonyLib;
using ProtoFlux.Core;

namespace ProtoFluxContextualActions.Utils.ProtoFlux;

public readonly struct ImpulseElement(INode node, int elementIndex, int? elementListIndex = null) : IElementIndex
{
  public readonly INode OwnerNode = node;

  public readonly int ElementIndex = elementIndex;

  public readonly int? ElementListIndex = elementListIndex;

  public readonly IOperation? Target
  {
    get => GetImpulseTarget();
    set => SetImpulseTarget(value);
  }

  public OperationElement? TargetElement()
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

  internal IOperation? GetImpulseTarget() =>
      ElementListIndex is int listIndex
        ? OwnerNode.GetImpulseList(listIndex).GetImpulseTarget(ElementIndex)
        : OwnerNode.GetImpulseTarget(ElementIndex);

  internal void SetImpulseTarget(IOperation? value)
  {
    if (ElementListIndex is int listIndex)
    {
      OwnerNode.GetImpulseList(listIndex).SetImpulseTarget(ElementIndex, value);
    }
    else
    {
      OwnerNode.SetImpulseTarget(ElementIndex, value);
    }
  }

  public readonly string DisplayName =>
    ElementListIndex is int listIndex
      ? $"{OwnerNode.GetImpulseListName(listIndex)}[{ElementListIndex}]"
      : OwnerNode.GetImpulseName(ElementIndex);

  public readonly ImpulseType TargetType => OwnerNode.GetImpulseType(ElementIndex);

  int IElementIndex.ElementIndex => ElementIndex;

  int? IElementIndex.ElementListIndex => ElementListIndex;

  public override string ToString() =>
    $"ImpulseElement.{TargetType} [{ElementIndex}, {ElementListIndex}] '{DisplayName}' -> {Target}";
}
