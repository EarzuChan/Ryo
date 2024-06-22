# Ryo (Me.EarzuChan.Ryo)

## Overview

Ryo is a comprehensive solution designed for browsing and editing game resources from the Simulacra series developed by
SEngine. The solution supports various types of game data, including game stories, save files, models, configurations,
and in-game audio-visual resources. The development stack consists mainly of .NET C# for the backend and TypeScript +
Vue3 for the front-end user interface.

The name "Ryo" is derived from the Japanese character "鿌" (Ryo), actually Ryo is a character from the anime [Bocchi the Rock!](https://en.wikipedia.org/wiki/Bocchi_the_Rock!).

The chinese version of this document is [here](README_CN.md).

## Features

- **Manage Game Resource Files**: Create, open, manage, import, export, and dump specific resource file system formats
  used in Simulacra games.
- **Utility Classes**: Built-in utilities for quick packaging and unpackaging of game textures and other resources.
- **Extensive API**: Provides a rich set of APIs for resource manipulation and conversion.
- **User-Friendly Editor**: We have an official editor application for general users to easily edit game resources.

## Usage

If you are a general user looking to immediately edit game resources, please
download Ryo Editor at [Release Page](https://github.com/earzuchan/ryo/releases). For support and updates, join the official Discord
community at [Hello Simulacra](https://discord.gg/KBhhVy2s).

## Core Projects

### Me.EarzuChan.Ryo.Core

Aka Ryo Library.

This is the core library that offers the primary functionalities of the Ryo solution.

To get started of this library, you can check out our [Wiki](https://github.com/EarzuChan/Ryo/wiki) for detailed
documentation.

#### Key Capabilities:

- **Resource File System Management**: Load, add, and manage resources in game data file systems (MassFile, ".fs").
- **Texture Handling**: Read, modify, and package game image resources (".texture").

#### Example Usage:

```csharp
// Load and manage game data file system resource package (MassFile, ".fs")
var massManager = new MassManager();
var mass = massManager.LoadMassFile("path/to/file.fs", "ResourcePackageName");
var dialogueTree = mass.Get<DialogueTree>(mass.IdStrPairs["ResourceItemKey"]);
mass.Add("ResourceItemKey", dialogueTree);

// Read game image resource (".texture")
var stream = FileUtils.OpenFile("path/to/photo.texture");
var textureFile = new TextureFile();
textureFile.Load(stream);
var fragmentalImage = textureFile.Get<FragmentalImage>(textureFile.ImageIDsArray.First().First()) ?? throw new FileNotFoundException("Default image not found");
Image outputImage = fragmentalImage.ToImage();
string savePath = FileName.Replace(".texture", "");
if (fragmentalImage.RyoPixmaps.First().First().IsJPG) {
    if (savePath.contains(".png")) savePath = savePath.replace(".png", ".jpg");
}
outputImage.Save(savePath);

// Package image into texture
FileStream fileStream = FileUtils.OpenFile(ImgPath);
FragmentalImage image = Image.Load(fileStream).ToFragmentalImage(512);
var txfile = new TextureFile();
txfile.ImageIDsArray.Add(new List<int> { txfile.Add(image) });
FileStream saveStream = FileUtils.OpenFile(FileName, true, true, false);
txfile.Save(saveStream);
```

### Me.EarzuChan.Ryo.Editor

Aka Ryo Editor.

An in-development GUI for Ryo built using the Me.EarzuChan.Ryo.WinWebAppSystem framework. It is a sleek and
user-friendly editor application allowing users to open and edit multiple resource packages simultaneously using a
visual editor view, akin to VSCode. This powerful editor app is designed to be easily used by general users.

We have an official user guide for Ryo Editor, which you can find in the App's Help menu.

### Me.EarzuChan.Ryo.Editor.UI

The front-end UI part of Me.EarzuChan.Ryo.Editor, built with Vue3 and TypeScript, and following
the [Material 3](https://m3.material.io) design system.

## Additional Projects

### Me.EarzuChan.Ryo.ConsoleSystem

A framework for quickly building command-line applications that can run anywhere.

### Me.EarzuChan.Ryo.ConsoleFrontEnd

Aka Ryo Console.

A command-line frontend application built using the Me.EarzuChan.Ryo.ConsoleSystem framework. The User Handbook
is [here](RYO_CONSOLE_USER_HANDBOOK.md).

### Me.EarzuChan.Ryo.ConsoleTest

A test application for the Ryo system, also built using the Me.EarzuChan.Ryo.ConsoleSystem framework.

### Me.EarzuChan.Ryo.Extensions

Provides additional capabilities such as generating schemas for game resource items, converting resource objects to
JSON, and restoring them from JSON.

### Me.EarzuChan.Ryo.WinWebAppSystem

A framework similar to Electron for building desktop web applications. It allows users to write backend code in C# and
provides many APIs for front-end (Web) and backend (C#) interaction, such as WebCall (similar to RESTful requests) and
WebEvent (bidirectional communication).

### Me.EarzuChan.Ryo.WPFImageConverter

A small WPF application for users to convert textures and images bidirectionally.

## Future Sister Solutions

- **Teio**: A launcher for Simulacra series games (using SEngine) across Windows and Android. It will support mods, use
  any game resource package, and include various modded resources markets.
- **Hachimi**: An IDE for Simulacra players interested in creating mods and derivative versions. It will facilitate
  unpacking original game apps (users must provide their own copies), managing game resources, one-click packing of mod
  resources, and integration with Ryo Editor.

## Contact

For any questions or suggestions, please reach out via QQ: 2421565269,
email: [huascq@gmail.com](mailto:huascq@gmail.com), or contact me on Discord through
the [Hello Simulacra](https://discord.gg/KBhhVy2s) server (or @earzu).