using System.Collections.Generic;
using System.Linq;
using Elements.Core;
using ProtoFlux.Runtimes.Execution.Nodes.Operators;
using ProtoFlux.Core;
using ProtoFlux.Runtimes.Execution.Nodes;
using ProtoFluxContextualActions.Extensions;
using System;
using System.Diagnostics.CodeAnalysis;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Slots;

namespace ProtoFluxContextualActions.Utils.ProtoFlux;

public static class SwapHelper
{
  internal static void TransferGlobals(INode from, INode to, bool tryByIndex = false)
  {
    // todo: type check

    foreach (var fromGlobalRefSource in from.AllGlobalRefElements())
    {
      var globalByName = to.GetGlobalByName(fromGlobalRefSource.DisplayName);
      if (globalByName.HasValue)
      {
        to.TrySetGlobalRefBinding(globalByName.Value.ElementIndex, fromGlobalRefSource.Target);
      }
    }

    if (tryByIndex)
    {
      foreach (var globalRefSource in from.AllGlobalRefElements())
      {
        to.TrySetGlobalRefBinding(globalRefSource.ElementIndex, globalRefSource.Target);
      }
    }
  }

  internal static IEnumerable<ConnectionResult> TransferExternalReferences<N>(INode from, INode to, NodeQueryAcceleration query, NodeRuntime<N> runtime, bool overload = true) where N : class, INode
  {
    foreach (var element in query.GetReferencingElements(from))
    {
      // todo: type check
      yield return runtime.SetReference(element.OwnerNode, element.ElementIndex, to, overload, allowMergingGroups: true);
    }
  }

  internal static void TransferInternalReferences(INode from, INode to)
  {
    var lookup = to.AllReferenceElements().ToDictionary(o => o.DisplayName, o => o);

    foreach (var element in from.AllReferenceElements())
    {
      if (element.Target is INode referencedNode && lookup.TryGetValue(element.DisplayName, out var toReference))
      {
        toReference.Target = referencedNode;
      }
    }
  }


  internal static void TransferImpulses(INode from, INode to, bool tryByIndex = false)
  {
    foreach (var element in from.AllImpulseElements())
    {
      var toImpulse = to.GetImpulseByName(element.DisplayName);
      if (toImpulse.HasValue)
      {
        var impulse = toImpulse.Value;
        impulse.Target = element.Target;
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
    var impulsingFromElements = query.GetImpulsingElements(from);

    foreach (var element in impulsingFromElements)
    {
      var name = from.GetOperationName(element.Target.FindLinearOperationIndex());
      element.Target = to.GetOperationByName(name);
    }

    if (tryByIndex)
    {
      foreach (var source in impulsingFromElements)
      {
        source.Target = to.GetOperation(source.Target.FindLinearOperationIndex());
      }
    }
  }

  internal static Dictionary<(Type, Type), (string FromName, string ToName)[]> OutputMap = new() {
    {(typeof(For), typeof(RangeLoopInt)), [("Iteration", "Current")]},
    {(typeof(ValueNegate<>), typeof(ValuePlusMinus<>)), [("*", "Minus")]},
  };

  internal static bool TryGetOutputMap((Type, Type) typeTuple, [MaybeNullWhen(false)] out (string FromName, string ToName)[] elementMap) =>
    TryGetTypeTupleMapping(OutputMap, typeTuple, out elementMap);

  internal static void TransferOutputs(INode from, INode to, NodeQueryAcceleration query, bool tryByIndex = false)
  {
    // resize dynamic inputs to fit before transferring the outputs
    foreach (var fromOutputListMeta in from.Metadata.DynamicOutputs)
    {
      if (to.Metadata.GetOutputListByName(fromOutputListMeta.Name) is OutputListMetadata toOutputListMeta && fromOutputListMeta.TypeConstraint == toOutputListMeta.TypeConstraint)
      {
        var toOutputList = to.GetOutputList(toOutputListMeta.Index);
        var fromOutputList = from.GetOutputList(fromOutputListMeta.Index);
        toOutputList.EnsureSize(fromOutputList.Count);
      }
    }

    var typeTuple = (from.GetType().GetGenericTypeDefinitionOrSameType(), to.GetType().GetGenericTypeDefinitionOrSameType());
    var outputs = to.AllOutputElements().ToDictionary(o => o.DisplayName, o => o);
    var hasOutputMap = TryGetOutputMap(typeTuple, out var outputMap);
    var outputMapTable = outputMap?.ToDictionary();

    foreach (var evaluatingElement in query.GetEvaluatingElements(from))
    {
      if (evaluatingElement.SourceElement() is OutputElement outputElement)
      {
        if (tryByIndex && evaluatingElement.ValueType == outputElement.Target?.OutputType)
        {
          evaluatingElement.Source = to.GetOutput(outputElement.Target.FindLinearOutputIndex());
        }
        if (outputs.TryGetValue(outputElement.DisplayName, out var matchedOutputElement) && evaluatingElement.ValueType == matchedOutputElement.Target?.OutputType)
        {
          evaluatingElement.Source = matchedOutputElement.Target;
        }

        if (hasOutputMap && (outputMapTable?.TryGetValue(outputElement.DisplayName, out var remappedName) ?? false))
        {
          evaluatingElement.Source = to.GetOutputElementByName(remappedName)!.Value.Target;
        }
      }
    }
  }

  internal static Dictionary<(Type, Type), (string FromName, string ToName)[]> InputMap = new() {
    {(typeof(For), typeof(RangeLoopInt)), [("Count", "End")]},
    {(typeof(ValueNegate<>), typeof(ValuePlusMinus<>)), [("N", "Offset")]},
    {(typeof(FindChildByName), typeof(FindChildByTag)), [("Name", "Tag")]},
  };

  internal static bool TryGetInput((Type, Type) typeTuple, [MaybeNullWhen(false)] out (string FromName, string ToName)[] elementMap) =>
    TryGetTypeTupleMapping(InputMap, typeTuple, out elementMap);


  internal static void TransferInputs(INode from, INode to, bool tryByIndex = false)
  {
    // resize dynamic inputs to fit before transferring the inputs
    foreach (var fromInputListMeta in from.Metadata.DynamicInputs)
    {
      if (to.Metadata.GetInputListByName(fromInputListMeta.Name) is InputListMetadata toInputListMeta && fromInputListMeta.TypeConstraint == toInputListMeta.TypeConstraint)
      {
        var toInputList = to.GetInputList(toInputListMeta.Index);
        var fromInputList = from.GetInputList(fromInputListMeta.Index);
        toInputList.EnsureSize(fromInputList.Count);
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

    var lookup = to.AllInputElements().ToDictionary(e => e.DisplayName, e => e);

    foreach (var fromElement in from.AllInputElements())
    {
      if (fromElement.Source is IOutput output && lookup.TryGetValue(fromElement.DisplayName, out var toElement) && fromElement.ValueType == toElement.ValueType)
      {
        toElement.Source = output;
      }
    }


    // This can be made into a lookup or something nicer later if it comes up again, this is fine for now.
    var typeTuple = (from.GetType().GetGenericTypeDefinitionOrSameType(), to.GetType().GetGenericTypeDefinitionOrSameType());

    if (TryGetInput(typeTuple, out var elementMap))
    {
      foreach (var (fromName, toName) in elementMap)
      {
        var countIndex = from.Metadata.GetInputByName(fromName).Index;
        var endIndex = to.Metadata.GetInputByName(toName).Index;
        if (from.GetInputSource(countIndex) is IOutput output)
        {
          to.SetInputSource(endIndex, output);
        }
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

    TransferOperations(oldNode, newNode, query, tryByIndex);

    TransferImpulses(oldNode, newNode, tryByIndex);

    var results = TransferExternalReferences(oldNode, newNode, query, runtime, overload);

    TransferInternalReferences(newNode, oldNode);

    TransferGlobals(oldNode, newNode, tryByIndex);

    return results;
  }

  internal static bool TryGetTypeTupleMapping(Dictionary<(Type, Type), (string, string)[]> mapping, (Type, Type) typeTuple, [MaybeNullWhen(false)] out (string FromName, string ToName)[] elementMap)
  {
    if (mapping.TryGetValue(typeTuple, out elementMap))
    {
      return true;
    }
    else if (mapping.TryGetValue(typeTuple.SwapValues(), out elementMap))
    {
      elementMap = [.. elementMap.Select(t => t.SwapValues())];
      return true;
    }
    return false;
  }
}