using System.Collections.Generic;
using System.Linq;
using ProtoFlux.Core;
using ProtoFluxContextualActions.Utils.ProtoFlux;
using ImpulseSource = ProtoFluxContextualActions.Utils.ProtoFlux.ImpulseSource;

namespace ProtoFluxContextualActions.Extensions;

internal static partial class NodeQueryAccelerationExtensions
{
  public static IEnumerable<ImpulseSource> GetImpulsingSources(this NodeQueryAcceleration query, INode node) =>
    query.GetImpulsingNodes(node).SelectMany(n => n.AllImpulseSources().Where(i => i.Target?.OwnerNode == node));

  public static IEnumerable<InputSource> GetEvaluatingSources(this NodeQueryAcceleration query, INode node) =>
    query.GetEvaluatingNodes(node).SelectMany(n => n.AllInputSources().Where(i => i.Source?.OwnerNode == node));

  public static IEnumerable<ReferenceSource> GetReferencingSources(this NodeQueryAcceleration query, INode node) =>
    query.GetReferencingNodes(node).SelectMany(n => n.AllReferenceSources().Where(r => r.Target == node));
}