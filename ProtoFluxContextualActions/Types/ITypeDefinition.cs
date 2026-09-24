using FrooxEngine;

namespace ProtoFluxContextualActions.Types;

interface ITypeDefinition
{
  public IType? TryCreateTypeFrom(Type type, TypeManager worldTypes);
  public Type? TryMakeGenericType(params Type[] typeArguments);
}
