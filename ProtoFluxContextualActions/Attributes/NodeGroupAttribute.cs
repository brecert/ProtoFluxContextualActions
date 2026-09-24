using ProtoFlux.Core;

using static ProtoFluxContextualActions.Patches.ContextualSwapActionsPatch;

interface IFormatter<T>
{
  public static abstract string Format(T value);
}

// idk
class MultiNodeFormatter : IFormatter<Type>
{
  public static string Format(Type type) =>
    type.Name.Contains("Multi")
      ? $"{NodeMetadataHelper.GetMetadata(type).Name} (Multi)"
      : DefaultFormatter.Format(type);
}

class DefaultFormatter : IFormatter<Type>
{
  public static string Format(Type type) =>
    NodeMetadataHelper.GetMetadata(type).Name;
}

interface INodeGroupAttribute
{
  internal ConnectionTransferType ConnectionTransferType { get; }

  public string Format(Type type);
}

class NodeGroupAttribute<T>(ConnectionTransferType connectionTransferType = default) : Attribute, INodeGroupAttribute where T : IFormatter<Type>
{
  public ConnectionTransferType ConnectionTransferType => connectionTransferType;

  public string Format(Type type) => T.Format(type);
}

class NodeGroupAttribute(ConnectionTransferType connectionTransferType = default) : NodeGroupAttribute<MultiNodeFormatter>(connectionTransferType);
