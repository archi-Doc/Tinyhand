// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Threading.Tasks;
using Arc.Unit;
using Tinyhand.Tree;

namespace Tinyhand;

/// <summary>Executes a named operation over elements in a Tinyhand process script.</summary>
public interface IProcessCore
{
    // public static string StaticName => "process name";

    /// <summary>
    /// Gets the process name.
    /// </summary>
    string ProcessName { get; } // => this.StaticName;

    /// <summary>
    /// Called when the process is changed to this core.
    /// </summary>
    /// <param name="environment">The active script environment and loggers.</param>
    void Initialize(IProcessEnvironment environment);

    /// <summary>
    /// Called when the process is complete and changed to another process core.
    /// </summary>
    void Uninitialize();

    /// <summary>
    /// Processes one script element.
    /// </summary>
    /// <param name="element">The next element in the script.</param>
    /// <returns>A task that reports whether processing succeeded.</returns>
    Task<bool> Process(Element element);
}

/// <summary>Provides script paths, loggers, and failure reporting to process cores.</summary>
public interface IProcessEnvironment
{
    /// <summary>
    /// Gets a default logger (<see cref="DefaultLog"/> source).<br/>
    /// Use the <see cref="ProcessLoggerExtensions"/> methods to include the position of an <see cref="Element"/> in the message.
    /// </summary>
    ILogger Log { get; }

    /// <summary>
    /// Gets a result logger (<see cref="ResultLog"/> source).
    /// </summary>
    ILogger Result { get; }

    /// <summary>
    /// Gets a root element.
    /// </summary>
    Group Root { get; }

    /// <summary>
    /// Marks the script as failed so processing stops.
    /// </summary>
    void Fatal();

    /// <summary>
    /// Logs a fatal message and marks the script as failed.
    /// </summary>
    /// <param name="element">The element which caused the error (<see langword="null"/> to omit the position).</param>
    /// <param name="message">The message.</param>
    void Fatal(Element? element, string message);

    /// <summary>
    /// Gets the path selected by <paramref name="pathType"/>.
    /// </summary>
    /// <param name="pathType">Specifies the path type.</param>
    /// <returns>A path.</returns>
    string GetPath(PathType pathType);

    /// <summary>
    /// Gets a file path which is combined with the path specified by <paramref name="pathType"/> and <paramref name="path"/>.
    /// If <paramref name="path"/> contains a root, <paramref name="path"/> is returned as is.
    /// </summary>
    /// <param name="pathType">Specifies the path type.</param>
    /// <param name="path">The target path.</param>
    /// <returns>A path.</returns>
    string CombinePath(PathType pathType, string path);
}

/// <summary>
/// Specifies the path type.
/// </summary>
public enum PathType
{
    /// <summary>
    /// The path of the current tinyhand file. If not specified in Process(), this value will be string.Empty.
    /// </summary>
    TinyhandFile,

    /// <summary>
    /// The root folder is the folder of a tinyhand file or an executable.
    /// </summary>
    RootFolder,

    /// <summary>
    /// The source folder.
    /// </summary>
    SourceFolder,

    /// <summary>
    /// The destination folder.
    /// </summary>
    DestinationFolder,
}
