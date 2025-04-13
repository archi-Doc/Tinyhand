// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Tinyhand.Tree;

namespace Tinyhand;

public class TinyhandProcessCore_StartupTime : IProcessCore
{
    public static string StaticName => "startup time";

    public TinyhandProcessCore_StartupTime()
    {
        this.stopWatch = new Stopwatch();
    }

    public IProcessEnvironment Environment { get; private set; } = default!;

    public string ProcessName => StaticName;

    public int WarmupTime => 1000;

    public int CooldownTime => 2000;

    public int RepeatMax => 100;

    public void Initialize(IProcessEnvironment environment)
    {
        this.Environment = environment;
    }

    public void Uninitialize()
    {
    }

    public async Task<bool> Process(Element element)
    {
        if (element.TryGetRight_Value_Long("repeat", out var valueLong))
        {
            if (valueLong.ValueLong < 0 || valueLong.ValueLong > this.RepeatMax)
            {
                this.Environment.Log.Warning(element, $"The number of repetition should be in the range from 1 to {this.RepeatMax}.");
            }
            else
            {
                this.repeat = (int)valueLong.ValueLong;
            }
        }

        if (element is Value_String valueString)
        {
            var path = this.Environment.CombinePath(PathType.SourceFolder, valueString.Utf16);
            var logpath = valueString.Utf16;
            if (!File.Exists(path))
            {
                this.Environment.Log.Error(element, $"\"{path}\" does not exists.");
                return false;
            }

            var result = new double[this.repeat];
            for (var n = 0; n < this.repeat; n++)
            {
                await Task.Delay(this.WarmupTime);

                foreach (var checkProcess in System.Diagnostics.Process.GetProcessesByName(Path.GetFileNameWithoutExtension(path)))
                {
                    if (string.Compare(checkProcess.MainModule?.FileName, path, true) == 0)
                    {
                        this.Environment.Log.Error(element, $"The executable file \"{path}\" is already running.");
                        return false;
                    }
                }

                // Start the executable file.
                this.Environment.Log.Information(element, $"Starts the executable (\"{logpath}\").");
                var process = System.Diagnostics.Process.Start(path);

                // Wait for input idle.
                this.stopWatch.Restart();
                process.WaitForInputIdle();
                var elapsed = (double)this.stopWatch.ElapsedTicks / (double)Stopwatch.Frequency;
                this.Environment.Log.Information(null, $"Startup time is {elapsed.ToString("F2")} seconds.");
                result[n] = elapsed;

                // Cooldown and shutdown the process.
                await Task.Delay(this.CooldownTime);
                process.CloseMainWindow();
                process.Close();
            }

            if (this.repeat > 1)
            { // Mean and Count.
                this.Environment.Log.Information(null, $"Mean: {Enumerable.Average(result).ToString("F2")} seconds. Count: {this.repeat}");
            }
        }

        return true;
    }

    private Stopwatch stopWatch;

    private int repeat = 1;
}
