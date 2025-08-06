# ProtoFlux Contextual Actions

ProtoFlux Contextual Actions is a [Resonite Mod Loader](https://github.com/resonite-modding-group/ResoniteModLoader) mod that adds additional context menu actions for different contexts that revolve around protoflux.

Bug reports welcome, feel free to create an issue for any actions that you want added.

## Patches
There are currently three patches.

### Contextual Actions
Adds 'Contextual Actions' to the ProtoFlux Tool. Pressing secondary while holding a protoflux tool will open a context menu of actions based on what wire you're dragging instead of always spawning an input/display node. Pressing secondary again will spawn out an input/display node like normal.

### Contextual Swap Actions
Adds 'Contextual Swapping Actions' to the ProtoFlux Tool. Double pressing secondary pointing at a node with protoflux tool will be open a context menu of actions to swap the node for another node.

This is intended to be paired with Contextual Actions.
For example a `ValueLessThan` may be wanted when dragging a `float` output wire, however that node will not appear in the context menu by default. Instead a `ValueEquals` should be selected first, then swapped for `ValueLessThan` using contextual swap actions.

Some actions are grouped together like that in order to keep a soft limit of 10 maximum items in the context menu at once.
This may be made configurable at some point.

### DynamicVariableInput Creation
Adds a context menu item to create DynamicVariableInputs when holding a dynamic variable component with the ProtoFlux tool.

## Acknowledgements
The project structure is based on https://github.com/esnya/ResoniteEsnyaTweaks.