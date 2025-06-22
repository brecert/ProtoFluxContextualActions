namespace ProtoFluxContextualActions.Utils.ProtoFlux;

public interface IElementIndex
{
  int ElementIndex { get; }
  int? ElementListIndex { get; }
}

public static class ElementIndexExtensions
{
  public static bool IsDynamic(this IElementIndex element) => element.ElementListIndex.HasValue;
}