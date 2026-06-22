# Demosaic colMask VB

[日本語 README](README.ja.md)

An independent VB.NET / WinForms port inspired by [trimmerpop/Demosaic_colMask](https://github.com/trimmerpop/Demosaic_colMask). It scans Unity asset files for Shader objects and can disable mosaic-style rendering by setting selected Shader `colMask` values to `0`.

This is not a GitHub fork in the strict repository sense. The original project is Python/UnityPy-based; this version is a separate VB.NET implementation with a different save strategy focused on preserving Unity asset structure.

![Main window](docs/images/main-window.png)

## Highlights

- WinForms GUI for scanning Unity asset folders or files.
- Version is shown in the title bar so screenshots and reports identify the exact build.
- Automatic selection for configurable mosaic shader name keywords such as `mos`, `moz`, `masi`, `maz`, `censor`, `ピクセル`, and `モザイク`.
- `pixel` matches are highlighted in light orange for manual review instead of being auto-selected.
- Sortable shader lists and source-replacement suggestions.
- Dry-run confirmation before writing, showing target files, path IDs, backup mode, and planned writer type.
- Raw byte patching is preferred for loose `.assets` files across Unity versions, preserving original file length, object layout, metadata, padding, and unknown bytes whenever the `colMask` change is safe.
- Backup support with `.bak` files, plus a restore button for reverting backed-up files.
- Diagnostics for Texture2D, Sprite, and Material names that look like baked or 2D mosaics.
- Multilingual UI: English, Japanese, Traditional Chinese, Simplified Chinese, and Korean.
- APK repack/sign flow exists, but loose `.assets` files are the primary tested path.

## Why Raw Patch?

Some Unity assets, especially Unity 6000 assets without TypeTree data, can become unusable if a tool reads and rewrites the whole asset file. Even when the file still parses, the rewritten output may drop padding or unknown trailing bytes and prevent the game from starting.

For `colMask`, the desired edit is usually a small same-size value change. This tool therefore tries to patch only the affected bytes inside the original Shader object instead of reserializing the whole asset file.

If the raw patch safety checks fail, the tool does not overwrite the original file.

## Requirements

- Windows
- .NET 8 SDK or runtime
- `classdata.tpk` next to the executable or embedded in the project

For APK mode:

- Java in `PATH`
- `uber-apk-signer*.jar` next to the executable

## Build

```powershell
dotnet build --no-restore
```

The debug executable is generated under:

```text
bin\Debug\net8.0-windows\DemosaicColMaskVB.exe
```

Release packages can be built locally:

```powershell
dotnet publish -c Release -o .\artifacts\framework-dependent
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:PublishTrimmed=false -o .\artifacts\self-contained
```

GitHub Actions also builds both package types on `main` pushes and creates release assets for `v*` tags.

## Packages

- `windows-framework-dependent.zip`: smaller; requires the .NET 8 Windows Desktop Runtime.
- `windows-x64-self-contained.zip`: larger; includes the .NET runtime for Windows x64.

## Usage

1. Launch `DemosaicColMaskVB.exe`.
2. Drag and drop a Unity asset folder or double-click the path field to select one.
3. Review the detected Shader list.
4. Use **Keywords** if you want to adjust auto-selection names.
5. Optionally click **Diagnose** to search for Texture2D, Sprite, or Material names that may indicate baked/2D mosaics.
6. Move only the shaders you want to process into the selected list.
7. Click **Start Demosaic**, review the dry-run confirmation, then continue only if the target files and PathIDs are correct.
8. Keep the `.bak` files until you have confirmed that the game starts and renders correctly.
9. Use **Restore** if you need to copy `.bak` files back over their original files.

## Safety Notes

- Always test on a copy or keep backups.
- Read the dry-run confirmation before writing; it is the last in-app checkpoint before files are modified.
- Raw patch is used for loose `.assets` files when possible.
- Bundle and APK paths are more fragile because repacking can change compression, signing, or container layout.
- Shader replacement is riskier than `colMask` editing because replacement can require size or structure changes.
- 2D games or image-baked mosaics may not change visually. This tool only edits Shader `colMask` values; it cannot remove censorship already baked into textures, sprites, or image assets.
- Restore overwrites originals from `.bak` files but does not delete the `.bak` files.
- This tool is for local research and compatibility testing. Use it responsibly and respect applicable laws and content rules.

## Credits

Inspired by [trimmerpop/Demosaic_colMask](https://github.com/trimmerpop/Demosaic_colMask), a Python/UnityPy tool for editing Unity Shader `colMask` values.
