using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using Tinyhand;

namespace Sandbox
{
    [TinyhandObject(KeyAsPropertyName = true)]
    public partial class MyClass
    {
        public ImmutableQueue<int> ImmutableQueueInt { get; set; }

        public IImmutableQueue<int> IImmutableQueueInt { get; set; }

        public MyClass()
        {
            this.ImmutableQueueInt = ImmutableQueue.Create<int>(new int[] { 0, 3, -44 });
            this.IImmutableQueueInt = this.ImmutableQueueInt;
        }
    }


    class Program
    {
        static void Main(string[] args)
        {
            var myClass = new MyClass();
            var b = TinyhandSerializer.Serialize(myClass);
            var myClass2 = TinyhandSerializer.Deserialize<MyClass>(b);
        }
    }
}
