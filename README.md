# VsTweaks

Personal tweaks for Visual Studio 2026, packaged as one extension. Each feature lives in its own folder and is started by `VsTweaksPackage` once Visual Studio has loaded.

| Feature | Folder | What it does |
|---|---|---|
| Classic Solution Explorer | `SolutionExplorer\` | Restores the Visual Studio 2022 look of Solution Explorer |

## Requirements

Visual Studio 2026 (18.x) Community, Professional or Enterprise, x64.

## Build

Open `VsTweaks.csproj` in Visual Studio 2026 and build, or run:

```
MSBuild VsTweaks.csproj -restore -p:Configuration=Release
```

The output is `bin\Release\net48\VsTweaks.vsix`.

## Install and uninstall

1. Close every Visual Studio 2026 window.
2. Double-click `VsTweaks.vsix` and follow the prompts.
3. Start Visual Studio again.

If nothing changes after the restart, run `devenv /updateconfiguration` once from a Developer Command Prompt, then start Visual Studio.

To uninstall, open **Extensions > Manage Extensions > Installed**, uninstall VsTweaks, and restart.

Extensions do not roam with the Visual Studio account, so every machine needs its own install.

## Classic Solution Explorer

| | Visual Studio 2026 default | With VsTweaks |
|---|---|---|
| Indentation per level | 12 px | 18 px |
| Row height | 24 px (20 px in compact mode) | about 19 px (16 px icon with 2/1 px padding) |
| Expanders | Chevrons | Filled/hollow triangles |
| Selected row | Rounded, light fill | Full-width fill |
| Folder icons | Outline only | Outline with a half-opacity fill |
| Expanded folders | Still show the closed icon | Show the open-folder icon |

File icons are left as Visual Studio 2026 draws them.

### How it works

Solution Explorer's tree is an internal Visual Studio control with no public extensibility point. The feature finds the tree at run time and changes three things:

1. **Layout.** Visual Studio 2026 turns on an "alternate style" switch (`UseAlternateStyle`) on the tree, but the classic Visual Studio 2022 rendering code is still there. The feature pins the switch off, so the tree draws the classic way. Visual Studio writes the switch back on whenever it re-hosts the tree, so the value is held by a hold-forever animation; animated values take precedence over the local values Visual Studio writes.
2. **Row background.** Classic rows are filled with `TreeViewColors.Background`, while the Visual Studio 2026 tree uses the tool window background. Where the two colors differ, thin lines show between rows. Inside this tree only, that color is redirected to the tree's own background.
3. **Folder icons.** `ClassicFolderIcons.imagemanifest` provides the Visual Studio 2022 folder icons under the extension's own GUID, with the same IDs as the image catalog entries they replace. Each row's icon bindings go through a converter that swaps those folder icons for the classic ones, and uses the open-folder version for expanded nodes. The documentation says a custom manifest must not override the image catalog's GUID, so the swap happens only in Solution Explorer rows and nothing else is affected.

### Icon source

The XAML files in `SolutionExplorer\FolderIcons` are the 57 images from the Visual Studio 2022 (17.14) image catalog, `Microsoft.VisualStudio.ImageCatalog`, whose names contain `Folder` and whose artwork changed in Visual Studio 2026. They are unmodified. The images belong to Microsoft and are kept here for personal use only, which is why this repository is private. The official Visual Studio 2022 Image Library download is not a substitute: its versions of these icons have a much fainter fill.

### Compatibility

The feature looks up the internal types and properties it depends on by name. If a future Visual Studio changes them, the feature stops applying instead of breaking Visual Studio, and writes a warning to the ActivityLog (produced when Visual Studio is started with `devenv /log`).

## Adding a feature

1. Create a folder for the feature at the project root and use the namespace `VsTweaks.<Feature>`.
2. Start it from `VsTweaksPackage.InitializeAsync`. A feature that depends on Visual Studio internals should log an ActivityLog warning and skip itself when they cannot be resolved, never throw.
3. Give the feature's own assets (image manifests, XAML and so on) their own `ItemGroup` in `VsTweaks.csproj`, and register any image manifest under `Assets` in `source.extension.vsixmanifest`.
