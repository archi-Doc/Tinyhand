// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Unit;
using Tinyhand.Tree;

namespace Tinyhand;

/// <summary>
/// The log source of <see cref="IProcessEnvironment.Result"/>.<br/>
/// <see cref="DefaultLog"/> is used as the log source of <see cref="IProcessEnvironment.Log"/>.
/// </summary>
public class ResultLog
{
}

/// <summary>
/// Selects the log output of the <c>log</c> and <c>result</c> directives.
/// </summary>
public enum ProcessLogOutput
{
    /// <summary>
    /// Discards the log (<see cref="EmptyLogger"/>).
    /// </summary>
    None,

    /// <summary>
    /// Writes to the console (<see cref="ConsoleLogger"/>).
    /// </summary>
    Console,

    /// <summary>
    /// Writes to a file (<see cref="FileLogger{TOption}"/>).
    /// </summary>
    File,

    /// <summary>
    /// Writes to both the console and a file (<see cref="ConsoleAndFileLogger"/>).
    /// </summary>
    ConsoleAndFile,
}

/// <summary>
/// Specifies the log message format.
/// </summary>
public enum ProcessLogFormat
{
    /// <summary>
    /// Timestamp, log level and message.
    /// </summary>
    Log,

    /// <summary>
    /// Log level and message.
    /// </summary>
    Message,
}

/// <summary>
/// Writes a log message with the position (Line/BytePosition) of the <see cref="Element"/> which the message refers to.
/// </summary>
public static class ProcessLoggerExtensions
{
    public static void Debug(this ILogger logger, Element? element, string message)
        => logger.GetWriter(LogLevel.Debug)?.Write(AddPosition(element, message));

    public static void Information(this ILogger logger, Element? element, string message)
        => logger.GetWriter(LogLevel.Information)?.Write(AddPosition(element, message));

    public static void Warning(this ILogger logger, Element? element, string message)
        => logger.GetWriter(LogLevel.Warning)?.Write(AddPosition(element, message));

    public static void Error(this ILogger logger, Element? element, string message)
        => logger.GetWriter(LogLevel.Error)?.Write(AddPosition(element, message));

    /// <summary>
    /// Writes a fatal message.<br/>
    /// Use <see cref="IProcessEnvironment.Fatal(Element?, string)"/> instead to also abort the process.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="element">The element which the message refers to (<see langword="null"/> to omit the position).</param>
    /// <param name="message">The message.</param>
    public static void Fatal(this ILogger logger, Element? element, string message)
        => logger.GetWriter(LogLevel.Fatal)?.Write(AddPosition(element, message));

    /// <summary>
    /// Appends the position of the <see cref="Element"/> which the message refers to.
    /// </summary>
    /// <param name="element">The element (<see langword="null"/> to return the message as is).</param>
    /// <param name="message">The message.</param>
    /// <returns>The message with the position of the element.</returns>
    public static string AddPosition(Element? element, string message)
        => element is null ? message : $"{message} (Line:{element.LineNumber} BytePosition:{element.BytePositionInLine})";
}
