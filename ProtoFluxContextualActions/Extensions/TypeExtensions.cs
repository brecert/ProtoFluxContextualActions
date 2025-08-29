using System;
using System.Collections.Generic;

namespace ProtoFluxContextualActions.Extensions;

public static class TypeExtensions
{
  // todo: better name
  public static Type GetGenericTypeDefinitionOrSameType(this Type type) => type.IsGenericType ? type.GetGenericTypeDefinition() : type;
}
