# EditorGraph #
Experimenting with extending Unity's editor functionality and C# reflection to create a graph editor.

## Setup ##

* Place the folder in your project's Assets directory.

* _Optional_: Create a folder titled 'Graphs' in the Assets directory as well.

## How to Use ##

### Creating a Graph ###

* Right-click the graph editor and select 'New Graph'.

* The editor will attempt to create a graph asset in your 'Assets/Graphs' directory, or just 'Assets' if you don't have that.

### Saving a Graph ###

* Graphs are autosaved after each change.

### Loading a Graph ###

* Right-click the graph editor and select 'Load Graph'.

### Creating a Node ###

* To create your own nodes, simply define functions in a class that derives off of FunctionLibrary.

* All function libraries are listed in the context menu, selecting functions from there automatically creates the nodes on your graph.

* I will be adding support for non-function nodes soon. Because it would be nice to plug values in and actually retrieve results from this.
