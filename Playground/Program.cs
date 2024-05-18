using System;

namespace Playground;

public class IntegralityEngine
{
    public IntegralityEngine()
    {
    }

    public int Level { get; private set; }

    public void GetIdentity(IIntegrality integrality)
    {
        if (this.Level == 0)
        {// Get root hash
            var rootHash = integrality.GetRootHash();
        }
    }
}

public class IntegralityContext
{
}

public class Test1
{
    public void Test()
    {
        var engine = new IntegralityEngine();
        /*var identity = engine.GetIdentity(g);
        var difference = engine.GetDifference();
        engine.Integrate(difference);*/
    }
}

public interface IIntegrality
{// Exaltation of the Integrality by Baxter.
    ulong RootHash { get; set; }

    ulong GetRootHash();

    void ClearRootHash();
}

internal class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
    }
}
