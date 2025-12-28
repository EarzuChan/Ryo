## 📢 Maintenance Status and Seeking Maintainers

**Note: This project is looking for new maintainers.**

Due to shifts in my personal career and life focus, I am no longer able to continue providing the active maintenance this ecosystem deserves. For the past three years, I have poured all my passion into these projects—despite no financial return, I have experienced tremendous growth and felt the joy brought by the community.

The vision remains ambitious, and the foundation remains solid:
*   [RyoCore](Me.EarzuChan.Ryo.Core): Featuring powerful performance and a rational architecture, built with C# with a focus on excellence.
*   [RyoEditor](Me.EarzuChan.Ryo.Editor): Dedicated to beautiful UI, powerful features, high performance, and high stability, although there is still a distance from fully achieving its goals.
*   Teio: An unexploited multiplatform Simulacra series game launcher with community potential.
*   [Sakiko](https://github.com/earzuchan/sakiko): A brand new and unified Kotlin multiplatform Hook framework, awaiting its first major application.

After three years of dedicated development, due to changes in my personal career, I am transitioning this project into a passive maintenance state. Although I can no longer be on the front lines, the roadmap for this ecosystem remains grand and feasible.

I am fading away (sadly), but the "flame" should not go out. I am looking for a visionary—not just someone who can "fix bugs," but a **torchbearer** who can carry these blueprints toward their final form.

To the next person in charge: You are inheriting a legacy that consolidates three years of effort and architectural evolution. You are the messenger of this great vision. If you have the drive to turn these possibilities into reality, please contact me or submit an Issue to discuss the transfer of maintenance/management rights.

# Ryo (Me.EarzuChan.Ryo)

[中文](README_CN.md)

## Overview

Ryo is a comprehensive solution designed for browsing and editing game resources from the Simulacra series developed by
SEngine. The solution supports various types of game data, including game stories, save files, models, configurations,
and in-game audio-visual resources. The development stack consists mainly of .NET C# for the backend and TypeScript +
Vue3 for the front-end user interface.

The name "Ryo" is derived from the Japanese character "鿌" (Ryo), actually Ryo is a character from the
anime [Bocchi the Rock!](https://en.wikipedia.org/wiki/Bocchi_the_Rock!).

## Features

- **Manage Game Resource Files**: Create, open, manage, import, export, and dump specific resource file system formats
  used in Simulacra games.
- **Utility Classes**: Built-in utilities for quick packaging and unpackaging of game textures and other resources.
- **Extensive API**: Provides a rich set of APIs for resource manipulation and conversion.
- **User-Friendly Editor**: We have an official editor application for general users to easily edit game resources.

## Usage

If you are a general user looking to immediately edit game resources, please
download Ryo Editor at [Release Page](https://github.com/earzuchan/ryo/releases). For support and updates, join the
official Discord
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
mass.Add("ResourceItemKey2", dialogueTree);

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

An in-development GUI for Ryo built using the Me.EarzuChan.Ryo.Kurisu framework. It is a sleek and
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

### Me.EarzuChan.Ryo.Kurisu

A framework similar to Electron for building desktop web applications. It allows users to write backend code in C# and
provides many APIs for front-end (Web) and backend (C#) interaction, such as WebCall (similar to RESTful requests) and
WebEvent (bidirectional communication).

The framework's name is from Makise Kurisu from Steins;Gate, she is my assistant ~~wife~~, no adding Tina Kora!

## Future Sister Solutions

- **Teio**: A launcher for Simulacra series games (using SEngine) across Windows and Android. It will support mods, use
  any game resource package, and include various modded resources markets.
- **TeioAPI**: A set of APIs for Teio Mod Developers to interact with the launcher and manipulate game and resources.
- **~~Soyorin~~No Name Yet**: An IDE for Simulacra players interested in creating mods and derivative versions. It will facilitate
  unpacking original game apps (users must provide their own copies), managing game resources, one-click packing of mod
  resources, and integration with Ryo Editor.
- **Sakiko**: Next-generation JvmHook framework, planned to support desktop and Android platforms, providing the ability
  to register game lifecycle events and custom hooks for TeioAPI.

## Contact

For any questions or suggestions, please reach out via QQ: 2421565269,
email: [huascq@gmail.com](mailto:huascq@gmail.com), or contact me on Discord through
the [Hello Simulacra](https://discord.gg/KBhhVy2s) server (or @earzu).