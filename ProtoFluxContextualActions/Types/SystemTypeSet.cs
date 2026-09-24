using FrooxEngine;

using ProtoFluxContextualActions.Utils;

namespace ProtoFluxContextualActions.Types;

class SystemTypeSet : HashSet<Type>, ITypeSet
{
  public IEnumerable<Type> GetMatchingTypes(Type type, TypeManager worldTypes)
  {
    foreach (var t in this)
    {
      if (TypeUtils.MatchInterface(type, t, out var matchedType))
      {
        foreach (var ty in this)
        {
          if (matchedType.IsGenericType)
          {
            if (ty.TryMakingGenericTypeFrom(matchedType) is Type filledType)
            {
              yield return filledType;
            }
          }
          else
          {
            yield return ty;
          }
        }
        break;
      }
    }
  }

  public TypeMap MapTo(SystemTypeSet other, bool bidirectional) => new(
    bidirectional: bidirectional,
    collection: [
      .. this
        .Zip(other)
        .Select(a => (new SystemTypeDefinition(a.First) as ITypeDefinition, new SystemTypeDefinition(a.Second) as ITypeDefinition))
    ]
  );

}
