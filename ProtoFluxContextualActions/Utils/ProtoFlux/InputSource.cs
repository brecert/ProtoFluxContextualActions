using System;
using ProtoFlux.Core;

namespace ProtoFluxContextualActions.Utils.ProtoFlux;

public readonly struct InputSource(INode owner, int index)
{
  public readonly INode OwnerNode = owner;

  public readonly int InputIndex = index;

  public readonly IOutput? Source
  {
    get => OwnerNode.GetInputSource(InputIndex);
    set => OwnerNode.SetInputSource(InputIndex, value);
  }

  public readonly string Name => OwnerNode.GetInputName(InputIndex);

  public readonly Type ValueType => OwnerNode.GetInputType(InputIndex);
}
