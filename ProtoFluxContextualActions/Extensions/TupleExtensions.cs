namespace ProtoFluxContextualActions.Extensions;

public static class TupleExtensions
{
  public static (B, A) SwapValues<A, B>(this (A A, B B) tuple) => (tuple.B, tuple.A);
}
