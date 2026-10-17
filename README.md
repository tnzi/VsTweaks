# VsTweaks

Personal tweaks for Visual Studio 2026, packaged as one extension. Each feature lives in its own folder and is started by `VsTweaksPackage` once Visual Studio has loaded.

| Feature | Folder | What it does |
|---|---|---|
| Classic Solution Explorer | `SolutionExplorer\` | Restores the Visual Studio 2022 layout of Solution Explorer |
| Classic folder icons | `FolderIcons\` | Shows the filled Visual Studio 2022 folder icons instead of the Visual Studio 2026 outlines, in every WPF window |
| Themes | `Themes\` | Adds the VsTweaks Light theme and draws active window frames with the theme's border color instead of its accent |

`ShellTreeInternals.cs` and `VisualTrees.cs` at the root are shared by several features.

## Requirements

Visual Studio 2026 (18.x) Community, Professional or Enterprise, x64.

## Build

Open `VsTweaks.csproj` in Visual Studio 2026 and build, or run:

```
MSBuild VsTweaks.csproj -restore -t:Rebuild -p:Configuration=Release
```

The output is `bin\Release\net48\VsTweaks.vsix`. Use a full rebuild after moving or renaming icon files: `git mv` keeps file timestamps, so an incremental build can leave the old resource paths in the assembly.

## Install and uninstall

1. Close every Visual Studio 2026 window.
2. Double-click `VsTweaks.vsix` and follow the prompts.
3. Start Visual Studio again.

If nothing changes after the restart, run `devenv /updateconfiguration` once from a Developer Command Prompt, then start Visual Studio.

To upgrade an installed copy, raise `Version` in `source.extension.vsixmanifest` before building; double-clicking the new VSIX then upgrades in place. A VSIX with the same version as the installed one is reported as already installed and changes nothing.

To uninstall, open **Extensions > Manage Extensions > Installed**, uninstall VsTweaks, and restart.

Extensions do not roam with the Visual Studio account, so every machine needs its own install.

## Classic Solution Explorer

| | Visual Studio 2026 default | With VsTweaks |
|---|---|---|
| Indentation per level | 12 px | 18 px |
| Row height | 24 px (20 px in compact mode) | about 19 px (16 px icon with 2/1 px padding) |
| Expanders | Chevrons | Filled/hollow triangles |
| Selected row | Rounded, light fill | Full-width fill |

### How it works

Solution Explorer's tree is an internal Visual Studio control with no public extensibility point. The feature finds the tree at run time and changes two things:

1. **Layout.** Visual Studio 2026 turns on an "alternate style" switch (`UseAlternateStyle`) on the tree, but the classic Visual Studio 2022 rendering code is still there. The feature pins the switch off, so the tree draws the classic way. Visual Studio writes the switch back on whenever it re-hosts the tree, so the value is held by a hold-forever animation; animated values take precedence over the local values Visual Studio writes.
2. **Row background.** Classic rows are filled with `TreeViewColors.Background`, while the Visual Studio 2026 tree uses the tool window background. Where the two colors differ, thin lines show between rows. Inside this tree only, that color is redirected to the tree's own background.

## Classic folder icons

Every icon that shows a folder as an item (`FolderClosed`, `FolderOpened`, hidden, linked, missing, properties, reference, web and similar folders, 38 in all) is shown with the Visual Studio 2022 artwork, an outline with a half-opacity fill, instead of the Visual Studio 2026 outline. In Visual Studio's tree control, an expanded folder also shows the open-folder icon, as Visual Studio 2022 did. Command icons such as New Folder or Add Folder are left alone, so menus and toolbars do not mix two icon styles; so are file icons.

### How it works

The image library keeps the first definition of an icon and ignores later ones, and the documentation says a custom image manifest must not reuse the image catalog's GUID, so the catalog's folder images cannot be replaced at the source. `ClassicFolderIcons.imagemanifest` instead provides the classic images under the extension's own GUID, with the same IDs as the catalog images, and the feature swaps the icon where it is displayed:

- **CrispImage**, the icon control used by most WPF windows and dialogs (Git Changes, Git Repository, tool windows). CrispImage raises a static `Created` event for every instance. The feature replaces the one internal binding that turns its `Moniker` into what it renders, so the swap applies however `Moniker` is set.
- **Visual Studio's tree control** (`VirtualizingTreeView`, used by Solution Explorer and other hierarchy windows), whose rows draw their icons themselves. Each row's icon bindings are rebound through the same swap.

Older windows built on native Win32 trees, such as Class View, get their icons from bitmap lists and keep the Visual Studio 2026 artwork.

### Icon source

The XAML files in `FolderIcons\Images` are the corresponding images from the Visual Studio 2022 (17.14) image catalog, `Microsoft.VisualStudio.ImageCatalog`, unmodified. The images belong to Microsoft and are kept here for personal use only, which is why this repository is private. The official Visual Studio 2022 Image Library download is not a substitute: its versions of these icons have a much fainter fill.

## Compatibility

Features that rely on Visual Studio internals look up the types and properties they need by name. If a future Visual Studio changes them, the feature stops applying instead of breaking Visual Studio, and writes a warning to the ActivityLog (produced when Visual Studio is started with `devenv /log`).

## Themes

### VsTweaks Light

Visual Studio 2026 Light with a few colors changed. Select it under **Tools > Options > Environment > Visual Experience > Color theme**, or set `"environment.visualExperience.colorTheme": "vstweaks-light"` in `settings.json`.

| Area (`ShellInternal` color) | Light | VsTweaks Light |
|---|---|---|
| Active window frame and tab (`EnvironmentBorder`) | Indigo accent, `#5649B0` | Neutral gray, `#9A9A9A`; inactive frames stay `#ADADAD` |
| Tab strips and toolbar trays (`EnvironmentLayeredBackground`) | Half-transparent white, about `#F7F7F7` | `#EDEDED` |
| Tool window title rows (`EnvironmentHeader`, `EnvironmentHeaderInactive`) | `#F9F9F9`, the same as the tool window body | `#EDEDED` |
| Title bar, menu bar, toolbar area (`EnvironmentBackground`) and gutters between windows (`EnvironmentBody`) | `#EEEEEE` | `#E6E6E6` |

Content keeps Light's colors (editor `#FFFFFF`, tool window bodies `#F9F9F9`), so the result is three clear brightness steps: content, then the panels around it, then the environment. Every color not listed falls back to Light.

### How themes are built

A theme is a `*.vstheme` file in `Themes\`, in the XML format of the Visual Studio Color Theme Designer: a `Theme` with a `GUID` and a `FallbackId`, `Category` elements, and `Color` elements whose `Background`/`Foreground` have `Type="CT_RAW"` and an `AARRGGBB` `Source`. `Themes.targets` compiles each one at build time into the binary `.pkgdef` format Visual Studio reads theme colors from, and packs it into the VSIX under `Themes\`. Each compiled `.pkgdef` must also be listed as a `Microsoft.VisualStudio.VsPackage` asset in `source.extension.vsixmanifest`.

The theme's moniker, the value used in `settings.json`, is its name in kebab case (`VsTweaks Light` becomes `vstweaks-light`), matching the built-in themes.

To find a color's category and name, look at the theme `.pkgdef` files under `Common7\IDE\CommonExtensions\Platform` in the Visual Studio installation (for Light: `Shell.Light.pkgdef`, `ShellInternal.Light.pkgdef`, `Theme.Light.pkgdef`).

### Neutral frame borders

Visual Studio 2026 outlines the active document or tool window, and its tab, with the theme's accent color (`Shell.AccentFillDefault`). Primary buttons, check boxes and focus lines use the same color, so a theme cannot change the outline alone. Inside window frames only (tool windows, tool window tab groups, the document well and the auto-hide tab strip), `NeutralFrameBorders` redirects the accent to the theme's environment border color, and restores the real accent for the content each frame hosts. Built-in themes use one color for both, so they look unchanged; VsTweaks Light sets the environment border to gray.

## Adding a feature

1. Create a folder for the feature at the project root and use the namespace `VsTweaks.<Feature>`.
2. Start it from `VsTweaksPackage.InitializeAsync`. A feature that depends on Visual Studio internals should log an ActivityLog warning and skip itself when they cannot be resolved, never throw.
3. Give the feature's own assets (image manifests, XAML and so on) their own `ItemGroup` in `VsTweaks.csproj`, and register any image manifest under `Assets` in `source.extension.vsixmanifest`.
