# EditorGraph #
Experimenting with extending Unity's editor functionality and C# reflection to create a graph editor.

## Setup ##

* Place the folder in your project's Assets directory.

* Create a folder titled 'Graphs' in the Assets directory as well.

## How to Use ##

### Creating a Graph ###

Currently, you can't do a lot with this yet. You can create a graph containing nodes that can't be linked together.

* Pressing the 'New Graph' button will attempt to save an asset to your 'Assets/Graphs' folder, so make sure that exists.

* The asset it creates will always have the same name, so make sure to rename it yourself if you don't want your graph to be overwritten by the New Graph button.

* All of this will be fixed later.

### Saving a Graph ###

* Graphs are autosaved after each change.

### Loading a Graph ###

* Pressing 'Load Graph' will bring up an asset selector from which you can choose a graph to load in.

* Simply select it and everything should be good, there are some issues with the UI not resetting state, it's currently a bit hacked together.

### Creating a Node ###

* Below the 'New Graph' button you will see a list of buttons (currently 1), these are function libraries.

* Selecting one of these expands to reveal its functions.

* Clicking a function button creates a node.

* To create your own nodes, simply define functions in a class that derives off of FunctionLibrary.
