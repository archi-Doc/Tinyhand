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
public partial class Payload : IPayload
{
    [Key(0)]
    public int X { get; set; }
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

        public void Test()
        {
            var item = new Item(4, default!);
            var b = TinyhandSerializer.SerializeObject(item);
        }

        [TinyhandObject]
        private sealed partial class Item
        {
            public Item()
            {
            }

            public Item(int id, TPayload payload)
            {
                var pt = typeof(ZenItz.Itz<>.DefaultShip<>.Item);
                Id = id;
                Payload = payload;
            }

            [Key(0)]
            public int Id { get; set; }

            [Key(1)]
            TPayload Payload { get; set; } = default!;
        }
    }
}
