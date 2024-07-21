# How to setup

If the project gets opened in vscode, it will suggest to install the csharp tools extension. Install that one.
By pressing Ctrl+Shift+P you get a command menu. Enter `.Net Install: Install System-Wide`, it will ask for a version, enter `6.0.402`.

In the launch.json, for the "Play" configuration, you will have to change the path of the program there, to the path on your local machine.

To make vscode the default editor, when working in Godot, go to Editor -> Editor Settings -> Dotnet -> Editor -> External Editor and change it to Visual Studio Code.