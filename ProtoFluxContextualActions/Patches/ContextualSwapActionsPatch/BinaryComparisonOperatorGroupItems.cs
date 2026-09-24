using Elements.Core;

using ProtoFlux.Core;
using ProtoFlux.Runtimes.Execution.Nodes;
using ProtoFlux.Runtimes.Execution.Nodes.Operators;

using ProtoFluxContextualActions.Types;
using ProtoFluxContextualActions.Utils;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  [NodeGroup]
  static readonly TypeSet ValueComparisonBinaryOperatorGroup = [
    new SystemTypeDefinition(typeof(ValueEquals<>)),
    new SystemTypeDefinition(typeof(ValueNotEquals<>)),
    new SystemTypeDefinition(typeof(ValueLessThan<>)),
    new SystemTypeDefinition(typeof(ValueLessOrEqual<>)),
    new SystemTypeDefinition(typeof(ValueGreaterThan<>)),
    new SystemTypeDefinition(typeof(ValueGreaterOrEqual<>)),
    PsuedoTypeDefinition.LessThan,
    PsuedoTypeDefinition.LessOrEqual,
    PsuedoTypeDefinition.GreaterThan,
    PsuedoTypeDefinition.GreaterOrEqual,
  ];

  [NodeGroup]
  static readonly TypeSet ObjectComparisonBinaryOperatorGroup = [
    new SystemTypeDefinition(typeof(ObjectEquals<>)),
    new SystemTypeDefinition(typeof(ObjectNotEquals<>)),
    new SystemTypeDefinition(typeof(ObjectLessThan<>)),
    new SystemTypeDefinition(typeof(ObjectLessOrEqual<>)),
    new SystemTypeDefinition(typeof(ObjectGreaterThan<>)),
    new SystemTypeDefinition(typeof(ObjectGreaterOrEqual<>)),
  ];
}
