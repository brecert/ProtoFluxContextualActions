using FrooxEngine.ProtoFlux;

using System.Collections.Generic;

namespace ProtoFluxContextualActions.Patches;


static partial class ContextualSelectionActionsPatch
{
  internal static IEnumerable<MenuItem> GeneralObjectOperationMenuItems(ProtoFluxElementProxy? target)
  {
    // this is no longer used (other parts handle this as well)
    // but maybe some things can be added here / moved back here
    /*if (target != null)
	{	
      var targetType = target.GetType();
      var typeArgs = targetType.GenericTypeArguments;
      var nodeType = typeArgs[typeArgs.Length - 1];
      var coder = Traverse.Create(typeof(Coder<>).MakeGenericType(nodeType));

      if (coder.Property<bool>("SupportsComparison").Value)
      {
        yield return new MenuItem(typeof(ObjectEquals<>).MakeGenericType(nodeType));
      }
      if (nodeType.IsNullable())
	  {
        yield return new MenuItem(typeof(IsNull<>).MakeGenericType(nodeType));
        yield return new MenuItem(typeof(NotNull<>).MakeGenericType(nodeType));
	  }
      if (target is ProtoFluxOutputProxy { OutputType.Value: var outputType } && !outputType.IsUnmanaged())
      {
      }
	}*/
    yield break;
  }
}