using Elements.Core;

namespace ProtoFluxContextualActions.Types;

class TypeSet : HashSet<ITypeDefinition>
{
  public IEnumerable<Type> TryMakeGeneric(params Type[] typeArguments) =>
    this.Select(t => t.TryMakeGenericType(typeArguments)).OfType<Type>();
}
