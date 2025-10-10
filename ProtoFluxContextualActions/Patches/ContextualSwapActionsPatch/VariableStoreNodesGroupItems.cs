using System;
using System.Collections.Generic;
using System.Linq;
using Elements.Core;
using FrooxEngine.ProtoFlux;
using ProtoFlux.Runtimes.Execution.Nodes;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Variables;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  static readonly HashSet<Type> VariableStoreNodesGroup = [
    typeof(LocalValue<>),
    typeof(LocalObject<>),
    typeof(StoredValue<>),
    typeof(StoredObject<>),
    typeof(DataModelUserRefStore),
    typeof(DataModelTypeStore),
    typeof(DataModelObjectAssetRefStore<>),
    typeof(DataModelObjectAssetRefStore<>),
    typeof(DataModelValueFieldStore<>),
    typeof(DataModelObjectRefStore<>),
    typeof(DataModelObjectFieldStore<>),
  ];

  internal static IEnumerable<MenuItem> VariableStoreNodesGroupItems(ContextualContext context)
  {
    if (VariableStoreNodesGroup.Any(t => context.NodeType.IsGenericType ? t == context.NodeType.GetGenericTypeDefinition() : t == context.NodeType))
    {
      var storageType = GetIVariableValueType(context.NodeType);
      yield return new MenuItem(NodeUtils.ProtoFluxBindingMapping[ProtoFluxHelper.GetLocalNode(storageType).GetGenericTypeDefinition()].MakeGenericType(storageType));
      yield return new MenuItem(NodeUtils.ProtoFluxBindingMapping[ProtoFluxHelper.GetStoreNode(storageType).GetGenericTypeDefinition()].MakeGenericType(storageType));

      var dataModelStore = ProtoFluxHelper.GetDataModelStoreNode(storageType);
      if (dataModelStore.IsGenericType)
      {
        yield return new MenuItem(NodeUtils.ProtoFluxBindingMapping[dataModelStore.GetGenericTypeDefinition()].MakeGenericType(dataModelStore.GenericTypeArguments));
      }
      else
      {
        yield return new MenuItem(NodeUtils.ProtoFluxBindingMapping[dataModelStore]);
      }
    }
  }
}