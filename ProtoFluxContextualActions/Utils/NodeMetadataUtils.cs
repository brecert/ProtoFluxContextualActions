using System;
using System.Collections.Generic;
using System.Reflection;
using ProtoFlux.Core;
using ProtoFluxContextualActions.Utils;

namespace ProtoFluxContextualActions.Utils.ProtoFlux;

static class NodeMetadataUtils
{

  // todo: move to a utility class
  // lighter than GetMetadata
  public static IEnumerable<OutputMetadata> GetOutputMetadata(Type type)
  {
    var index = 0;
    {
      if (TypeUtils.MatchInterface(type, typeof(IOutput<>), out var outputType))
      {
        yield return new OutputMetadata(
          index: index++,
          ownerType: type,
          outputType: outputType.GenericTypeArguments[0],
          dataClass: outputType.GenericTypeArguments[0].IsValueType ? DataClass.Value : DataClass.Object
        );
      }
    }
    foreach (var field in type.EnumerateAllInstanceFields(BindingFlags.Instance | BindingFlags.Public))
    {
      if (TypeUtils.MatchInterface(field.FieldType, typeof(IOutput<>), out var outputType))
      {
        yield return new OutputMetadata(
          index: index++,
          field: field,
          dataClass: outputType.GenericTypeArguments[0].IsValueType ? DataClass.Value : DataClass.Object
        );
      }
    }
  }

  // todo: move to a utility class
  // lighter than GetMetadata
  public static IEnumerable<InputMetadata> GetInputMetadata(Type type)
  {
    var index = 0;
    foreach (var field in type.EnumerateAllInstanceFields(BindingFlags.Instance | BindingFlags.Public))
    {
      if (TypeUtils.MatchInterface(field.FieldType, typeof(IInput<>), out var inputType))
      {
        yield return new InputMetadata(
          index: index++,
          field: field,
          dataClass: inputType.GenericTypeArguments[0].IsValueType ? DataClass.Value : DataClass.Object,
          defaultValue: default // this isn't correct but we'll ignore it for now because we're not using it.
        );
      }
    }
  }

  // lighter than GetMetadata
  public static IEnumerable<GlobalRefMetadata> GetGlobalRefMetadata(Type type)
  {
    var index = 0;
    foreach (var field in type.EnumerateAllInstanceFields(BindingFlags.Instance | BindingFlags.Public))
    {
      if (field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(GlobalRef<>))
      {
        yield return new GlobalRefMetadata(index++, field);
      }
    }
  }
}
