using ProtoFluxContextualActions.Types;
using ProtoFluxContextualActions.Utils;

namespace ProtoFluxContextualActions.Patches;

static partial class ContextualSwapActionsPatch
{
  [NodeGroup]
  static readonly TypeSet BooleanVectorToBoolOperationsGroup = [
    PsuedoTypeDefinition.Any,
    PsuedoTypeDefinition.All,
    PsuedoTypeDefinition.None,
    PsuedoTypeDefinition.XorElements,
  ];
}
