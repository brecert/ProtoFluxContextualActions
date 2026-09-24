namespace ProtoFluxContextualActions.Types;

interface IType
{
  Type OriginalType { get; }
  Type[] GenericArguments { get; }
  Type? ResolvedType { get; }
}
