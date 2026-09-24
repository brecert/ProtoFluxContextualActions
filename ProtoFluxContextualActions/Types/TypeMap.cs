using Elements.Core;

using FrooxEngine;

using ProtoFluxContextualActions.Types;

class TypeMap(bool bidirectional, IEnumerable<(ITypeDefinition A, ITypeDefinition B)> collection) : List<(ITypeDefinition A, ITypeDefinition B)>(collection), ITypeSet
{
  public bool BiDirectional = bidirectional;

  public IEnumerable<Type> GetMatchingTypes(Type type, TypeManager worldTypes)
  {
    UniLog.Log(this.Count);
    UniLog.Log(string.Join(", ", this));
    foreach (var (A, B) in this)
    {
      if (A.TryCreateTypeFrom(type, worldTypes) is { } matchedTypeA)
      {
        return B.TryMakeGenericType(matchedTypeA.GenericArguments) is { } b ? [b] : [];
      }
      if (BiDirectional)
      {
        if (B.TryCreateTypeFrom(type, worldTypes) is { } matchedTypeB)
        {
          UniLog.Log((type, B, matchedTypeB));
          return A.TryMakeGenericType(matchedTypeB.GenericArguments) is { } a ? [a] : [];
        }
      }
    }
    return [];
  }
}
