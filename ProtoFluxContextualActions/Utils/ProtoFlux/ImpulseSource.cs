using ProtoFlux.Core;

namespace ProtoFluxContextualActions.Utils.ProtoFlux;

public readonly struct ImpulseSource
{
  public ImpulseSource(INode node, int index)
  {
    OwnerNode = node;
    ImpulseIndex = index;
  }

  public ImpulseSource(INode node, IImpulse impulse)
  {
    OwnerNode = node;
    ImpulseIndex = node.Metadata.FixedImpulses.Find(m => m.Field.GetValue(node).GetHashCode() == impulse.GetHashCode()).Index;
  }

  public ImpulseSource(INode node, string name)
  {
    OwnerNode = node;
    ImpulseIndex = node.Metadata.GetImpulseByName(name).Index;
  }

  public readonly INode OwnerNode;
  public readonly int ImpulseIndex;

  public readonly IOperation? Target
  {
    get => OwnerNode.GetImpulseTarget(ImpulseIndex);
    set => OwnerNode.SetImpulseTarget(ImpulseIndex, value);
  }

  public readonly string Name => OwnerNode.GetImpulseName(ImpulseIndex);

  public readonly ImpulseType TargetType => OwnerNode.GetImpulseType(ImpulseIndex);

  public override string ToString()
  {
    return $"ImpulseSource.{TargetType} [{ImpulseIndex}] '{Name}' -> {Target}";
  }
}
