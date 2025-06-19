using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.ProtoFlux;
using FrooxEngine.ProtoFlux.Runtimes.Execution.Nodes.Operators;
using FrooxEngine.ProtoFlux.Runtimes.Execution.Nodes.Strings;
using FrooxEngine.Undo;
using HarmonyLib;
using ProtoFlux.Core;

namespace ProtoFluxContextualActions.Extensions;

internal static class NodeExtensions
{
  public readonly struct InputSource(INode owner, int index)
  {
    public readonly INode OwnerNode = owner;

    public readonly int InputIndex = index;

    public readonly IOutput? Source
    {
      get => OwnerNode.GetInputSource(InputIndex);
      set => OwnerNode.SetInputSource(InputIndex, value);
    }

    public readonly string Name => OwnerNode.GetInputName(InputIndex);

    public readonly Type ValueType => OwnerNode.GetInputType(InputIndex);
  }



  public readonly struct GlobalRefSource(INode owner, int index)
  {
    public readonly INode OwnerNode = owner;

    public readonly int RefIndex = index;

    public readonly Global? Target
    {
      get => OwnerNode.GetGlobalRefBinding(RefIndex);
      set => OwnerNode.SetGlobalRefBinding(RefIndex, value);
    }

    public readonly string Name => OwnerNode.GetGlobalRefName(RefIndex);

    public readonly Type ValueType => OwnerNode.GetGlobalRefValueType(RefIndex);
  }

  public readonly struct ImpulseSource
  {
    public ImpulseSource(INode node, int index)
    {
      OwnerNode = node;
      ImpulseIndex = index;
    }

    public ImpulseSource(INode node, IImpulse impulse)
    {
      OwnerNode = node;
      ImpulseIndex = node.Metadata.FixedImpulses.Find(m => m.Field.GetValue(node).GetHashCode() == impulse.GetHashCode()).Index;
    }

    public ImpulseSource(INode node, string name)
    {
      OwnerNode = node;
      ImpulseIndex = node.Metadata.GetImpulseByName(name).Index;
    }

    public readonly INode OwnerNode;
    public readonly int ImpulseIndex;

    public readonly IOperation? Target
    {
      get => OwnerNode.GetImpulseTarget(ImpulseIndex);
      set => OwnerNode.SetImpulseTarget(ImpulseIndex, value);
    }

    public readonly string Name => OwnerNode.GetImpulseName(ImpulseIndex);

    public readonly ImpulseType TargetType => OwnerNode.GetImpulseType(ImpulseIndex);

    public override string ToString()
    {
      return $"ImpulseSource.{TargetType} [{ImpulseIndex}] '{Name}' -> {Target}";
    }
  }

  public readonly struct ReferenceSource(INode node, int index)
  {
    public readonly INode OwnerNode = node;
    public readonly int ReferenceIndex = index;

    public readonly INode? Target
    {
      get => OwnerNode.GetReferenceTarget(ReferenceIndex);
      set => OwnerNode.SetReferenceTarget(ReferenceIndex, value);
    }

    public readonly string Name => OwnerNode.GetReferenceName(ReferenceIndex);

    public readonly Type TargetType => OwnerNode.GetReferenceType(ReferenceIndex);

    public override string ToString()
    {
      return $"ReferenceSource.{TargetType} [{ReferenceIndex}] '{Name}' -> {Target}";
    }
  }

  public static IEnumerable<(INode instance, ProtoFluxNode node)> NodeInstances(this ProtoFluxNodeGroup group) =>
    group.Nodes.Select(node => (node.NodeInstance, node));

  public static IEnumerable<InputSource> AllInputSources(this INode node)
  {
    for (int i = 0; i < node.InputCount; i++)
    {
      yield return new(node, i);
    }
  }

  public static IEnumerable<ImpulseSource> AllImpulses(this INode node)
  {
    for (int i = 0; i < node.ImpulseCount; i++)
    {
      yield return new ImpulseSource(node, i);
    }
  }

  public static IEnumerable<INode> AllReferenceTargets(this INode node)
  {
    for (int i = 0; i < node.FixedReferenceCount; i++)
    {
      yield return node.GetReferenceTarget(i);
    }
  }

  public static IEnumerable<ReferenceSource> AllReferenceSources(this INode node)
  {
    for (int i = 0; i < node.FixedReferenceCount; i++)
    {
      yield return new(node, i);
    }
  }

  public static IEnumerable<GlobalRefSource> AllGlobalRefs(this INode node)
  {
    for (int i = 0; i < node.FixedGlobalRefCount; i++)
    {
      yield return new GlobalRefSource(node, i);
    }
  }


  public static void MapInputs(this INode from, ProtoFluxNode to, Dictionary<INode, ProtoFluxNode> nodeMapping, bool undoable)
  {
    foreach (var inputSource in from.AllInputSources().Where(s => s.Source != null))
    {
      var index = inputSource.Source.FindLinearOutputIndex();
      var inputNode = nodeMapping[inputSource.Source.OwnerNode];
      var input = to.GetInput(index);
      if (undoable) input.CreateUndoPoint(forceNew: true);
      input.Target = inputNode.GetOutput(index);
    }
  }

  public static void MapOutputs(this INode from, ProtoFluxNode to, Dictionary<INode, ProtoFluxNode> nodeMapping, NodeQueryAcceleration query, bool undoable)
  {
    foreach (var source in query.GetEvaluatingSources(from))
    {
      var sourceNode = nodeMapping[source.OwnerNode];
      sourceNode.GetInput(source.InputIndex).TrySet(to.GetOutput(source.Source.FindLinearOutputIndex()));
    }
  }

  public static void MapOperations(this INode from, ProtoFluxNode to, Dictionary<INode, ProtoFluxNode> nodeMapping, NodeQueryAcceleration query, bool undoable)
  {
    foreach (var source in query.GetImpulsingSources(from))
    {
      var sourceNode = nodeMapping[source.OwnerNode];
      sourceNode.GetImpulse(source.ImpulseIndex).TrySet(to.GetOperation(source.Target.FindLinearOperationIndex()));
    }
  }

  public static void MapExternalReferences(this INode from, ProtoFluxNode to, Dictionary<INode, ProtoFluxNode> nodeMapping, NodeQueryAcceleration query, bool undoable)
  {
    foreach (var source in query.GetReferencingSources(from))
    {
      var referencingNode = nodeMapping[source.OwnerNode];
      var syncRef = referencingNode.GetReference(source.ReferenceIndex);
      if (undoable) syncRef.CreateUndoPoint(forceNew: true);
      syncRef.Target = to;
    }
  }

  public static void MapImpulses(this INode from, ProtoFluxNode to, Dictionary<INode, ProtoFluxNode> nodeMapping, bool undoable)
  {
    UniLog.Log(from.AllImpulses().Join());

    foreach (var impulse in from.AllImpulses())
    {
      if (impulse.Target == null) continue;
      var nodeToImpulse = nodeMapping[impulse.Target.OwnerNode];
      var impulseRef = to.GetImpulse(impulse.ImpulseIndex);
      if (undoable) impulseRef.CreateUndoPoint(forceNew: true);
      impulseRef.Target = nodeToImpulse.GetOperation(impulse.Target.FindLinearOperationIndex());
    }
  }

  public static void MapInternalReferences(this INode from, ProtoFluxNode to, Dictionary<INode, ProtoFluxNode> nodeMapping, bool undoable)
  {
    foreach (var (referenceTarget, i) in from.AllReferenceTargets().WithIndex().Where(s => s.value != null))
    {
      var target = nodeMapping[referenceTarget];
      var syncRef = to.GetReference(i);
      if (undoable) syncRef.CreateUndoPoint(forceNew: true);
      syncRef.Target = syncRef;
    }
  }

  public static void MapGlobals(this INode from, ProtoFluxNode to, bool undoable)
  {
    foreach (var source in from.AllGlobalRefs())
    {
      var globalRef = to.GetGlobalRef(source.RefIndex);
      if (undoable) globalRef.CreateUndoPoint(forceNew: true);
      globalRef.Target = (IWorldElement)to.Group.GetGlobal(source.Target.Index);
    }
  }

  public static void MapElements(this INode from, ProtoFluxNode to, Dictionary<INode, ProtoFluxNode> nodeMapping, bool undoable)
  {
    var query = new NodeQueryAcceleration(from.Runtime.Group);

    from.MapInputs(to, nodeMapping, undoable);
    from.MapImpulses(to, nodeMapping, undoable);
    from.MapInternalReferences(to, nodeMapping, undoable);
    from.MapGlobals(to, undoable);

    from.MapOutputs(to, nodeMapping, query, undoable);
    from.MapOperations(to, nodeMapping, query, undoable);
    from.MapExternalReferences(to, nodeMapping, query, undoable);
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
    for (int i = 0; i < from.DynamicInputCount; i++)
    {
      node.GetInputList(i).EnsureSize(from.GetInputList(i).Count);
    }
  }

  public static void CopyDynamicOutputLayout(this INode node, INode from)
  {
    for (int i = 0; i < from.DynamicOutputCount; i++)
    {
      node.GetOutputList(i).EnsureSize(from.GetOutputList(i).Count);
    }
  }

  public static IEnumerable<ImpulseSource> GetImpulsingSources(this NodeQueryAcceleration query, INode node) =>
    query.GetImpulsingNodes(node).SelectMany(n => n.AllImpulses().Where(i => i.Target?.OwnerNode == node));

  public static IEnumerable<InputSource> GetEvaluatingSources(this NodeQueryAcceleration query, INode node) =>
    query.GetEvaluatingNodes(node).SelectMany(n => n.AllInputSources().Where(i => i.Source?.OwnerNode == node));

  public static IEnumerable<ReferenceSource> GetReferencingSources(this NodeQueryAcceleration query, INode node) =>
    query.GetReferencingNodes(node).SelectMany(n => n.AllReferenceSources().Where(r => r.Target == node));

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
