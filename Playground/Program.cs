using System;
using Arc.Unit;
using Tinyhand.IO;

namespace Playground;

internal class Program
{
    static void Main(string[] args)
    {
        using (var writer = new TinyhandWriter(ByteArrayPool.Default.Rent(10)))
        {
            writer.WriteInt8(123);
            var array = writer.FlushAndGetArray();
            var memoryOwner = writer.FlushAndGetMemoryOwner();
            memoryOwner.Return();
        }

        Console.WriteLine("Hello, World!");
    }
}
