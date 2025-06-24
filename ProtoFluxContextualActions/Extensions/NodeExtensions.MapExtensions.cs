using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.ProtoFlux;
using FrooxEngine.Undo;
using HarmonyLib;
using ProtoFlux.Core;
using ProtoFluxContextualActions.Utils.ProtoFlux;

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
    foreach (var source in query.GetReferencingElements(from))
    {
      var referencingNode = nodeMapping[source.OwnerNode];
      var syncRef = referencingNode.GetReference(source.ReferenceIndex);
      if (undoable) syncRef.CreateUndoPoint(forceNew: true);
      syncRef.Target = to;
    }
  }

  public static void MapGlobals(this INode from, ProtoFluxNode to, bool undoable)
  {
    foreach (var source in from.AllGlobalRefElements())
    {
      var globalRef = to.GetGlobalRef(source.RefIndex);
      if (undoable) globalRef.CreateUndoPoint(forceNew: true);
      globalRef.Target = (IWorldElement)to.Group.GetGlobal(source.Target.Index);
    }
  }

  public static void MapImpulses(this INode from, ProtoFluxNode to, Dictionary<INode, ProtoFluxNode> nodeMapping, bool undoable)
  {
    foreach (var impulse in from.AllImpulseElements())
    {
      if (impulse.Target == null) continue;
      var nodeToImpulse = nodeMapping[impulse.Target.OwnerNode];
      var syncRef = to.GetImpulse(impulse);
      if (undoable) syncRef?.CreateUndoPoint(forceNew: true);
      syncRef?.TrySet(nodeToImpulse.GetOperation(impulse.TargetElement().Value));
    }
  }

  public static void MapInputs(this INode from, ProtoFluxNode to, Dictionary<INode, ProtoFluxNode> nodeMapping, bool undoable)
  {
    foreach (var source in from.AllInputElements())
    {
      UniLog.Log(source);
      if (source.Source == null) continue;
      var inputFrom = nodeMapping[source.Source.OwnerNode];
      var syncRef = to.GetInput(source);
      if (undoable) syncRef?.CreateUndoPoint(forceNew: true);
      syncRef?.TrySet(inputFrom.GetOutput(source.SourceElement().Value));
    }
  }

  public static void MapInternalReferences(this INode from, ProtoFluxNode to, Dictionary<INode, ProtoFluxNode> nodeMapping, bool undoable)
  {
    foreach (var source in from.AllReferenceElements())
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
    foreach (var source in query.GetImpulsingElements(from))
    {
      var sourceNode = nodeMapping[source.OwnerNode];
      var syncRef = sourceNode.GetImpulse(source);
      if (undoable) syncRef?.CreateUndoPoint(forceNew: true);
      syncRef?.TrySet(to.GetOperation(source.TargetElement().Value));
    }
  }

  public static void MapOutputs(this INode from, ProtoFluxNode to, Dictionary<INode, ProtoFluxNode> nodeMapping, NodeQueryAcceleration query, bool undoable)
  {
    foreach (var source in query.GetEvaluatingElements(from))
    {
      var sourceNode = nodeMapping[source.OwnerNode];
      var syncRef = sourceNode.GetInput(source);
      if (undoable) syncRef?.CreateUndoPoint(forceNew: true);
      syncRef?.TrySet(to.GetOutput(source.SourceElement().Value));
    }
  }
}
