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

  public Type? TryMakeGenericType(params Type[]? typeArguments) =>
    typeArguments != null ? Definition.TryMakeGenericType(typeArguments) : Definition;

  public IType? TryCreateTypeFrom(Type type, TypeManager worldTypes) =>
    TryCreateTypeFrom(type);

  public static implicit operator SystemTypeDefinition(Type type) => new(type);
}

record SystemType(Type OriginalType, Type Definition, Type Matched) : IType
{
  public Type[]? GenericArguments => Matched.IsGenericType ? Matched.GenericTypeArguments : null;
  public Type? ResolvedType => Matched;
}
