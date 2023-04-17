# Real-time Watch Window

This package adds a "Watch Window" to the Unity Editor, allowing for quick inspection and analysis.

![Watch Window demo](./Documentation~/watch.gif)

- Watch queries can be *any C# expression*.
- Runs in both Edit and Play modes.
- Values can be graphed over time to visualize their changes.
- Variables can be set up to reference Objects in the scene. References will attempt to persist between runs.
- Expressions can include references to any object members, even if they're `private`, `protected`, or `internal`.

You can integrate this package into your own project by using the Package Manager to [add the following Git URL](https://docs.unity3d.com/Manual/upm-ui-giturl.html):

```txt
https://github.com/oculus-samples/Unity-Decommissioned.git?path=Packages/com.meta.utilities.watch-window
```

## Using a Watch

Use the "Window/Analysis/Watch Window" menu to open, or press `ctrl+shift+w`.

The *Watch Window* works very similarly to the watch window in Visual Studio (or any other debugger). Click "New Watch" to create a watch element. In it, you can type in any C# expression. Next to the entry, it will show the result of that expression (in JSON format, so classes will work, too!).

![Watch Expression demo](./Documentation~/new-watch.png)

If the result is a built-in type, it will display its `ToString()`. If the result is an `object`, it will convert it to JSON. In addition, if the result is a `UnityEngine.Object`, it will display a button; clicking the button will select the object.

Unlike a normal Watch Window, this one updates every frame.

![Real-time Updates demo](./Documentation~/realtime.gif)

### Graphing

Clicking the ![graph](./Editor/Resources/graphs_Outline_24.png) button will toggle a graph view. If the result is a number or vector type, it will render its value over time.

![Graphing demo](./Documentation~/graph.gif)

Clicking the "EDITOR" button will toggle between updating every Editor frame and only updating on Game (Play mode) frames. Using the scroll wheel over a graph will zoom its time axis in and out.

## Using a Variable

Variables can be useful for referencing objects in the scene without having to use methods like `GameObject.Find`.

Click "New Variable" to create a new Watch Variable. This variable can be used by watches. The text field sets the Variable's identifier.

To set the value of a Variable, You can use the object field's picker, or you can drag in an object from the hierarchy. Even better, you can drag in a component from the Inspector.

![Watch Variables demo](./Documentation~/variables.gif)

Once a Variable is set up, you can use its identifier in a Watch's expression as if it were a normal C# variable.

## Settings

Settings can be accessed by clicking the settings button in the corner of the window. From there, you can set the "code precursor" that is inserted before each Watch expression. This generally includes any `using` statements needed to make the expression compile. Settings are stored in `Assets/Editor/WatchWindowSettings.asset`.
