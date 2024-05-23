using System;
using Arc.Collections;
using Tinyhand.IO;

namespace Playground;

internal class Program
{
    static void Main(string[] args)
    {
        using (var writer = new TinyhandWriter(BytePool.Default.Rent(10)))
        {
            writer.WriteInt8(123);
            var array = writer.FlushAndGetArray();
            var memoryOwner = writer.FlushAndGetRentMemory();
            memoryOwner.Return();
        }

        Console.WriteLine("Hello, World!");
    }
}
