// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Arc.Collections;
using Arc.Unit;
using Microsoft.Extensions.DependencyInjection;
using Tinyhand.Tree;

#pragma warning disable CS1998
#pragma warning disable SA1009 // Closing parenthesis should be spaced correctly
#pragma warning disable SA1201 // Elements should appear in the correct order
#pragma warning disable SA1202 // Elements should be ordered by access
#pragma warning disable SA1204 // Static elements should appear before instance elements
#pragma warning disable SA1513 // Closing brace should be followed by blank line

namespace Tinyhand;

public class TinyhandProcessCoreInfo
{
    public TinyhandProcessCoreInfo(ProcessEnvironment environment, string processName, Func<IProcessCore> factory)
    {
        this.Environment = environment;
        this.ProcessName = processName;
        this.Factory = factory;
    }

    public TinyhandProcessCoreInfo(ProcessEnvironment environment, string processName, string pluginPath, string className)
    {
        this.Environment = environment;
        this.ProcessName = processName;
        this.PluginPath = pluginPath;
        this.ClassName = className;
    }

    public ProcessEnvironment Environment { get; }

    public string ProcessName { get; }

    private IProcessCore? instance;

    public IProcessCore? GetInstance(Element? element)
    {
        if (this.instance == null)
        {
            try
            {
                if (this.Factory != null)
                {
                    this.instance = this.Factory();
                }
                else if (this.PluginPath != null)
                {
                    var asm = System.Reflection.Assembly.LoadFrom(this.PluginPath);
                    var obj = asm?.CreateInstance(this.ClassName!);
                    this.instance = obj as IProcessCore;
                    if (this.instance == null)
                    {
                        this.Environment.Log.Error(element, $"Plugin {Path.GetFileName(this.PluginPath)} - {this.ClassName}: Could not create an instance.");
                    }
                }
                else
                {
                    throw new Exception();
                }
            }
            catch
            {
                this.Environment.Log.Error(element, $"Process name \"{this.ProcessName}\": Could not create an instance.");
            }
        }

        return this.instance;
    }

    public Func<IProcessCore>? Factory { get; }

    public string? PluginPath { get; }

    public string? ClassName { get; }
}

public class TinyhandProcessCore_None : IProcessCore
{
    public static string StaticName => "none";

    public string ProcessName => StaticName;

    public void Initialize(IProcessEnvironment environment)
    {
    }

    public void Uninitialize()
    {
    }

    public async Task<bool> Process(Element element)
    {
        return true;
    }
}

/// <summary>
/// The file logger options of <see cref="IProcessEnvironment.Result"/>.<br/>
/// A distinct options type is required so that the result file logger can use a path and a format
/// of its own, independent of <see cref="FileLoggerOptions"/> used by <see cref="IProcessEnvironment.Log"/>.
/// </summary>
public record ResultFileLoggerOptions : FileLoggerOptions
{
}

/// <summary>
/// Writes to both the console and the result file (<see cref="ConsoleAndFileLogger"/> for <see cref="ResultFileLoggerOptions"/>).
/// </summary>
public class ConsoleAndResultFileLogger : ILogOutput
{
    private readonly ConsoleLogger consoleLogger;
    private readonly FileLogger<ResultFileLoggerOptions> fileLogger;

    public ConsoleAndResultFileLogger(ConsoleLogger consoleLogger, FileLogger<ResultFileLoggerOptions> fileLogger)
    {
        this.consoleLogger = consoleLogger;
        this.fileLogger = fileLogger;
    }

    public void Output(LogEvent logEvent)
    {
        this.consoleLogger.Output(logEvent);
        this.fileLogger.Output(logEvent);
    }
}

public class ProcessEnvironment : IProcessEnvironment, IDisposable
{
    public const string PluginFolder = "plugins";

    public ProcessEnvironment(Group root, string? tinyhandFile)
    {
        this.ProcessCore = new Dictionary<string, TinyhandProcessCoreInfo>(); // Process name to TinyhandProcessCoreInfo
        this.currentCore = default!;
        this.Root = Group.Empty;
        this.rootGroup = root;
        if (tinyhandFile != null)
        {
            this.TinyhandFile = tinyhandFile;
            this.RootFolder = Path.GetDirectoryName(tinyhandFile) ?? Directory.GetCurrentDirectory();
        }
        else
        {
            this.TinyhandFile = string.Empty;
            this.RootFolder = Directory.GetCurrentDirectory();
        }
        this.SourceFolder = this.RootFolder;
        this.DestinationFolder = this.RootFolder;

        // Add to this.ProcessCore.
        this.AddProcessCoreInfo(TinyhandProcessCore_None.StaticName, () => new TinyhandProcessCore_None());
        this.AddProcessCoreInfo(TinyhandProcessCore_Example.StaticName, () => new TinyhandProcessCore_Example());
        this.AddProcessCoreInfo(TinyhandProcessCore_LanguageFile.StaticName, () => new TinyhandProcessCore_LanguageFile());
        this.AddProcessCoreInfo(TinyhandProcessCore_StartupTime.StaticName, () => new TinyhandProcessCore_StartupTime());
        this.AddProcessCoreInfo(TinyhandProcessCore_TextToTinyhand.StaticName, () => new TinyhandProcessCore_TextToTinyhand());

        // Add to identifierTable (identifier to Func<>).
        this.identifierTable.TryAdd(ProcessString, this.IdentifierTable_process);
        this.identifierTable.TryAdd(RootIdentifier, this.IdentifierTable_root);
        this.identifierTable.TryAdd(SourceIdentifier, this.IdentifierTable_source);
        this.identifierTable.TryAdd(DestinationIdentifier, this.IdentifierTable_destination);
        this.identifierTable.TryAdd(LogIdentifier, this.IdentifierTable_logger);
        this.identifierTable.TryAdd(ResultIdentifier, this.IdentifierTable_logger);

        // The log outputs of Arc.Unit are resolved when the unit is built, so the folder and the logger
        // directives are read in advance. Messages produced while reading them are queued and
        // written as soon as the loggers become available.
        this.PreConfigure();

        this.product = this.BuildUnit();
        var logService = this.product.Context.ServiceProvider.GetRequiredService<ILogService>();
        this.Log = logService.GetLogger<DefaultLog>();
        this.Result = logService.GetLogger<ResultLog>();
        foreach (var (level, element, message) in this.pendingMessages)
        {
            this.Log.GetWriter(level)?.Write(ProcessLoggerExtensions.AddPosition(element, message));
        }

        this.pendingMessages.Clear();

        this.LoadPlugin();
    }

    public static byte[] ModeIdentifier { get; } = Encoding.UTF8.GetBytes("mode");

    public static byte[] ProcessString { get; } = Encoding.UTF8.GetBytes("process");

    private static byte[] RootIdentifier { get; } = Encoding.UTF8.GetBytes("root");

    private static byte[] SourceIdentifier { get; } = Encoding.UTF8.GetBytes("source");

    private static byte[] DestinationIdentifier { get; } = Encoding.UTF8.GetBytes("destination");

    private static byte[] LogIdentifier { get; } = Encoding.UTF8.GetBytes("log");

    private static byte[] ResultIdentifier { get; } = Encoding.UTF8.GetBytes("result");

    public bool IsProcessMode { get; private set; } = false;

    public Dictionary<string, TinyhandProcessCoreInfo> ProcessCore { get; }

    public ILogger Log { get; }

    public ILogger Result { get; }

    public Group Root { get; private set; }

    public string TinyhandFile { get; private set; } = string.Empty;

    public string RootFolder { get; private set; } = string.Empty;

    public string SourceFolder { get; private set; } = string.Empty;

    public string DestinationFolder { get; private set; } = string.Empty;

    public bool FatalStatus { get; private set; }

    public void Fatal()
    {
        this.FatalStatus = true;
    }

    public void Fatal(Element? element, string message)
    {
        this.Log.Fatal(element, message);
        this.FatalStatus = true;
    }

    public string GetPath(PathType folderType) => folderType switch
    {
        PathType.TinyhandFile => this.TinyhandFile,
        PathType.RootFolder => this.RootFolder,
        PathType.SourceFolder => this.SourceFolder,
        PathType.DestinationFolder => this.DestinationFolder,
        _ => string.Empty,
    };

    public string CombinePath(PathType pathType, string path)
    {
        if (Path.IsPathRooted(path))
        {
            return path;
        }

        return Path.Combine(this.GetPath(pathType), path);
    }

    public async Task<bool> Process()
    {
        this.Root = this.rootGroup;

        this.FatalStatus = false;
        this.IsProcessMode = false;
        this.currentCore = this.ProcessCore[TinyhandProcessCore_None.StaticName].GetInstance(null)!;
        foreach (var x in this.rootGroup)
        {
            if (this.FatalStatus)
            {
                break;
            }

            if (x.TryGetLeft_IdentifierUtf8(out var identifier))
            { // identifier = "value"
                if (identifier.SequenceEqual(ModeIdentifier))
                { // "mode"
                    if (x.TryGetRight_Value_String(out var valueString) && valueString.Utf8.SequenceEqual(ProcessString))
                    { // "process"
                        this.IsProcessMode = true;
                    }
                    else
                    { // other
                        this.IsProcessMode = false;
                    }

                    continue;
                }

                if (this.IsProcessMode)
                { // Process mode
                    if (this.identifierTable.TryGetValue(identifier, out var action))
                    {
                        action(x);
                    }
                    else
                    { // other
                        await this.currentCore.Process(x);
                    }
                }
            }
            else if (this.IsProcessMode)
            {
                await this.currentCore.Process(x);
            }
        }

        this.currentCore.Uninitialize();

        if (!this.FatalStatus)
        {
            this.Log.Information(null, "Done.");
        }
        else
        {
            this.Log.Fatal(null, "Aborted.");
        }

        // Clear
        this.Root = Group.Empty;
        await this.product.Context.ServiceProvider.GetRequiredService<LogUnit>().Flush().ConfigureAwait(false);
        return true;
    }

    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;
        (this.product.Context.ServiceProvider as IDisposable)?.Dispose();
    }

    private readonly Group rootGroup;
    private readonly UnitProduct product;
    private readonly List<(LogLevel Level, Element? Element, string Message)> pendingMessages = new();
    private readonly LoggerSettings logSettings = new(ProcessLogOutput.Console, ProcessLogFormat.Log, ".log");
    private readonly LoggerSettings resultSettings = new(ProcessLogOutput.None, ProcessLogFormat.Message, ".txt");
    private bool disposed;

    private Utf8Hashtable<Action<Element>> identifierTable = new Utf8Hashtable<Action<Element>>();

    private IProcessCore currentCore;

    private sealed class LoggerSettings
    {
        public LoggerSettings(ProcessLogOutput output, ProcessLogFormat format, string defaultExtension)
        {
            this.Output = output;
            this.Format = format;
            this.DefaultExtension = defaultExtension;
        }

        public ProcessLogOutput Output { get; set; }

        public ProcessLogFormat Format { get; set; }

        public string DefaultExtension { get; }

        public string Path { get; set; } = string.Empty;
    }

    #region Configuration

    /// <summary>
    /// Reads the folder and the logger directives before the unit (and therefore the log outputs) is built.
    /// </summary>
    private void PreConfigure()
    {
        var isProcessMode = false;
        foreach (var x in this.rootGroup)
        {
            if (!x.TryGetLeft_IdentifierUtf8(out var identifier))
            {
                continue;
            }

            if (identifier.SequenceEqual(ModeIdentifier))
            {
                isProcessMode = x.TryGetRight_Value_String(out var valueString) && valueString.Utf8.SequenceEqual(ProcessString);
                continue;
            }

            if (!isProcessMode)
            {
                continue;
            }

            if (identifier.SequenceEqual(RootIdentifier))
            {
                this.IdentifierTable_root(x);
            }
            else if (identifier.SequenceEqual(SourceIdentifier))
            {
                this.IdentifierTable_source(x);
            }
            else if (identifier.SequenceEqual(DestinationIdentifier))
            {
                this.IdentifierTable_destination(x);
            }
            else if (identifier.SequenceEqual(LogIdentifier))
            {
                this.ReadLoggerSettings(x, this.logSettings);
            }
            else if (identifier.SequenceEqual(ResultIdentifier))
            {
                this.ReadLoggerSettings(x, this.resultSettings);
            }
        }
    }

    private UnitProduct BuildUnit()
    {
        var builder = new UnitBuilder()
            .PreConfigure(context =>
            {
                context.SetOptions(new FileLoggerOptions
                {
                    Path = this.logSettings.Path,
                    ClearLogsAtStartup = true,
                    FormatterOptions = CreateFormatterOptions(this.logSettings.Format, enableColor: false),
                });

                context.SetOptions(new ResultFileLoggerOptions
                {
                    Path = this.resultSettings.Path,
                    ClearLogsAtStartup = true,
                    FormatterOptions = CreateFormatterOptions(this.resultSettings.Format, enableColor: false),
                });

                context.SetOptions(new ConsoleLoggerOptions
                {
                    FormatterOptions = CreateFormatterOptions(this.logSettings.Format, enableColor: true),
                });
            })
            .Configure(context =>
            {
                context.AddSingleton<FileLogger<ResultFileLoggerOptions>>();
                context.AddSingleton<ConsoleAndResultFileLogger>();
                context.AddLoggerResolver(x =>
                {
                    if (x.LogSourceType == typeof(ResultLog))
                    {
                        SetOutput(x, this.resultSettings.Output, result: true);
                    }
                    else
                    {
                        SetOutput(x, this.logSettings.Output, result: false);
                    }
                });
            });

        return builder.Build();
    }

    private static void SetOutput(LoggerResolverContext context, ProcessLogOutput output, bool result)
    {
        switch (output)
        {
            case ProcessLogOutput.Console:
                context.SetOutput<ConsoleLogger>();
                break;

            case ProcessLogOutput.File:
                if (result)
                {
                    context.SetOutput<FileLogger<ResultFileLoggerOptions>>();
                }
                else
                {
                    context.SetOutput<FileLogger<FileLoggerOptions>>();
                }

                break;

            case ProcessLogOutput.ConsoleAndFile:
                if (result)
                {
                    context.SetOutput<ConsoleAndResultFileLogger>();
                }
                else
                {
                    context.SetOutput<ConsoleAndFileLogger>();
                }

                break;

            default:
                context.SetOutput<EmptyLogger>();
                break;
        }
    }

    private static SimpleLogFormatterOptions CreateFormatterOptions(ProcessLogFormat format, bool enableColor)
        => format == ProcessLogFormat.Message ?
            new SimpleLogFormatterOptions(enableColor) { TimestampFormat = null, } : // Omit the timestamp.
            new SimpleLogFormatterOptions(enableColor);

    #endregion

    private void QueueMessage(LogLevel level, Element? element, string message)
        => this.pendingMessages.Add((level, element, message));

    private void IdentifierTable_process(Element element)
    { // "process"
        if (element.TryGetRight_Value_String(out var valueString))
        { // Get ProcessCore.
            if (this.ProcessCore.TryGetValue(valueString.Utf16, out var info))
            { // Get an instance.
                var instance = info.GetInstance(element);
                if (instance != null)
                { // Change currentCore.
                    this.currentCore.Uninitialize();
                    this.currentCore = instance;
                    this.currentCore.Initialize(this);
                }
            }
            else
            { // Cannot find matched ProcessCore.
                this.Fatal(valueString, $"Process name \"{valueString.Utf16}\" is unknown.");
            }
        }
    }

    private void IdentifierTable_root(Element element)
    { // "root"
        if (element.TryGetRight_Value_String(out var valueString))
        {
            if (Path.IsPathRooted(valueString.Utf16))
            {
                this.RootFolder = valueString.Utf16;
            }
            else
            {
                this.QueueMessage(LogLevel.Error, element, "root must be a rooted (absolute) path.");
            }
        }
    }

    private void IdentifierTable_source(Element element)
    { // "source"
        if (element.TryGetRight_Value_String(out var valueString))
        {
            if (Path.IsPathRooted(valueString.Utf16))
            {
                this.SourceFolder = valueString.Utf16;
            }
            else
            {
                this.SourceFolder = Path.Combine(this.RootFolder, valueString.Utf16);
            }
        }
    }

    private void IdentifierTable_destination(Element element)
    { // "destination"
        if (element.TryGetRight_Value_String(out var valueString))
        {
            if (Path.IsPathRooted(valueString.Utf16))
            {
                this.DestinationFolder = valueString.Utf16;
            }
            else
            {
                this.DestinationFolder = Path.Combine(this.RootFolder, valueString.Utf16);
            }
        }
    }

    private void IdentifierTable_logger(Element element)
    {// "log" and "result": already applied by PreConfigure().
    }

    private void ReadLoggerSettings(Element element, LoggerSettings settings)
    { // Read the logger settings.
        Value_String? stringValue;
        if (!element.TryGetRightGroup_Value_String(null, out stringValue))
        {
            return;
        }

        if (stringValue.Utf16 == "console")
        { // Console logger
            settings.Output = ProcessLogOutput.Console;
        }
        else if (stringValue.Utf16 == "file")
        { // File logger
            string path = string.Empty;
            var consoleFlag = false;

            if (element.TryGetRightGroup_Value_String("path", out var pathValue))
            {
                path = pathValue.Utf16;
                if (!Path.IsPathRooted(path))
                {
                    path = Path.Combine(this.GetPath(PathType.RootFolder), path);
                }

                if (!Path.HasExtension(path))
                {
                    path = path + settings.DefaultExtension;
                }
            }

            if (element.TryGetRightGroup_Value("console", out var consoleValue))
            {
                consoleFlag = consoleValue.IsTrue();
            }

            if (path == string.Empty)
            {
                var tinyhandFile = this.GetPath(PathType.TinyhandFile);
                if (!string.IsNullOrEmpty(tinyhandFile))
                {
                    path = Path.ChangeExtension(tinyhandFile, settings.DefaultExtension);
                }
            }

            if (string.IsNullOrEmpty(path))
            {
                path = Path.Combine(this.GetPath(PathType.RootFolder), "process" + settings.DefaultExtension);
            }

            settings.Path = path;
            settings.Output = consoleFlag ? ProcessLogOutput.ConsoleAndFile : ProcessLogOutput.File;
        }
        else if (stringValue.Utf16 == string.Empty)
        { // No output.
            settings.Output = ProcessLogOutput.None;
        }
        else
        {
            this.QueueMessage(LogLevel.Error, stringValue, $"Logger type \"{stringValue.Utf16}\" is not registered.");
            return;
        }

        if (element.TryGetRightGroup_Value_String("format", out var formatValue))
        {
            if (Enum.TryParse<ProcessLogFormat>(formatValue.Utf16, out var f))
            {
                settings.Format = f;
            }
        }
    }

    private void AddProcessCoreInfo(string processName, Func<IProcessCore> factory)
    {
        this.ProcessCore.TryAdd(processName, new TinyhandProcessCoreInfo(this, processName, factory));
    }

    private void AddProcessCoreInfo(string processName, string pluginPath, string className)
    {
        this.ProcessCore.TryAdd(processName, new TinyhandProcessCoreInfo(this, processName, pluginPath, className));
    }

    private void LoadPlugin()
    { // Load plugins.
        var folder = Path.Combine(Directory.GetCurrentDirectory(), PluginFolder);
        if (!Directory.Exists(folder))
        {
            return;
        }

        try
        {
            foreach (var x in Directory.GetFiles(folder, "*.dll"))
            {
                var asm = System.Reflection.Assembly.LoadFrom(x);
                foreach (Type t in asm.GetTypes())
                {
                    if (t.IsClass && t.IsPublic && !t.IsAbstract && t.GetInterface(typeof(IProcessCore).FullName!) != null)
                    {
                        var staticNameProperty = t.GetProperty("StaticName", BindingFlags.Public | BindingFlags.Static);
                        var name = staticNameProperty?.GetValue(null);
                        if (name is string processName)
                        {
                            this.AddProcessCoreInfo(processName, x, t.FullName!);
                        }
                        else
                        {
                            this.Log.Error(null, $"Plugin {Path.GetFileName(x)} - {t.FullName}: Could not find StaticName property.");
                        }
                    }
                }
            }
        }
        catch
        {
        }
    }
}

public static class TinyhandProcess
{
    public static async Task<bool> Process(Element element, string? tinyhandFile)
    {
        if (element is not Group group)
        {
            return false;
        }

        using var environment = new ProcessEnvironment(group, tinyhandFile);
        return await environment.Process();
    }
}
