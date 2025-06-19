using System;
using ProtoFlux.Core;

namespace ProtoFluxContextualActions.Utils.ProtoFlux;

  public readonly struct ReferenceSource(INode node, int index)
  {
    public readonly INode OwnerNode = node;
    public readonly int ReferenceIndex = index;

    public readonly INode? Target
    {
      get => OwnerNode.GetReferenceTarget(ReferenceIndex);
      set => OwnerNode.SetReferenceTarget(ReferenceIndex, value);
    }

    public readonly string Name => OwnerNode.GetReferenceName(ReferenceIndex);

    public readonly Type TargetType => OwnerNode.GetReferenceType(ReferenceIndex);

    public override string ToString()
    {
      return $"ReferenceSource.{TargetType} [{ReferenceIndex}] '{Name}' -> {Target}";
    }
  }
