// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Tinyhand.Tree;

#pragma warning disable CS1998
#pragma warning disable SA1009 // Closing parenthesis should be spaced correctly
#pragma warning disable SA1201 // Elements should appear in the correct order
#pragma warning disable SA1202 // Elements should be ordered by access
#pragma warning disable SA1204 // Static elements should appear before instance elements
#pragma warning disable SA1513 // Closing brace should be followed by blank line
#pragma warning disable SA1649 // File name should match first type name

namespace Tinyhand
{
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

    public class ProcessEnvironment : IProcessEnvironment
    {
        public const string PluginFolder = "plugins";

        public ProcessEnvironment(string? tinyhandFile)
        {
            this.LogInstance = new ConsoleLogger(this);
            this.ResultInstance = new NullLogger(this);
            this.ResultInstance.SetLogFormat(LogFormat.Message);
            this.ProcessCore = new Dictionary<string, TinyhandProcessCoreInfo>(); // Process name to TinyhandProcessCoreInfo
            this.currentCore = default!;
            this.TinyhandFile = tinyhandFile ?? string.Empty;
            this.Root = Group.Empty;
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
            this.AddProcessCoreInfo(TinyhandProcessCore_LanguageFile.StaticName, () => new TinyhandProcessCore_LanguageFile());
            this.AddProcessCoreInfo(TinyhandProcessCore_StartupTime.StaticName, () => new TinyhandProcessCore_StartupTime());

            // Add to identifierTable (identifier to Func<>).
            this.identifierTable.TryAdd(ProcessString, this.IdentifierTable_process);
            this.identifierTable.TryAdd(Encoding.UTF8.GetBytes("root"), this.IdentifierTable_root);
            this.identifierTable.TryAdd(Encoding.UTF8.GetBytes("source"), this.IdentifierTable_source);
            this.identifierTable.TryAdd(Encoding.UTF8.GetBytes("destination"), this.IdentifierTable_destination);
            this.identifierTable.TryAdd(Encoding.UTF8.GetBytes("log"), this.IdentifierTable_log);
            this.identifierTable.TryAdd(Encoding.UTF8.GetBytes("result"), this.IdentifierTable_result);

            this.LoadPlugin();
        }

        public static byte[] ModeIdentifier { get; } = Encoding.UTF8.GetBytes("mode");

        public static byte[] ProcessString { get; } = Encoding.UTF8.GetBytes("process");

        public bool IsProcessMode { get; private set; } = false;

        public Dictionary<string, TinyhandProcessCoreInfo> ProcessCore { get; }

        public Logger LogInstance { get; private set; }

        public Logger ResultInstance { get; private set; }

        public ILogger Log => this.LogInstance;

        public ILogger Result => this.ResultInstance;

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

        public string GetPath(PathType folderType) => folderType switch
        {
            PathType.TinyhandFile => this.TinyhandFile,
            PathType.RootFolder => this.RootFolder,
            PathType.SourceFolder => this.SourceFolder,
            PathType.DestinationFolder => this.DestinationFolder,
            _ => string.Empty
        };

        public string CombinePath(PathType pathType, string path)
        {
            if (Path.IsPathRooted(path))
            {
                return path;
            }

            return Path.Combine(this.GetPath(pathType), path);
        }

        public async Task<bool> Process(Element element)
        {
            var group = (Group)element;
            if (group == null)
            {
                return false;
            }

            this.Root = group;

            this.FatalStatus = false;
            this.currentCore = this.ProcessCore[TinyhandProcessCore_None.StaticName].GetInstance(null)!;
            foreach (var x in group)
            {
                if (this.FatalStatus)
                {
                    break;
                }

                if (x.TryGetLeft_IdentifierUtf8(out var identifier))
                { // identifier = "value"
                    if (identifier.SequenceEqual(ModeIdentifier))
                    { // "mode"
                        if (x.TryGetRight_Value_String(out var valueString) && valueString.ValueStringUtf8.SequenceEqual(ProcessString))
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
            this.LogInstance.Dispose();
            this.ResultInstance.Dispose();
            this.Root = Group.Empty;
            return true;
        }

        private Utf8Hashtable<Action<Element>> identifierTable = new Utf8Hashtable<Action<Element>>();

        private IProcessCore currentCore;

        private void IdentifierTable_process(Element element)
        { // "process"
            if (element.TryGetRight_Value_String(out var valueString))
            { // Get ProcessCore.
                if (this.ProcessCore.TryGetValue(valueString.ValueStringUtf16, out var info))
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
                    this.Log.Fatal(valueString, $"Process name \"{valueString.ValueStringUtf16}\" is unknown.");
                }
            }
        }

        private void IdentifierTable_root(Element element)
        { // "root"
            if (element.TryGetRight_Value_String(out var valueString))
            {
                if (Path.IsPathRooted(valueString.ValueStringUtf16))
                {
                    this.RootFolder = valueString.ValueStringUtf16;
                }
                else
                {
                    this.Log.Error(element, "root must be a rooted (absolute) path.");
                }
            }
        }

        private void IdentifierTable_source(Element element)
        { // "source"
            if (element.TryGetRight_Value_String(out var valueString))
            {
                if (Path.IsPathRooted(valueString.ValueStringUtf16))
                {
                    this.SourceFolder = valueString.ValueStringUtf16;
                }
                else
                {
                    this.SourceFolder = Path.Combine(this.RootFolder, valueString.ValueStringUtf16);
                }
            }
        }

        private void IdentifierTable_destination(Element element)
        { // "destination"
            if (element.TryGetRight_Value_String(out var valueString))
            {
                if (Path.IsPathRooted(valueString.ValueStringUtf16))
                {
                    this.DestinationFolder = valueString.ValueStringUtf16;
                }
                else
                {
                    this.DestinationFolder = Path.Combine(this.RootFolder, valueString.ValueStringUtf16);
                }
            }
        }

        private void IdentifierTable_log(Element element)
        {// "log"
            var logger = this.LogInstance;
            this.IdentifierTable_set_logger(element, ref logger, ".log", LogFormat.Log);
            if (this.LogInstance != logger)
            {
                Console.WriteLine($"Switched from {this.LogInstance.GetLoggerInformation()} to {logger.GetLoggerInformation()}");
                this.LogInstance.Dispose();
                this.LogInstance = logger;
            }
        }

        private void IdentifierTable_result(Element element)
        {// "result"
            var logger = this.ResultInstance;
            this.IdentifierTable_set_logger(element, ref logger, ".txt", LogFormat.Message);
            if (this.ResultInstance != logger)
            {
                this.ResultInstance.Dispose();
                this.ResultInstance = logger;
            }
        }

        private void IdentifierTable_set_logger(Element element, ref Logger logger, string defaultExtension, LogFormat format)
        { // Set logger.
            Value_String? stringValue;
            if (!element.TryGetRightGroup_Value_String(null, out stringValue))
            {
                return;
            }

            Logger? newlogger = null;

            if (stringValue.ValueStringUtf16 == "console")
            { // Console logger
                newlogger = new ConsoleLogger(this);
            }
            else if (stringValue.ValueStringUtf16 == "file")
            { // File logger
                string path = string.Empty;
                bool consoleFlag = false;

                if (element.TryGetRightGroup_Value_String("path", out stringValue))
                {
                    path = stringValue.ValueStringUtf16;
                    if (!Path.IsPathRooted(path))
                    {
                        path = Path.Combine(this.GetPath(PathType.RootFolder), path);
                    }

                    if (!Path.HasExtension(path))
                    {
                        path = path + defaultExtension;
                    }
                }

                if (element.TryGetRightGroup_Value("console", out var consoleValue))
                {
                    consoleFlag = consoleValue.IsTrue();
                }

                if (path == string.Empty)
                {
                    var tinyhandFile = this.GetPath(PathType.TinyhandFile);
                    if (tinyhandFile != null)
                    {
                        path = Path.ChangeExtension(tinyhandFile, defaultExtension);
                    }
                }

                if (path == null || path == string.Empty)
                {
                    path = Path.Combine(this.GetPath(PathType.RootFolder), "process" + defaultExtension);
                }

                if (path == null || path == string.Empty)
                {
                    this.Log.Error(stringValue, "File logger could not get the file path.");
                    return;
                }

                try
                {
                    newlogger = new FileLogger(this, path, console: consoleFlag);
                }
                catch
                {
                    this.Log.Error(stringValue, $"File logger could not open the file ({path}).");
                }
            }
            else if (stringValue.ValueStringUtf16 == string.Empty)
            { // Null logger.
                newlogger = new NullLogger(this);
            }
            else
            {
                this.Log.Error(stringValue, $"Logger type \"{stringValue.ValueStringUtf16}\" is not registered.");
            }

            if (newlogger != null)
            {
                if (element.TryGetRightGroup_Value_String("format", out var formatValue))
                {
                    if (Enum.TryParse<LogFormat>(formatValue.ValueStringUtf16, out var f))
                    {
                        format = f;
                    }
                }

                newlogger.SetLogFormat(format);
                logger = newlogger;
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
            var environment = new ProcessEnvironment(tinyhandFile);

            return await environment.Process(element);
        }
    }
}
