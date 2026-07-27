<p align="center">
  <img src="SmartExchanger/Assets/Icons/large-logo.png"
       width="240"
       alt="SmartExchanger logo" />
</p>

<h1 align="center">Smart Exchanger</h1>

<p align="center">
  <strong>Procedural Texture and Material Editor for Windows</strong>
</p>

<p align="center">
  A node-based desktop application for generating, transforming, previewing,
  and exporting procedural textures and material maps.
</p>

> [!WARNING]
> SmartExchanger is currently in an early alpha stage and is under development.
> Features, nodes, the user interface, and rendering behaviour may change between releases.

## About

SmartExchanger is a node-based texture and material editor built for Windows.
It allows textures to be created by connecting generators, filters,
transformations, utility nodes, and output nodes in a visual graph.

The application uses GPU-accelerated rendering for texture generation and
provides both a 2D texture preview and a real-time PBR material preview on a
3D sphere.

The long-term goal is to create a lightweight and approachable procedural
material authoring tool.

## Features

* Visual node-based texture editing
* GPU-accelerated texture rendering with SkiaSharp
* Procedural noise and gradient generators
* Loading custom PNG and JPEG textures
* Texture filtering, blending, masking, and transformation
* Height map to normal map conversion
* Texture scattering with randomized placement
* Dedicated 2D texture preview
* PBR material preview on a 3D sphere
* Base color, normal, roughness, metallic, and opacity material inputs
* Selectable DDS environment maps for material lighting
* Texture export to PNG, JPEG, and TIFF
* Configurable rendering and preview settings through `appsettings.json`

## Available nodes

### Start nodes

* **Color** — generates a solid RGB color
* **Value** — generates a grayscale value
* **Texture Input** — loads a PNG or JPEG image
* **Linear Gradient** — generates a configurable linear gradient
* **Texture Size** — controls the output texture resolution

### Generator and processing nodes

* **Perlin Noise Fractal**
* **Perlin Noise Turbulence**
* **Worley Noise**
* **Blend**
* **Threshold**
* **Invert**
* **Height To Normal**
* **Translate 2D**
* **Scatter Texture**
* **Alpha To Mask**
* **Apply Opacity Mask**
* **Reroute**

### Output nodes

* **Output** — displays and exports the final texture
* **Texture Preview** — displays a larger 2D texture preview
* **Material Output** — sends texture maps to the PBR material preview

## System requirements

* Windows 10 or Windows 11
* 64-bit operating system
* A GPU with current Windows graphics drivers

The official installer should be published as a self-contained application,
so users do not need to install the .NET Desktop Runtime separately.

## Installation

1. Open the [Releases](../../releases) page.
2. Download the latest `SmartExchanger-<version>-win-x64.msi` installer.
3. Run the installer and follow the setup wizard.
4. Launch SmartExchanger from the Start menu or the desktop shortcut.

SmartExchanger is currently distributed only for 64-bit Windows.

## Quick start

1. Start SmartExchanger.
2. Right-click an empty area of the graph workspace.
3. Open **Create Node** and add a generator, such as **Linear Gradient** or
   **Worley Noise**.
4. Add an **Output**, **Texture Preview**, or **Material Output** node.
5. Drag from an output connector to a compatible input connector.
6. Change the node parameters and observe the preview update.
7. To export a texture, right-click an **Output** node and select **Export**.

A **Texture Size** node is created automatically and controls the resolution
used when rendering and exporting textures.

## Material preview

The **Material Output** node provides the following inputs:

* **Base Color**
* **Normal**
* **Roughness**
* **Metallic**
* **Opacity**

Connect generated textures to these inputs to preview the material on the 3D
sphere. The environment map can be selected from the material preview header
to inspect reflections under different lighting conditions.

## Exporting textures

Textures are exported from the context menu of an **Output** node.

Supported formats:

* **PNG** — suitable for lossless textures and transparency
* **JPEG** — suitable for smaller opaque images; transparency is replaced with white
* **TIFF** — lossless export with ZIP compression

The exported resolution is determined by the **Texture Size** node. JPEG
quality and other application defaults can be adjusted in `appsettings.json`.

## Building from source

### Requirements

* Visual Studio 2026 version 18.0 or newer
* The **.NET desktop development** workload
* .NET 10 SDK
* Git

The project currently targets:

```text
net10.0-windows10.0.19041.0
```

## Technology stack

* **WPF** — desktop user interface
* **.NET 10** — application runtime
* **CommunityToolkit.Mvvm** — MVVM infrastructure and generated commands
* **Nodify** — visual node editor
* **SkiaSharp** — GPU texture rendering
* **SKSL** — custom shader effects
* **Helix** — 3D PBR material preview


## Configuration

Application defaults are stored in:

```text
SmartExchanger/appsettings.json
```

The file currently contains settings for:

* application name and splash screen timing
* default texture and preview sizes
* GPU cache limits
* material preview defaults
* environment map directory and default environment map
* shader directory
* JPEG export quality

Invalid values may prevent the application from starting correctly. Keep a
backup before changing configuration manually.

## Repository overview

```text
SmartExchanger/
├── Assets/          Application icons and environment maps
├── Configuration/  Dependency injection and options registration
├── Models/          Rendering and preview data models
├── Options/         Strongly typed application options
├── Resources/       WPF resources and node templates
├── Services/        Shared application services
├── Shaders/         SKSL shader source files
├── ViewModels/      Editor, preview, and node view models
├── Views/           WPF windows and controls
└── appsettings.json Application configuration
```

## Known limitations

* SmartExchanger is available only for Windows.
* Undo and redo are not yet available.
* The node set and material workflow are still incomplete.
* Performance depends on texture resolution, graph complexity, and GPU drivers.
* Early unsigned installers may display a Windows security warning.

## Roadmap

* [ ] Undo and redo
* [ ] Copy and Paste selected node
* [ ] Additional procedural generators and filters

## Author

Created and maintained by **Bartosz Zięba**.

## License

This project is source-available under the PolyForm Strict License 1.0.0.

The source code is publicly available for review, educational purposes, testing, and portfolio evaluation. Modification, redistribution, creation of derivative works, and commercial use are not permitted.

See the `LICENSE` file for the complete terms.
