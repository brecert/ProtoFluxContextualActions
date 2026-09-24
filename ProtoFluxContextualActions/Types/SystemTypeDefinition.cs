using Elements.Core;

using FrooxEngine;

using ProtoFluxContextualActions.Utils;

namespace ProtoFluxContextualActions.Types;

class SystemTypeDefinition(Type Definition) : ITypeDefinition
{
  public SystemType? TryCreateTypeFrom(Type type)
  {
    if (TypeUtils.MatchInterface(type, Definition, out var match))
    {
      return new(type, Definition, match);
    }
    return null;
  }

  Type? ITypeDefinition.TryMakeGenericType(params Type[] typeArguments) =>
    Definition.TryMakeGenericType(typeArguments);

  IType? ITypeDefinition.TryCreateTypeFrom(Type type, TypeManager worldTypes) =>
    TryCreateTypeFrom(type);

  public static implicit operator SystemTypeDefinition(Type type) => new(type);
}

record SystemType(Type OriginalType, Type Definition, Type Matched) : IType
{
  public Type[] GenericArguments => Matched.GenericTypeArguments;
  public Type? ResolvedType => Matched;
}
