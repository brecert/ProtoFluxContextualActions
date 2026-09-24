using FrooxEngine;

using ProtoFluxContextualActions.Utils;

namespace ProtoFluxContextualActions.Types;

record SystemTypeDefinition(Type Definition) : ITypeDefinition
{
  public SystemType? TryCreateTypeFrom(Type type)
  {
    if (TypeUtils.MatchInterface(type, Definition, out _))
    {
      return new(type, Definition);
    }
    return null;
  }

  Type? ITypeDefinition.TryMakeGenericType(params Type[] typeArguments) =>
    Definition.TryMakeGenericType(typeArguments);

  IType? ITypeDefinition.TryCreateTypeFrom(Type type, TypeManager worldTypes) =>
    TryCreateTypeFrom(type);
}

record SystemType(Type OriginalType, Type Definition) : IType
{
  public Type[] Generics => OriginalType.GenericTypeArguments;
}
