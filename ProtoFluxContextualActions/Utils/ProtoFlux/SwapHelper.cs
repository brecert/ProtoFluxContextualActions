using System.Collections.Generic;
using Elements.Core;
using FrooxEngine.ProtoFlux.Runtimes.Execution.Nodes;
using ProtoFlux.Core;
using ProtoFluxContextualActions.Extensions;

public static class SwapHelper
{
  internal static void TransferGlobals(INode from, INode to, bool tryByIndex = false)
  {
    foreach (var fromGlobalRefSource in from.AllGlobalRefSources())
    {
      var globalByName = to.GetGlobalByName(fromGlobalRefSource.Name);
      if (globalByName.HasValue)
      {
        to.TrySetGlobalRefBinding(globalByName.Value.RefIndex, fromGlobalRefSource.Target);
      }
    }

    if (tryByIndex)
    {
      foreach (var globalRefSource in from.AllGlobalRefSources())
      {
        to.TrySetGlobalRefBinding(globalRefSource.RefIndex, globalRefSource.Target);
      }
    }
  }

  internal static IEnumerable<ConnectionResult> TransferExternalReferences<N>(INode from, INode to, NodeQueryAcceleration query, NodeRuntime<N> runtime, bool overload = true) where N : class, INode
  {
    foreach (var source in query.GetReferencingSources(from))
    {
      yield return runtime.SetReference(source.OwnerNode, source.ReferenceIndex, to, overload, allowMergingGroups: true);
    }

    // foreach (var referencingNode in query.GetReferencingNodes(from))
    // {
    //   for (int i = 0; i < referencingNode.FixedReferenceCount; i++)
    //   {
    //     var reference = referencingNode.GetReferenceTarget(i);
    //     if (reference == from)
    //     {
    //       // referencingNode.SetReferenceTarget(i, to);
    //     }
    //   }
    // }
  }

  internal static void TransferImpulses(INode from, INode to, bool tryByIndex = false)
  {
    foreach (var source in from.AllImpulseSources())
    {
      var toImpulse = to.GetImpulseByName(source.Name);
      if (toImpulse.HasValue)
      {
        var impulse = toImpulse.Value;
        impulse.Target = source.Target;
      }
    }

    // if (tryByIndex)
    // {
    //   foreach (var source in from.AllImpulses())
    //   {
    //     var toImpulse = to.GetImpulseByIndex(source.ImpulseIndex);
    //     if (toImpulse.Target != null)
    //     {
    //       toImpulse.Target = source.Target;
    //     }
    //   }
    // }
  }

  /// <summary>
  /// Transfers the impulse sources from one node to another.
  /// </summary>
  /// <param name="from">The node to transfer operations from</param>
  /// <param name="to">The node to transfer the operations to</param>
  /// <param name="query"></param>
  /// <param name="tryByIndex">if transfers should attempt to match by index instead of by name, this parameter is not stable</param>
  internal static void TransferOperations(INode from, INode to, NodeQueryAcceleration query, bool tryByIndex = false)
  {
    var impulsingFromSources = query.GetImpulsingSources(from);

    foreach (var source in impulsingFromSources)
    {
      var name = from.GetOperationName(source.Target.FindLinearOperationIndex());
      source.Target = to.GetOperationByName(name);
    }

    if (tryByIndex)
    {
      foreach (var source in impulsingFromSources)
      {
        source.Target = to.GetOperation(source.Target.FindLinearOperationIndex());
      }
    }
  }

  internal static void TransferOutputs(INode from, INode to, NodeQueryAcceleration query, bool tryByIndex = false)
  {
    // resize dynamic inputs to fit before transferring the outputs
    foreach (var fromOutputListMeta in from.Metadata.DynamicOutputs)
    {
      if (to.Metadata.GetOutputListByName(fromOutputListMeta.Name) is OutputListMetadata toOutputListMeta && fromOutputListMeta.TypeConstraint == toOutputListMeta.TypeConstraint)
      {
        var toOutputList = to.GetOutputList(toOutputListMeta.Index);
        var fromOutputList = from.GetOutputList(fromOutputListMeta.Index);

        if (toOutputList.Count < fromOutputList.Count)
        {
          for (int i = 0; i < fromOutputList.Count - toOutputList.Count; i++)
          {
            toOutputList.AddOutput();
          }
        }
      }
    }

    if (tryByIndex || true)
    {
      foreach (var node in query.GetEvaluatingNodes(from))
      {
        for (int i = 0; i < node.InputCount; i++)
        {
          var fromSource = node.GetInputSource(i);
          if (fromSource?.OwnerNode == from)
          {
            var toSourceIndex = fromSource.FindLinearOutputIndex();
            node.SetInputSource(i, to.GetOutput(toSourceIndex));
          }
        }
      }
    }
  }


  internal static void TransferInputs(INode from, INode to, bool tryByIndex = false)
  {
    // resize dynamic inputs to fit before transferring the inputs
    foreach (var fromInputListMeta in from.Metadata.DynamicInputs)
    {
      if (to.Metadata.GetInputListByName(fromInputListMeta.Name) is InputListMetadata toInputListMeta && fromInputListMeta.TypeConstraint == toInputListMeta.TypeConstraint)
      {
        var toInputList = to.GetInputList(toInputListMeta.Index);
        var fromInputList = from.GetInputList(fromInputListMeta.Index);

        if (toInputList.Count < fromInputList.Count)
        {
          for (int i = 0; i < fromInputList.Count - toInputList.Count; i++)
          {
            toInputList.AddInput(null);
          }
        }
      }
    }

    if (tryByIndex)
    {
      for (int i = 0; i < MathX.Min(from.InputCount, to.InputCount); i++)
      {
        if (from.GetInputType(i) == to.GetInputType(i) && from.GetInputSource(i) is IOutput output)
        {
          to.SetInputSource(i, output);
        }
      }
    }

    foreach (var fromInputMeta in from.Metadata.FixedInputs)
    {
      if (to.Metadata.GetInputByName(fromInputMeta.Name) is InputMetadata toInputMeta)
      {
        if (fromInputMeta.InputType != toInputMeta.InputType) continue;
        if (from.GetInputSource(fromInputMeta.Index) is IOutput output)
        {
          to.SetInputSource(new ElementRef(toInputMeta.Index), output);
        }
      }
    }

    foreach (var fromInputListMeta in from.Metadata.DynamicInputs)
    {
      if (to.Metadata.GetInputListByName(fromInputListMeta.Name) is InputListMetadata toInputListMeta)
      {
        if (fromInputListMeta.TypeConstraint != toInputListMeta.TypeConstraint) continue;

        var toInputList = to.GetInputList(toInputListMeta.Index);
        var fromInputList = from.GetInputList(fromInputListMeta.Index);
        for (int i = 0; i < fromInputList.Count; i++)
        {
          if (fromInputList.GetInputSource(i) is IOutput output)
          {
            toInputList.SetInputSource(i, output);
          }
        }
        fromInputList.Clear();
      }
    }

    // This can be made into a lookup or something nicer later if it comes up again, this is fine for now.
    var typeTuple = (from.GetType(), to.GetType());
    if (typeTuple == (typeof(For), typeof(RangeLoopInt)))
    {
      var countIndex = from.Metadata.GetInputByName("Count").Index;
      var endIndex = to.Metadata.GetInputByName("End").Index;
      if (from.GetInputSource(countIndex) is IOutput output)
      {
        to.SetInputSource(endIndex, output);
      }
    }
    if (typeTuple == (typeof(RangeLoopInt), typeof(For)))
    {
      var endIndex = from.Metadata.GetInputByName("End").Index;
      var countIndex = to.Metadata.GetInputByName("Count").Index;
      if (from.GetInputSource(endIndex) is IOutput output)
      {
        to.SetInputSource(countIndex, output);
      }
    }
  }

  public static IEnumerable<ConnectionResult> TransferElements<N>(INode oldNode, INode newNode, NodeQueryAcceleration query, NodeRuntime<N> runtime, bool tryByIndex = false, bool overload = true) where N : class, INode
  {
    newNode.CopyDynamicInputLayout(oldNode);
    newNode.CopyDynamicOutputLayout(oldNode);

    if (tryByIndex)
    {
      foreach (var list in newNode.InputLists()) list.EnsureSize(2);
      foreach (var list in newNode.OutputLists()) list.EnsureSize(2);
      foreach (var list in newNode.ImpulseLists()) list.EnsureSize(2);
      foreach (var list in newNode.OperationLists()) list.EnsureSize(2);
    }

    // while SwapNodes should handle things for us, it does not handle everything so we use our own as well;
    TransferInputs(oldNode, newNode, tryByIndex);

    // by now oldNode has lost the group while newNode has inherited it
    TransferOutputs(oldNode, newNode, query, tryByIndex);

    // meow
    TransferOperations(oldNode, newNode, query, tryByIndex);

    // meow
    TransferImpulses(oldNode, newNode, tryByIndex);

    // meow
    var results = TransferExternalReferences(oldNode, newNode, query, runtime, overload);

    // meow
    TransferGlobals(oldNode, newNode, tryByIndex);

    return results;
  }
}