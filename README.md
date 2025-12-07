# Unturned Images
A repository of images for assets in the game Unturned.

Images are available on the [images branch](https://github.com/SilKsPlugins/UnturnedImages/tree/images).

For information on contributing, [see the contributing page](/CONTRIBUTING.md).

## Purpose

Unturned Images is a public, open-source repository of images of in-game assets to be used either by plugins or websites.

The asset images are accessible via the [jsDeliver CDN](https://www.jsdelivr.com/?docs=gh). For an example, view the [Usage section](#Usage).

This project is public and free to anyone to be used by anyone and pull requests will be reviewed.

## Usage

To access images, it's recommended to use the [jsDeliver CDN](https://www.jsdelivr.com/?docs=gh). For item images, it's as simple as substituting the item ID in the following example.

When an image for the ID doesn't exist, the HTTP GET request will return a 404 error code.

Eaglefire image URL (item ID 4):
```
https://cdn.jsdelivr.net/gh/SilKsPlugins/UnturnedImages@images/vanilla/items/4.png
```

Blimp image URL (vehicle ID 189):
```
https://cdn.jsdelivr.net/gh/SilKsPlugins/UnturnedImages@images/vanilla/vehicles/189.png
```

## Plans

- [ ] Modded assets are planned and will likely start with the most popular mods (i.e. Elver). After which pull requests to add images for other mods will be accepted.

---

# UnturnedImages Module

The `tools/UnturnedImages.Module` folder contains an Unturned module for generating asset icons programmatically. This module provides:

- **Icon generation** for items and vehicles (vanilla and modded)
- **Automatic crash recovery** - if an asset crashes the game, it's automatically skipped on the next run
- **Auto-start** - unattended icon generation when the game starts
- **PowerShell automation script** - for fully automated batch processing with restart capability

## Module Installation

1. Build the module:
   ```bash
   cd tools/UnturnedImages.Module
   dotnet build -c Release
   ```

2. Copy the output to your Unturned installation:
   ```
   Unturned/
   └── Modules/
       └── unturnedimages/
           ├── UnturnedImages.Module.dll
           └── UnturnedImages.Module.Bootstrapper.dll
   ```

3. Create a `config.json` in your Unturned root folder (see [Configuration](#configuration)).

## Configuration

Create a `config.json` file in your Unturned installation folder:

```json
{
  "SkipGuids": [],
  "AutoStart": {
    "Enabled": false,
    "Mode": "all",
    "ModId": null,
    "GenerateItems": true,
    "GenerateVehicles": true,
    "ItemAngles": null,
    "VehicleAngles": null,
    "QuitWhenDone": false,
    "StartDelaySeconds": 5
  }
}
```

### Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `SkipGuids` | `Guid[]` | `[]` | List of asset GUIDs to skip. Automatically populated when assets cause crashes. |
| `AutoStart.Enabled` | `bool` | `false` | Enable automatic icon generation on game startup. |
| `AutoStart.Mode` | `string` | `"all"` | Generation mode: `"all"`, `"items"`, `"vehicles"`, or `"mod"`. |
| `AutoStart.ModId` | `ulong?` | `null` | Workshop mod ID when `Mode` is `"mod"`. |
| `AutoStart.GenerateItems` | `bool` | `true` | Generate item icons (for `"all"` or `"mod"` mode). |
| `AutoStart.GenerateVehicles` | `bool` | `true` | Generate vehicle icons (for `"all"` or `"mod"` mode). |
| `AutoStart.ItemAngles` | `float[]` | `null` | Custom rotation angles [X, Y, Z] for item icons. |
| `AutoStart.VehicleAngles` | `float[]` | `null` | Custom rotation angles [X, Y, Z] for vehicle icons. |
| `AutoStart.QuitWhenDone` | `bool` | `false` | Automatically quit the game when generation completes. |
| `AutoStart.StartDelaySeconds` | `float` | `5` | Delay in seconds before starting generation after game loads. |

### Example Configurations

**Generate all vanilla icons:**
```json
{
  "SkipGuids": [],
  "AutoStart": {
    "Enabled": true,
    "Mode": "all",
    "GenerateItems": true,
    "GenerateVehicles": true,
    "QuitWhenDone": true,
    "StartDelaySeconds": 10
  }
}
```

**Generate icons for a specific workshop mod:**
```json
{
  "SkipGuids": [],
  "AutoStart": {
    "Enabled": true,
    "Mode": "mod",
    "ModId": 3222327985,
    "GenerateItems": true,
    "GenerateVehicles": true,
    "QuitWhenDone": true,
    "StartDelaySeconds": 10
  }
}
```

**Generate only vehicle icons:**
```json
{
  "SkipGuids": [],
  "AutoStart": {
    "Enabled": true,
    "Mode": "vehicles",
    "QuitWhenDone": true,
    "StartDelaySeconds": 5
  }
}
```

## Crash Recovery

Some assets may cause the game to crash when generating icons (due to missing models, shader issues, etc.). The module handles this automatically:

1. Before processing each asset, it writes the asset GUID to `pending_asset.txt`
2. After successful processing, the file is deleted
3. If the game crashes, the file remains
4. On the next startup, the module detects this file, adds the GUID to `SkipGuids`, and saves the config

This means you can run the generator unattended - crashes are handled automatically and the problematic assets are skipped on subsequent runs.

## Automated Generation Script

For fully unattended batch processing, use the PowerShell script in `tools/UnturnedImages.Module/Scripts/`:

### Setup

1. Copy `RunIconGenerator.ps1` and `RunIconGenerator.bat` to your Unturned folder
2. Configure `config.json` with `AutoStart.Enabled: true` and `QuitWhenDone: true`
3. Run `RunIconGenerator.bat` (or `RunIconGenerator.ps1` directly from PowerShell)

### Script Features

- **Auto-restart on crash**: If the game crashes, it restarts automatically
- **Crash tracking**: Shows which assets caused crashes
- **Completion detection**: Stops when `generation_complete.txt` is created
- **Statistics**: Shows total runtime and restart count

### Script Parameters

```powershell
.\RunIconGenerator.ps1 [-UnturnedPath <path>] [-MaxRestarts <int>] [-RestartDelay <int>] [-StopOnComplete <bool>]
```

| Parameter | Default | Description |
|-----------|---------|-------------|
| `-UnturnedPath` | `C:\Program Files (x86)\Steam\steamapps\common\Unturned` | Path to Unturned installation |
| `-MaxRestarts` | `0` (unlimited) | Maximum number of restarts before stopping |
| `-RestartDelay` | `5` | Seconds to wait between crash and restart |
| `-StopOnComplete` | `$true` | Stop when generation completes |

### Example Usage

```powershell
# Use defaults
.\RunIconGenerator.ps1

# Custom Unturned path with max 50 restarts
.\RunIconGenerator.ps1 -UnturnedPath "D:\Games\Unturned" -MaxRestarts 50

# Quick restarts with 2 second delay
.\RunIconGenerator.ps1 -RestartDelay 2
```

## Output

Generated icons are saved to:
- **Items**: `Unturned/Extras/Items/{id}.png`
- **Vehicles**: `Unturned/Extras/Vehicles/{id}.png`

## Troubleshooting

### Icons not generating on startup

1. Check `Logs/Client.log` for `[AutoStart]` messages
2. Ensure `AutoStart.Enabled` is `true` in config.json
3. Increase `StartDelaySeconds` if assets haven't finished loading

### Game keeps crashing on the same asset

Check `config.json` - the crashed asset's GUID should be in `SkipGuids`. If not:
1. Check if `pending_asset.txt` exists in the Unturned folder
2. Manually add the GUID to `SkipGuids` if needed

### No icons appearing in Extras folder

1. Ensure the module DLLs are in `Modules/unturnedimages/`
2. Check `Logs/Client.log` for errors
3. Verify the module is loading (look for UnturnedImages log messages)
