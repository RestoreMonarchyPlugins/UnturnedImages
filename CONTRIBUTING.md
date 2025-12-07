# Contributing

There are two likely ways you may want to contribute to this project:

- Adding/updating asset images.
- Contributing to the project's tools source.

## Adding/updating asset images

Simply create a fork of the project, make your changes, and create a pull request. A better guide for contributing will be written when more standards are founded.

### Using the Icon Generator Module

The easiest way to generate icons is using the UnturnedImages module:

1. Install the module (see [README.md](/README.md#module-installation))
2. Configure `config.json` for your needs
3. Run the automated script or start Unturned with auto-start enabled
4. Copy generated icons from `Unturned/Extras/Items/` and `Unturned/Extras/Vehicles/`

For modded assets, set the `Mode` to `"mod"` and specify the workshop `ModId`.

## Project Tools

The `tools/` folder contains:

- **UnturnedImages.Module** - Unturned module for generating icons
- **UnturnedImages.Module.Bootstrapper** - Module loader

### Building the Module

```bash
cd tools/UnturnedImages.Module
dotnet build -c Release
```

### Module Features

- Icon generation for items and vehicles
- Automatic crash recovery (skips problematic assets)
- Auto-start for unattended generation
- PowerShell automation script

See [README.md](/README.md#unturnedimages-module) for detailed documentation.

### Making Changes

Create a pull request with your requested changes and discussion can be held there.

When contributing to the module:
1. Test your changes with both vanilla and modded assets
2. Ensure crash recovery still works properly
3. Update documentation if adding new features
