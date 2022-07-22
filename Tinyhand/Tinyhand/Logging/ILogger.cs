// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand.Tree;

namespace Tinyhand.Logging;

public interface ILogger
{
    void Log(LogLevel level, Element? element, string message);

    void Debug(Element? element, string message) => this.Log(LogLevel.Debug, element, message);

    void Information(Element? element, string message) => this.Log(LogLevel.Information, element, message);

    void Warning(Element? element, string message) => this.Log(LogLevel.Warning, element, message);

    void Error(Element? element, string message) => this.Log(LogLevel.Error, element, message);

    void Fatal(Element? element, string message) => this.Log(LogLevel.Fatal, element, message);
}
