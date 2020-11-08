// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Tinyhand;
using Tinyhand.Tree;

namespace TinyhandTest
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Tinyhand test.");
            Console.WriteLine();

            var projectPath = GetProjectPath();
            var testPath = Path.Combine(projectPath, "Test");

            // TestFile(Path.Combine(testPath, "group.tinyhand"));
            // TestFile(Path.Combine(testPath, "#.tinyhand"));
            // TestAll(testPath);

            // TestCompose(Path.Combine(testPath, "#.tinyhand"));

            // TestDeepCopy(Path.Combine(testPath, "simple.tinyhand"));

            TestProcess(Path.Combine(testPath, "process startup time.tinyhand"));
            // TestProcess(Path.Combine(testPath, "process language file.tinyhand"));
            // TestProcess(Path.Combine(testPath, "process log.tinyhand"));
        }

        private static void TestProcess(string fileName)
        {
            using var fs = new FileStream(fileName, FileMode.Open);
            /*var length = fs.Length;
            var buffer = new byte[length];
            fs.Read(buffer.AsSpan());*/

            var task = TinyhandProcess.Process(TinyhandParser.Parse(fs), fileName);
            task.Wait();
        }

        private static void TestDeepCopy(string fileName)
        {
            using var fs = new FileStream(fileName, FileMode.Open);
            var length = fs.Length;
            var buffer = new byte[length];
            fs.Read(buffer.AsSpan());

            var element = TinyhandParser.Parse(buffer, true);
            var element2 = (Element)element.DeepCopy();

            var b = TinyhandComposer.Compose(element);
            var b2 = TinyhandComposer.Compose(element2);
            Debug.Assert(b.SequenceEqual(b2), "not identical");

            Console.WriteLine(Encoding.UTF8.GetString(b2));
        }

        private static void TestAll(string folder)
        {
            var di = new DirectoryInfo(folder);
            if (di == null)
            {
                return;
            }

            foreach (var x in di.GetFiles("*.tinyhand"))
            {
                TestAll_File(x.FullName);
            }
        }

        private static void TestAll_File(string fileName)
        {
            var resultFile = Path.ChangeExtension(fileName, "txt");
            if (resultFile == null)
            {
                return;
            }

            using var fs = new FileStream(fileName, FileMode.Open);
            var length = fs.Length;
            var buffer = new byte[length];
            fs.Read(buffer.AsSpan());
        }

        private static string GetProjectPath()
        {
            var current = Directory.GetCurrentDirectory();

            var debugIndex = current.IndexOf("\\bin\\Debug");
            if (debugIndex >= 0)
            {
                return current.Substring(0, debugIndex);
            }

            var releaseIndex = current.IndexOf("\\bin\\Release");
            if (releaseIndex >= 0)
            {
                return current.Substring(0, releaseIndex);
            }

            throw new Exception("Fatal error, no project path.");
        }

        private static void TestFile(string fileName)
        {
            using var fs = new FileStream(fileName, FileMode.Open);
            var length = fs.Length;
            var buffer = new byte[length];
            fs.Read(buffer.AsSpan());

            var root = TinyhandParser.Parse(buffer, true);

            // Console.WriteLine(root.Dump());

            var b = TinyhandComposer.Compose(root, TinyhandComposeOption.UseContextualInformation);
            var st = Encoding.UTF8.GetString(b);

            Console.WriteLine("Copmpose:");
            Console.WriteLine(st);
        }

        private static void TestCompose(string fileName)
        {
            using var fs = new FileStream(fileName, FileMode.Open);
            var length = fs.Length;
            var buffer = new byte[length];
            fs.Read(buffer.AsSpan());

            var root = TinyhandParser.Parse(buffer, true);
            var b = TinyhandComposer.Compose(root, TinyhandComposeOption.UseContextualInformation);

            var composedFile = Path.Combine(Directory.GetCurrentDirectory(), Path.GetFileName(fileName));
            using var fs2 = new FileStream(composedFile, FileMode.Create);
            fs2.Write(b);

            Console.WriteLine("Copmpose:");
            var st = Encoding.UTF8.GetString(b);
            Console.WriteLine(st);
        }
    }
}
