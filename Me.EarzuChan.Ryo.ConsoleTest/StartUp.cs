﻿using Me.EarzuChan.Ryo.ConsoleSystem;
using Me.EarzuChan.Ryo.ConsoleSystem.CommandManagement;
using System.Text;

Console.InputEncoding = Encoding.Unicode;

CommandManager commandManager = new(new ConsoleInfo { Name = "Console Test" });

commandManager.InitConsole();

CommandApplication.CreateBuilder().UseDefaultConsoleBackend().Build().Run();