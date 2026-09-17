using FrooxEngine;
using FrooxEngine.ProtoFlux;
using FrooxEngine.Undo;

using ProtoFlux.Core;

namespace ProtoFluxContextualActions.Extensions;

public static class MapExtensions
{
  /// <summary>
  /// Maps a ProtoFlux runtime node's values to a FrooxEngine ProtoFluxNode component's values
  /// </summary>
  public static void MapElements(this INode fromNode, ProtoFluxNode toNode, Dictionary<INode, ProtoFluxNode> nodeMapping, bool undoable)
  {
    var query = new NodeQueryAcceleration(fromNode.Runtime.Group);

    fromNode.MapInputs(toNode, nodeMapping, undoable);
    fromNode.MapImpulses(toNode, nodeMapping, undoable);
    fromNode.MapInternalReferences(toNode, nodeMapping, undoable);
    fromNode.MapGlobals(toNode, undoable);

    fromNode.MapOutputs(toNode, nodeMapping, query, undoable);
    fromNode.MapOperations(toNode, nodeMapping, query, undoable);
    fromNode.MapExternalReferences(toNode, nodeMapping, query, undoable);
  }

  public static void MapExternalReferences(this INode fromNode, ProtoFluxNode toNode, Dictionary<INode, ProtoFluxNode> nodeMapping, NodeQueryAcceleration query, bool undoable)
  {
    foreach (var source in query.GetReferencingElements(fromNode))
    {
      var referencingNode = nodeMapping[source.OwnerNode];
      var syncRef = referencingNode.GetReference(source.ElementIndex);
      if (undoable) syncRef.CreateUndoPoint(forceNew: true);
      syncRef.Target = toNode;
    }
  }

  public static void MapGlobals(this INode fromNode, ProtoFluxNode toNode, bool undoable)
  {
    foreach (var source in fromNode.AllGlobalRefElements())
    {
      var globalRef = toNode.GetGlobalRef(source.ElementIndex);
      if (undoable) globalRef.CreateUndoPoint(forceNew: true);
      if (source.Target is Global global)
      {
        globalRef.Target = (IWorldElement)toNode.Group.GetGlobal(global.Index);
      }
    }
  }

  public static void MapImpulses(this INode fromNode, ProtoFluxNode toNode, Dictionary<INode, ProtoFluxNode> nodeMapping, bool undoable)
  {
    foreach (var impulse in fromNode.AllImpulseElements())
    {
      if (impulse.Target == null) continue;
      var nodeToImpulse = nodeMapping[impulse.Target.OwnerNode];
      var syncRef = toNode.GetImpulse(impulse);
      if (undoable) syncRef?.CreateUndoPoint(forceNew: true);
      syncRef?.TrySet(nodeToImpulse.GetOperation(impulse.TargetElement().Value));
    }
  }

  public static void MapInputs(this INode fromNode, ProtoFluxNode toNode, Dictionary<INode, ProtoFluxNode> nodeMapping, bool undoable)
  {
    foreach (var source in fromNode.AllInputElements())
    {
      if (source.Source == null) continue;
      var inputFrom = nodeMapping[source.Source.OwnerNode];
      var syncRef = toNode.GetInput(source);
      if (undoable) syncRef?.CreateUndoPoint(forceNew: true);
      syncRef?.TrySet(inputFrom.GetOutput(source.SourceElement().Value));
    }
  }

  public static void MapInternalReferences(this INode fromNode, ProtoFluxNode toNode, Dictionary<INode, ProtoFluxNode> nodeMapping, bool undoable)
  {
    foreach (var source in fromNode.AllReferenceElements())
    {
      if (source.Target == null) continue;
      var target = nodeMapping[source.Target];
      var syncRef = toNode.GetReference(source.ElementIndex);
      if (undoable) syncRef.CreateUndoPoint(forceNew: true);
      syncRef.Target = target;
    }
  }

  public static void MapOperations(this INode fromNode, ProtoFluxNode toNode, Dictionary<INode, ProtoFluxNode> nodeMapping, NodeQueryAcceleration query, bool undoable)
  {
    foreach (var source in query.GetImpulsingElements(fromNode))
    {
      var sourceNode = nodeMapping[source.OwnerNode];
      var syncRef = sourceNode.GetImpulse(source);
      if (undoable) syncRef?.CreateUndoPoint(forceNew: true);
      syncRef?.TrySet(toNode.GetOperation(source.TargetElement().Value));
    }
  }

  public static void MapOutputs(this INode fromNode, ProtoFluxNode toNode, Dictionary<INode, ProtoFluxNode> nodeMapping, NodeQueryAcceleration query, bool undoable)
  {
    foreach (var source in query.GetEvaluatingElements(fromNode))
    {
      var sourceNode = nodeMapping[source.OwnerNode];
      var syncRef = sourceNode.GetInput(source);
      if (undoable) syncRef?.CreateUndoPoint(forceNew: true);
      syncRef?.TrySet(toNode.GetOutput(source.SourceElement().Value));
    }
  }
}
