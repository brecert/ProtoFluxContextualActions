using System;
using System.Collections.Generic;
using System.Linq;
using FrooxEngine.ProtoFlux;
using ProtoFlux.Core;
using ProtoFluxContextualActions.Utils.ProtoFlux;
using ImpulseSource = ProtoFluxContextualActions.Utils.ProtoFlux.ImpulseSource;

namespace ProtoFluxContextualActions.Extensions;

internal static partial class NodeExtensions
{
  public static IEnumerable<(INode instance, ProtoFluxNode node)> NodeInstances(this ProtoFluxNodeGroup group) =>
    group.Nodes.Select(node => (node.NodeInstance, node));

  public static IEnumerable<InputSource> AllInputSources(this INode node)
  {
    for (int i = 0; i < node.FixedInputCount; i++)
    {
      yield return new(node, index: i);
    }

    for (int i = 0; i < node.DynamicInputCount; i++)
    {
      var list = node.GetInputList(i);
      for (int j = 0; j < list.Count; j++)
      {
        yield return new(node, index: j, listIndex: i);
      }
    }
  }

  public static IEnumerable<ImpulseSource> AllImpulseSources(this INode node)
  {
    for (int i = 0; i < node.FixedImpulseCount; i++)
    {
      yield return new(node, i);
    }

    for (int i = 0; i < node.DynamicImpulseCount; i++)
    {
      var list = node.GetImpulseList(i);
      for (int j = 0; j < list.Count; j++)
      {
        yield return new(node, j, i);
      }
    }
  }

  public static IEnumerable<ReferenceSource> AllReferenceSources(this INode node)
  {
    for (int i = 0; i < node.FixedReferenceCount; i++)
    {
      yield return new(node, i);
    }
  }

  public static IEnumerable<GlobalRefSource> AllGlobalRefSources(this INode node)
  {
    for (int i = 0; i < node.FixedGlobalRefCount; i++)
    {
      yield return new GlobalRefSource(node, i);
    }
  }

  public static void EnsureSize(this IInputList list, int size)
  {
    for (int i = list.Count; i < size; i++)
    {
      list.AddInput(null);
    }
  }

  public static void EnsureSize(this IOutputList list, int size)
  {
    for (int i = list.Count; i < size; i++)
    {
      list.AddOutput();
    }
  }

  public static void CopyDynamicInputLayout(this INode node, INode from)
  {
    for (int i = 0; i < Math.Min(from.DynamicInputCount, node.DynamicInputCount); i++)
    {
      node.GetInputList(i).EnsureSize(from.GetInputList(i).Count);
    }
  }

  public static void CopyDynamicOutputLayout(this INode node, INode from)
  {
    for (int i = 0; i < Math.Min(from.DynamicOutputCount, node.DynamicOutputCount); i++)
    {
      node.GetOutputList(i).EnsureSize(from.GetOutputList(i).Count);
    }
  }

  public static IOperation? GetOperationByName(this INode node, string name)
  {
    var meta = node.Metadata.GetOperationByName(name);
    if (meta != null)
    {
      return node.GetOperation(meta.Index);
    }
    return null;
  }

  public static ImpulseSource GetImpulseByIndex(this INode node, int index) =>
    new(node, index);

  public static ImpulseSource? GetImpulseByName(this INode node, string name)
  {
    var found = node.Metadata.GetImpulseByName(name);
    if (found != null)
    {
      return new(node, found.Index);
    }
    return null;
  }

  public static GlobalRefSource? GetGlobalByName(this INode node, string name)
  {
    var found = node.Metadata.FixedGlobalRefs.Where(g => g.Name == name).FirstOrDefault();
    if (found != null)
    {
      return new(node, found.Index);
    }
    return null;
  }
}
