namespace ProtoFluxContextualActions.Utils.ProtoFlux;

/// <summary>
/// Represents an index to a protoflux node compatible with both `INode` and and `ProtoFluxNode` elements.
/// </summary>
public interface IElementIndex
{
  /// <summary>
  /// the index of the element
  /// if the element is inside of a list this is the index of the element inside of the list
  /// </summary>
  int ElementIndex { get; }

  /// <summary>
  /// the index of the dynamic list the element is in
  /// `null` if the element is not inside of a list
  /// </summary>
  int? ElementListIndex { get; }
}

// public static class ElementIndexExtensions
// {
//   public static bool IsDynamic(this IElementIndex element) => element.ElementListIndex.HasValue;
// }