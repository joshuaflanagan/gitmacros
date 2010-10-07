
gitmacros
=========

_gitmacros_ is a set of Visual Studio macros for aiding my git workflow. 

Macros
------

#### GitHubMacros ####

* ShowFile - open the GitHub page for the current file
* ShowBlame - open the GitHub blame page for the current file
* ShowLog - open the GitHub commit history page for the current file

Installation
------------

1. Open the Macro Explorer (ALT + F8)
2. Right-click on MyMacros and choose New Module...
3. Name the module GitHubMacros (the name is important)
4. Right-click on the new GitHubMacros node within MyMacros, and choose Edit
5. In the Macro IDE, for the MyMacros project, Add a Reference to System.Core.dll
6. Copy the entire contents of GitHubMacros.vb over the conent in the GitHubMacros editor window
7. Save the MyMacros project

You should now be able to run the macros by double-clicking their names in the Macro Explorer.

Configuration
-------------

For convenience, you will probably want to map them to a toolbar button, context menu, or keyboard shorcut. I mapped them to keyboard shortcut chords:

1. In Visual Studio, go to Tools | Customize...
2. Click the Keyboard... button
3. In the "Press shortcut keys" input, press CTRL+L
4. For each of the listed items in "Shortcut currently used by"
 - search for that command in the "Show command containing" input
 - click the Remove button to unassign it from CTRL+L
5. In the "Show command containing input", type: GitHubMacros
6. Select ShowLog, in the shortcut keys input, press CTRL+L, CTRL+L
7. Select ShowFile, in the shortcut keys input, press CTRL+L, CTRL+F
8. Select ShowBlame, in the shortcut keys input, press CTRL+L, CTRL+B
9. Press OK and Closet to close the dialogs.


Known Issues
------------

1. The macros jump to the wrong github fork

    The macros try to figure out the correct GitHub web URL by reading your configured remotes. It will use the first remote it finds that refers to github.com. If you have multiple remotes for GitHub forks of your repository, and the macros bring you to the wrong page, you will need to manually edit your .git\config file and move the remote definitions so that your desired remote is listed first.