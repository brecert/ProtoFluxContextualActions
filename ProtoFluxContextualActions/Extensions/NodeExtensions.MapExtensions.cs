using System.Collections.Generic;
using System.Linq;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.ProtoFlux;
using FrooxEngine.Undo;
using HarmonyLib;
using ProtoFlux.Core;

namespace ProtoFluxContextualActions.Extensions;

public static class MapExtensions
{
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

  public static void MapGlobals(this INode from, ProtoFluxNode to, bool undoable)
  {
    foreach (var source in from.AllGlobalRefSources())
    {
      var globalRef = to.GetGlobalRef(source.RefIndex);
      if (undoable) globalRef.CreateUndoPoint(forceNew: true);
      globalRef.Target = (IWorldElement)to.Group.GetGlobal(source.Target.Index);
    }
  }

  public static void MapImpulses(this INode from, ProtoFluxNode to, Dictionary<INode, ProtoFluxNode> nodeMapping, bool undoable)
  {
    UniLog.Log(from.AllImpulseSources().Join());

    foreach (var impulse in from.AllImpulseSources())
    {
      if (impulse.Target == null) continue;
      var nodeToImpulse = nodeMapping[impulse.Target.OwnerNode];
      var impulseRef = to.GetImpulse(impulse.ImpulseIndex);
      if (undoable) impulseRef.CreateUndoPoint(forceNew: true);
      impulseRef.Target = nodeToImpulse.GetOperation(impulse.Target.FindLinearOperationIndex());
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

  public static void MapInternalReferences(this INode from, ProtoFluxNode to, Dictionary<INode, ProtoFluxNode> nodeMapping, bool undoable)
  {
    foreach (var source in from.AllReferenceSources())
    {
      if (source.Target == null) continue;
      var target = nodeMapping[source.Target];
      var syncRef = to.GetReference(source.ReferenceIndex);
      if (undoable) syncRef.CreateUndoPoint(forceNew: true);
      syncRef.Target = target;
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

  public static void MapOutputs(this INode from, ProtoFluxNode to, Dictionary<INode, ProtoFluxNode> nodeMapping, NodeQueryAcceleration query, bool undoable)
  {
    foreach (var source in query.GetEvaluatingSources(from))
    {
      var sourceNode = nodeMapping[source.OwnerNode];
      sourceNode.GetInput(source.InputIndex).TrySet(to.GetOutput(source.Source.FindLinearOutputIndex()));
    }
  }
}
