// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Threading.Tasks;
using Tinyhand.Tree;

#pragma warning disable SA1649 // File name should match first type name

namespace Tinyhand
{
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
        /// <param name="environment">IProcessEnvironment.</param>
        void Initialize(IProcessEnvironment environment);

        /// <summary>
        /// Called when the process is complete and changed to another process core.
        /// </summary>
        void Uninitialize();

        /// <summary>
        /// Process elements.
        /// </summary>
        /// <param name="element">Element.</param>
        /// <returns>Returns true on success.</returns>
        Task<bool> Process(Element element);
    }

    public interface IProcessEnvironment
    {
        /// <summary>
        /// Gets a default logger.
        /// </summary>
        ILogger Log { get; }

        /// <summary>
        /// Gets a result logger.
        /// </summary>
        ILogger Result { get; }

        /// <summary>
        /// Gets a root element.
        /// </summary>
        Group Root { get; }

        /// <summary>
        /// Notify that a fatal error has occured and quit the process.
        /// </summary>
        void Fatal();

        /// <summary>
        /// Gets a file path which specified by <paramref name="pathType"/>.
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

    public interface ILogger
    {
        void Log(LogLevel level, Element? element, string message);

        void Debug(Element? element, string message) => this.Log(LogLevel.Debug, element, message);

        void Information(Element? element, string message) => this.Log(LogLevel.Information, element, message);

        void Warning(Element? element, string message) => this.Log(LogLevel.Warning, element, message);

        void Error(Element? element, string message) => this.Log(LogLevel.Error, element, message);

        void Fatal(Element? element, string message) => this.Log(LogLevel.Fatal, element, message);
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

    /// <summary>
    /// Specifies the meaning and relative importance of a log event.
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
}
