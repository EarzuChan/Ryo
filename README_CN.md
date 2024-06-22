# 鿌 (Me.EarzuChan.Ryo)

## 概述

鿌 是一个综合解决方案，专为浏览和编辑由 SEngine 开发的 Simulacra
系列游戏资源而设计。该解决方案支持各种游戏数据类型，包括游戏剧情、存档、模型、配置和游戏内的视听资源。开发栈主要由 .NET C#
组成后端，前端用户界面部分由 TypeScript 和 Vue3 组成。

鿌
的名字源于日文，鿌实际上是动漫作品[孤独摇滚！](https://zh.wikipedia.org/wiki/%E5%AD%A4%E7%8D%A8%E6%90%96%E6%BB%BE%EF%BC%81_(%E5%8B%95%E7%95%AB))
中的一个角色。

## 特性

- **管理游戏资源文件**：创建、打开、管理、导入、导出和转储 Simulacra 游戏中使用的特定资源文件系统格式。
- **实用类**：内置实用程序，快速打包和解包游戏纹理及其他资源。
- **广泛的 API**：提供丰富的 API 用于资源操作和转换。
- **用户友好的编辑器**：我们有一个官方编辑器应用程序，普通用户可以很容易地编辑游戏资源。

## 使用说明

如果你是普通用户，想立即编辑游戏资源，请在 [发布页](https://github.com/earzuchan/ryo/releases) 下载 鿌
Editor。如需支持和更新，请加入官方
Discord 社区 [Hello Simulacra](https://discord.gg/KBhhVy2s)。

## 核心项目

### Me.EarzuChan.Ryo.Core

也叫 鿌 Library。

这是提供 鿌 解决方案主要功能的核心库。

要开始使用此库，您可以查看我们的 [Wiki](https://github.com/EarzuChan/Ryo/wiki) 以获取详细文档。

#### 主要功能：

- **资源文件系统管理**：加载、添加和管理游戏数据文件系统中的资源（MassFile，“.fs”）。
- **纹理处理**：读取、修改和打包游戏图像资源（“.texture”）。

#### 示例用法：

```csharp
// 加载和管理游戏数据文件系统资源包（MassFile，“.fs”）
var massManager = new MassManager();
var mass = massManager.LoadMassFile("path/to/file.fs", "ResourcePackageName");
var dialogueTree = mass.Get<DialogueTree>(mass.IdStrPairs["ResourceItemKey"]);
mass.Add("ResourceItemKey", dialogueTree);

// 读取游戏图像资源（“.texture”）
var stream = FileUtils.OpenFile("path/to/photo.texture");
var textureFile = new TextureFile();
textureFile.Load(stream);
var fragmentalImage = textureFile.Get<FragmentalImage>(textureFile.ImageIDsArray.First().First()) ?? throw new FileNotFoundException("找不到默认图片");
Image outputImage = fragmentalImage.ToImage();
string savePath = FileName.Replace(".texture", "");
if (fragmentalImage.RyoPixmaps.First().First().IsJPG) {
    if (savePath.contains(".png")) savePath = savePath.replace(".png", ".jpg");
}
outputImage.Save(savePath);

// 将图片打包成纹理
FileStream fileStream = FileUtils.OpenFile(ImgPath);
FragmentalImage image = Image.Load(fileStream).ToFragmentalImage(512);
var txfile = new TextureFile();
txfile.ImageIDsArray.Add(new List<int> { txfile.Add(image) });
FileStream saveStream = FileUtils.OpenFile(FileName, true, true, false);
txfile.Save(saveStream);
```

### Me.EarzuChan.Ryo.Editor

也叫 鿌 Editor。

一个正在开发中的 鿌 GUI，使用 Me.EarzuChan.Ryo.WinWebAppSystem 框架构建。它是一个优雅且用户友好的编辑器应用程序，允许用户同时打开和编辑多个资源包，使用类似
VSCode 的可视化编辑器视图。这款强大的编辑器应用程序旨在为普通用户提供简便的使用体验。

我们有一个官方的 鿌 Editor 用户指南，你可以在软件的帮助菜单中找到。

### Me.EarzuChan.Ryo.Editor.UI

Me.EarzuChan.Ryo.Editor 的前端 UI 部分，使用 Vue3 和 TypeScript 构建，并且遵循 [Material 3](https://m3.material.io) 设计系统。

## 附加项目

### Me.EarzuChan.Ryo.ConsoleSystem

一个快速构建随处可运行的命令行应用程序的框架。

### Me.EarzuChan.Ryo.ConsoleFrontEnd

也叫 鿌 Console。

使用 Me.EarzuChan.Ryo.ConsoleSystem 框架构建的命令行前端应用程序。用户手书在 [这里](RYO_CONSOLE_USER_HANDBOOK.md)
。（由于我们不计划积极更新此项目，因此我们暂不会为该文档撰写中文版）

### Me.EarzuChan.Ryo.ConsoleTest

一个用于测试 鿌 系统的应用程序，同样使用 Me.EarzuChan.Ryo.ConsoleSystem 框架构建。

### Me.EarzuChan.Ryo.Extensions

提供额外功能，如生成游戏资源对象的 Schema，将资源对象转换为 JSON，并从 JSON 重建资源对象。

### Me.EarzuChan.Ryo.WinWebAppSystem

类似于 Electron 的框架，用于构建桌面 Web 应用程序。允许用户使用 C# 编写后端代码，并提供许多前端（Web）和后端（C#）交互的 API，例如
WebCall（类似于 RESTful 请求）和 WebEvent（双向通信）。

### Me.EarzuChan.Ryo.WPFImageConverter

一个小型 WPF 应用程序，用户可以双向转换纹理和图像。

## 未来的姊妹解决方案

- **Teio**：一个跨 Windows 和安卓平台的 Simulacra 系列游戏启动器（使用 SEngine），支持 Mod，可使用任意游戏资源包，并包含各种改版资源市场。
- **Hachimi**：一个为 Simulacra 玩家制作的 IDE，旨在创作 Mod 和衍生版本。它将方便解包原版游戏应用（用户需自行提供原版包）、管理游戏资源、一键打包改版资源，并与
  鿌 Editor 联动。

## 联系方式

如有任何问题或建议，请通过以下方式联系我：QQ：2421565269，邮箱：[huascq@gmail.com](mailto:huascq@gmail.com)，或在 Discord
上通过 [Hello Simulacra](https://discord.gg/KBhhVy2s) 服务器联系我（或 @earzu）。