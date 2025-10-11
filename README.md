# ProtoFlux Contextual Actions

ProtoFlux Contextual Actions is a [Resonite Mod Loader](https://github.com/resonite-modding-group/ResoniteModLoader) mod that adds additional context menu actions for different contexts that revolve around protoflux.

Bug reports welcome, feel free to create an issue for any actions that you want added.

## Patches
There are currently a few patches.

### Contextual Actions
Adds 'Contextual Actions' to the ProtoFlux Tool. Pressing secondary while holding a protoflux tool will open a context menu of actions based on what wire you're dragging instead of always spawning an input/display node. Pressing secondary again will spawn out an input/display node like normal.

### Contextual Swap Actions
Adds 'Contextual Swapping Actions' to the ProtoFlux Tool. Double pressing secondary pointing at a node with protoflux tool will be open a context menu of actions to swap the node for another node.

This is intended to be paired with Contextual Actions.
For example a `ValueLessThan` may be wanted when dragging a `float` output wire, however that node will not appear in the context menu by default. Instead a `ValueEquals` should be selected first, then swapped for `ValueLessThan` using contextual swap actions.

https://github.com/user-attachments/assets/15ad6739-dbd2-44a1-a7f2-7315a6a429f5

Some actions are grouped together like that in order to keep a soft limit of 10 maximum items in the context menu at once.
This may be made configurable at some point.

### Dynamic Variable Input Creation
Adds a context menu item to create DynamicVariableInput nodes when holding a dynamic variable component with the ProtoFlux tool.

### Sample Spatial Variable Creation
Adds a context menu item to create SampleSpatialVariable nodes when holding a spatial variable source component with the ProtoFlux tool.

## Acknowledgements
The project structure is based on https://github.com/esnya/ResoniteEsnyaTweaks.
