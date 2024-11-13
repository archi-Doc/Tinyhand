// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Tinyhand;
using Tinyhand.Tree;

namespace TinyhandProcessor;

public class Program
{
    public static async Task Main(string[] args)
    {
        var title = "Tinyhand Processor " + typeof(TinyhandHelper).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        Console.WriteLine(title);
        Console.WriteLine();

        if (args.Length == 0 || args[0].ToLower() == "--help")
        {
            var usage = "usage: tinyhand [FILE]";
            Console.WriteLine(usage);
        }
        else
        {
            await Process(args[0]);
        }

        Console.WriteLine();
        Console.WriteLine("Press any key to exit.");
        Console.ReadKey();
    }

    public static async Task<bool> Process(string file)
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var result = true;

        if (!Path.IsPathRooted(file))
        {
            file = Path.Combine(currentDirectory, file);
        }

        byte[] buffer;
        try
        {
            using var fs = new FileStream(file, FileMode.Open);
            var length = fs.Length;
            buffer = new byte[length];
            fs.ReadExactly(buffer.AsSpan());
        }
        catch
        {
            Console.WriteLine("Could not open: " + file);
            return false;
        }

        Element root;
        try
        {
            Console.WriteLine("Process file: " + file);
            Console.WriteLine();
            root = TinyhandParser.Parse(buffer);
        }
        catch (TinyhandException e)
        {
            Console.WriteLine("Parse error:");
            Console.WriteLine(e.Message);
            return false;
        }

        result = await TinyhandProcess.Process(root, file);

        return result;
    }
}
