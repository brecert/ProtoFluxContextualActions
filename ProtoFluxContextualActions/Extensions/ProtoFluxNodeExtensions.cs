using System.Collections.Generic;
using System.Linq;
using FrooxEngine;
using FrooxEngine.ProtoFlux;
using ProtoFlux.Core;
using ProtoFluxContextualActions.Utils.ProtoFlux;
using ImpulseSource = ProtoFluxContextualActions.Utils.ProtoFlux.ImpulseSource;

namespace ProtoFluxContextualActions.Extensions;

internal static partial class ProtoFluxNodeExtensions
{
  public static ISyncRef? GetImpulse(this ProtoFluxNode node, ImpulseSource source) =>
    source.ElementListIndex is int listIndex
      ? node.GetImpulseList(listIndex).GetElement(source.ElementIndex) as ISyncRef
      : node.GetImpulse(source.ElementIndex);

  public static INodeOperation? GetOperation(this ProtoFluxNode node, OperationSource source) =>
    source.ElementListIndex is int listIndex
      ? node.GetOperationList(listIndex).GetElement(source.ElementIndex) as INodeOperation
      : node.GetOperation(source.ElementIndex);

  public static INodeOutput? GetOutput(this ProtoFluxNode node, OutputSource source) =>
    source.ElementListIndex is int listIndex
      ? node.GetOutputList(listIndex).GetElement(source.ElementIndex) as INodeOutput
      : node.GetOutput(source.ElementIndex);

  public static ISyncRef? GetInput(this ProtoFluxNode node, InputSource source) =>
    source.ElementListIndex is int listIndex
      ? node.GetInputList(listIndex).GetElement(source.ElementIndex) as ISyncRef
      : node.GetInput(source.ElementIndex);
}