using System.Collections.Generic;
using System.Linq;
using FrooxEngine;
using FrooxEngine.ProtoFlux;
using ProtoFlux.Core;
using ProtoFluxContextualActions.Utils.ProtoFlux;
using ImpulseElement = ProtoFluxContextualActions.Utils.ProtoFlux.ImpulseElement;

namespace ProtoFluxContextualActions.Extensions;

internal static partial class ProtoFluxNodeExtensions
{
  public static ISyncRef? GetImpulse(this ProtoFluxNode node, ImpulseElement source) =>
    source.ElementListIndex is int listIndex
      ? node.GetImpulseList(listIndex).GetElement(source.ElementIndex) as ISyncRef
      : node.GetImpulse(source.ElementIndex);

  public static INodeOperation? GetOperation(this ProtoFluxNode node, OperationElement source) =>
    source.ElementListIndex is int listIndex
      ? node.GetOperationList(listIndex).GetElement(source.ElementIndex) as INodeOperation
      : node.GetOperation(source.ElementIndex);

  public static INodeOutput? GetOutput(this ProtoFluxNode node, OutputElement source) =>
    source.ElementListIndex is int listIndex
      ? node.GetOutputList(listIndex).GetElement(source.ElementIndex) as INodeOutput
      : node.GetOutput(source.ElementIndex);

  public static ISyncRef? GetInput(this ProtoFluxNode node, InputElement source) =>
    source.ElementListIndex is int listIndex
      ? node.GetInputList(listIndex).GetElement(source.ElementIndex) as ISyncRef
      : node.GetInput(source.ElementIndex);
}