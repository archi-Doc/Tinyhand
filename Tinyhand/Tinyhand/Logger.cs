// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Tinyhand.Tree;

#pragma warning disable SA1204 // Static elements should appear before instance elements
#pragma warning disable SA1401 // Fields should be private

namespace Tinyhand.Logging
{
    public abstract class Logger : ILogger, IDisposable
    {
        protected bool disposed = false; // To detect redundant calls.
        protected IProcessEnvironment environment;
        protected LogFormat format;

        public Logger(IProcessEnvironment environment)
        {
            this.environment = environment;
            this.format = LogFormat.Log;
        }

        public virtual void Log(LogLevel level, Element? element, string message)
        {
            if (level == LogLevel.Fatal)
            {
                this.environment.Fatal();
            }
        }

        public virtual string GetMessage(LogLevel level, Element? element, string message)
        {
            if (this.format == LogFormat.Log)
            {
                if (element != null)
                {
                    message = $"[{level.ToShortString()}] " + message + $" (Line:{element.LineNumber} BytePosition:{element.BytePositionInLine})";
                }
                else
                {
                    message = $"[{level.ToShortString()}] " + message;
                }
            }

            return message;
        }

        public void SetLogFormat(LogFormat format)
        {
            this.format = format;
        }

        public virtual string GetLoggerInformation() => "Logger";

        /// <summary>
        /// Finalizes an instance of the <see cref="Logger"/> class.
        /// </summary>
        ~Logger()
        {
            this.Dispose(false);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// free managed/native resources.
        /// </summary>
        /// <param name="disposing">true: free managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    // free managed resources.
                }

                // free native resources here if there are any.
                this.disposed = true;
            }
        }
    }

    public class NullLogger : Logger
    {
        public NullLogger(IProcessEnvironment environment)
            : base(environment)
        {
        }

        public override string GetLoggerInformation() => "Null Logger";
    }

    public class ConsoleLogger : Logger
    {
        public ConsoleLogger(IProcessEnvironment environment)
            : base(environment)
        {
        }

        public override void Log(LogLevel level, Element? element, string message)
        {
            Console.WriteLine(this.GetMessage(level, element, message));
            if (level == LogLevel.Fatal)
            {
                this.environment.Fatal();
            }
        }

        public override string GetLoggerInformation() => "Console Logger";
    }

    public class FileLogger : Logger
    {
        private string path;
        private bool console;

        public FileLogger(IProcessEnvironment environment, string path, bool append = false, bool console = false)
            : base(environment)
        {
            this.path = path;
            this.streamWriter = new StreamWriter(path, append, Encoding.UTF8);
            this.console = console;
        }

        private StreamWriter streamWriter;

        public override void Log(LogLevel level, Element? element, string message)
        {
            var s = this.GetMessage(level, element, message);
            this.streamWriter.WriteLine(s);
            if (this.console)
            {
                Console.WriteLine(s);
            }

            if (level == LogLevel.Fatal)
            {
                this.environment.Fatal();
            }
        }

        public override string GetLoggerInformation() => $"File Logger ({this.path})";

        protected override void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    // free managed resources.
                    this.streamWriter.Dispose();
                }

                // free native resources here if there are any.
                this.disposed = true;
            }
        }
    }

    public static class LoggerHelper
    {
        public static string ToShortString(this LogLevel level) => level switch
        {
            LogLevel.Fatal => "Fatal",
            LogLevel.Error => "Error",
            LogLevel.Warning => "Warn ",
            LogLevel.Information => "Info ",
            LogLevel.Debug => "Debug",
            _ => string.Empty
        };
    }
}
