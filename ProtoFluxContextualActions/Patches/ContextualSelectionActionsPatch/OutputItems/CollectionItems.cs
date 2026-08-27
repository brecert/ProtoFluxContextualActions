using System;
using System.Collections.Generic;
using ProtoFlux.Core;
using ProtoFlux.Runtimes.Execution.Nodes;
using ProtoFlux.Runtimes.Execution.Nodes.Collections;
using ProtoFluxContextualActions.Utils;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSelectionActionsPatch
{
  private static IEnumerable<MenuItem> CollectionItems(Type outputType)
  {
    if (TypeUtils.MatchInterface(outputType, typeof(IEnumerable<>), out var enumerableType))
    {
      var elementType = enumerableType.GenericTypeArguments[0];

      // todo: swap with index
      var forEachItem = elementType.IsUnmanaged() switch
      {
        true => typeof(ForEachValue<,>),
        false => typeof(ForEachObject<,>),
      };
      yield return new(forEachItem.MakeGenericType(outputType, elementType));
    }

    foreach (var dictionaryItem in DictionaryItems(outputType))
    {
      yield return dictionaryItem;
    }


    // todo: move to unpacking?
    if (TypeUtils.MatchInterface(outputType, typeof(KeyValuePair<,>), out var keyValuePairType))
    {
      var keyType = keyValuePairType.GenericTypeArguments[0];
      var valueType = keyValuePairType.GenericTypeArguments[1];

      var addItemWithKey = (keyType.IsUnmanaged(), valueType.IsUnmanaged()) switch
      {
        (true, true) => typeof(UnpackValueKeyValueValuePair<,>),
        (true, false) => typeof(UnpackValueKeyObjectValuePair<,>),
        (false, true) => typeof(UnpackObjectKeyValueValuePair<,>),
        (false, false) => typeof(UnpackObjectKeyObjectValuePair<,>)
      };
      yield return new(addItemWithKey.MakeGenericType(keyType, valueType));
    }

    {
      if (TypeUtils.MatchInterface(outputType, typeof(IReadOnlyCollection<>), out var readOnlyListType))
      {
        var valueType = readOnlyListType.GenericTypeArguments[0];

        yield return new(typeof(ReadOnlyCount<,>).MakeGenericType(outputType, valueType));

        var containsItem = valueType.IsUnmanaged() switch
        {
          true => typeof(ReadOnlyContainsValue<,>),
          false => typeof(ReadOnlyContainsObject<,>),
        };
        yield return new(containsItem.MakeGenericType(outputType, valueType));
      }
      else if (TypeUtils.MatchInterface(outputType, typeof(ICollection<>), out var collectionType))
      {
        var valueType = collectionType.GenericTypeArguments[0];

        yield return new(typeof(Count<,>).MakeGenericType(outputType, valueType));
        yield return new(typeof(IsReadOnly<,>).MakeGenericType(outputType, valueType));

        var containsItem = valueType.IsUnmanaged() switch
        {
          true => typeof(ContainsValue<,>),
          false => typeof(ContainsObject<,>),
        };
        yield return new(containsItem.MakeGenericType(outputType, valueType));
      }
      else if (TypeUtils.MatchInterface(outputType, typeof(System.Collections.ICollection), out _))
      {
        yield return new(typeof(Count<>).MakeGenericType(outputType));
      }
    }

    if (TypeUtils.MatchInterface(outputType, typeof(IReadOnlyList<>), out var readonlyListType))
    {
      var valueType = readonlyListType.GenericTypeArguments[1];

      var getAtItem = valueType.IsUnmanaged() switch
      {
        true => typeof(ReadOnlyGetAtValue<,>),
        false => typeof(ReadOnlyGetAtObject<,>),
      };
      yield return new(getAtItem.MakeGenericType(outputType, valueType));
    }


    if (TypeUtils.MatchInterface(outputType, typeof(IList<>), out var listType))
    {
      var valueType = listType.GenericTypeArguments[0];

      yield return new(typeof(Clear<,>).MakeGenericType(outputType, valueType));
      yield return new(typeof(RemoveAt<,>).MakeGenericType(outputType, valueType));

      if (readonlyListType is null)
      {
        var getItem = valueType.IsUnmanaged() switch
        {
          true => typeof(GetAtValue<,>),
          false => typeof(GetAtObject<,>),
        };
        yield return new(getItem.MakeGenericType(outputType, valueType));
      }

      var addItem = valueType.IsUnmanaged() switch
      {
        true => typeof(AddValue<,>),
        false => typeof(AddObject<,>),
      };
      yield return new(addItem.MakeGenericType(outputType, valueType));

      // todo: swap with add?
      var insertAtItem = valueType.IsUnmanaged() switch
      {
        true => typeof(InsertAtValue<,>),
        false => typeof(InsertAtObject<,>),
      };
      yield return new(insertAtItem.MakeGenericType(outputType, valueType));

      var indexOfItem = valueType.IsUnmanaged() switch
      {
        true => typeof(IndexOfValue<,>),
        false => typeof(IndexOfObject<,>),
      };
      yield return new(indexOfItem.MakeGenericType(outputType, valueType));

      var removeItem = valueType.IsUnmanaged() switch
      {
        true => typeof(RemoveValue<,>),
        false => typeof(RemoveObject<,>),
      };
      yield return new(removeItem.MakeGenericType(outputType, valueType));

      var setAtItem = valueType.IsUnmanaged() switch
      {
        true => typeof(SetAtValue<,>),
        false => typeof(SetAtObject<,>),
      };
      yield return new(setAtItem.MakeGenericType(outputType, valueType));
    }
    else if (TypeUtils.MatchInterface(outputType, typeof(System.Collections.IList), out _))
    {
      yield return new(typeof(Clear<>).MakeGenericType(outputType));
      yield return new(typeof(RemoveAt<>).MakeGenericType(outputType));
    }
  }

  private static IEnumerable<MenuItem> DictionaryItems(Type outputType)
  {
    if (TypeUtils.MatchInterface(outputType, typeof(IDictionary<,>), out var dictionaryType))
    {
      var keyType = dictionaryType.GenericTypeArguments[0];
      var valueType = dictionaryType.GenericTypeArguments[1];

      // I wish I didn't need to manually check these but generic creation is *not* failing and throwing with unmanaged types despite the constraint so...
      var addItemWithKey = (valueType.IsUnmanaged(), keyType.IsUnmanaged()) switch
      {
        (true, true) => typeof(AddValueWithValueKey<,,>),
        (true, false) => typeof(AddValueWithObjectKey<,,>),
        (false, true) => typeof(AddObjectWithValueKey<,,>),
        (false, false) => typeof(AddObjectWithObjectKey<,,>)
      };
      yield return new(addItemWithKey.MakeGenericType(outputType, keyType, valueType));

      var getItemWithKey = (valueType.IsUnmanaged(), keyType.IsUnmanaged()) switch
      {
        (true, true) => typeof(GetValueWithValueKey<,,>),
        (true, false) => typeof(GetValueWithObjectKey<,,>),
        (false, true) => typeof(GetObjectWithValueKey<,,>),
        (false, false) => typeof(GetObjectWithObjectKey<,,>)
      };
      yield return new(getItemWithKey.MakeGenericType(outputType, keyType, valueType));

      var setItemWithKey = (valueType.IsUnmanaged(), keyType.IsUnmanaged()) switch
      {
        (true, true) => typeof(SetValueWithValueKey<,,>),
        (true, false) => typeof(SetValueWithObjectKey<,,>),
        (false, true) => typeof(SetObjectWithValueKey<,,>),
        (false, false) => typeof(SetObjectWithObjectKey<,,>)
      };
      yield return new(setItemWithKey.MakeGenericType(outputType, keyType, valueType));


      var removeKey = keyType.IsUnmanaged() switch
      {
        true => typeof(RemoveValueKey<,,>),
        false => typeof(RemoveObjectKey<,,>),
      };
      yield return new(removeKey.MakeGenericType(outputType, keyType, valueType));

      var containsKey = keyType.IsUnmanaged() switch
      {
        true => typeof(ContainsValueKey<,,>),
        false => typeof(ContainsObjectKey<,,>),
      };
      yield return new(containsKey.MakeGenericType(outputType, keyType, valueType));
    }
  }
}
