// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Tinyhand.Logging;

/// <summary>
/// Specifies the meaning and relative importance of log events.
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// Critical errors causing complete failure of the application.
    /// </summary>
    Fatal,

    /// <summary>
    /// Functionality is unavailable. Application may or may not continue.
    /// </summary>
    Error,

    /// <summary>
    /// Unexpected events. Application will continue.
    /// </summary>
    Warning,

    /// <summary>
    /// Normal behavior.
    /// </summary>
    Information,

    /// <summary>
    /// For debugging. Events that aren't necessarily observable from the outside.
    /// </summary>
    Debug,
}

public enum LogFormat
{
    /// <summary>
    /// [LogLevel] Message (Line/Position)
    /// </summary>
    Log,

    /// <summary>
    /// Message only
    /// </summary>
    Message,
}
