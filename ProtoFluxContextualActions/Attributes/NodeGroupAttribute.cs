using static ProtoFluxContextualActions.Patches.ContextualSwapActionsPatch;

class NodeGroupAttribute(ConnectionTransferType connectionTransferType = default) : Attribute
{
  internal ConnectionTransferType ConnectionTransferType = connectionTransferType;
}
