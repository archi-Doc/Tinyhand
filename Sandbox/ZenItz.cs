using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Tinyhand;
using Tinyhand.IO;

namespace Sandbox.ZenItz;

public interface IPayload
{
}

[TinyhandObject]
public partial class InitializationPayload : IPayload
{
}

public partial class Itz<TIdentifier>
{
    public interface IShip<TPayload> : IShip
    where TPayload : IPayload
    {
        void Set(in TIdentifier id, in TPayload value);
    }

    public interface IShip : ISerializable
    {
        int Count();
    }

    public partial class DefaultShip<TPayload> : IShip<TPayload>
        where TPayload : IPayload, ITinyhandSerialize<TPayload>
    {
        public int Count()
        {
            return 0;
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
        }

        public void Set(in TIdentifier id, in TPayload value)
        {
        }

        [TinyhandObject(InitializerGenericsArguments = "int, Sandbox.ZenItz.InitializationPayload")]
        private sealed partial class Item
        {
            public Item()
            {
                var pt = typeof(ZenItz.Itz<>.DefaultShip<>.Item);
            }
        }
    }
}
