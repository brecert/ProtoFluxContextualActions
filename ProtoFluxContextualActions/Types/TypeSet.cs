using System.Diagnostics;

using Elements.Core;

using FrooxEngine;

using ProtoFluxContextualActions.Utils;

namespace ProtoFluxContextualActions.Types;

class TypeSet : HashSet<ITypeDefinition>
{
  public IEnumerable<Type> TryMakeGeneric(params Type[] typeArguments) =>
    this.Select(t => t.TryMakeGenericType(typeArguments)).OfType<Type>();

  public IEnumerable<IType> TryCreateTypeFrom(Type type, TypeManager worldTypes) =>
    this.Select(t => t.TryCreateTypeFrom(type, worldTypes)).OfType<IType>();

  public IEnumerable<Type> Matches(Type type, TypeManager worldTypes) =>
    TryCreateTypeFrom(type, worldTypes)
      .Select(t => t.ResolvedType)
      .OfType<Type>();

  public IEnumerable<Type> ValidTypesFrom(Type type, TypeManager worldTypes)
  {
    if (TryCreateTypeFrom(type, worldTypes).FirstOrDefault() is IType matched)
    {
      return TryMakeGeneric(matched.GenericArguments);
    }
    return [];
  }

}
