## Tinyhand
Tiny and simple data format/serializer by archi-Doc. Tinyhand is largely based on [MessagePack for C#](https://github.com/neuecc/MessagePack-CSharp) by neuecc, AArnott.



## Usage

```csharp
[TinyhandObject] // Annote a [TinyhandObject] attribute.
    public partial class MyClass // partial class is required for surce generation.
    {
        // Key attributes take a serialization index (or string name)
        // The values must be unique and versioning has to be considered as well.
        [Key(0)]
        public int Age { get; set; }

        [Key(1)]
        public string FirstName { get; set; } = string.Empty;

        [Key(2)]
        [DefaultValue("Doe")] // If there is no corresponding data, the default value is set.
        public string LastName { get; set; } = string.Empty;

        // All fields or properties that should not be serialized must be annotated with [IgnoreMember].
        [IgnoreMember]
        public string FullName { get { return FirstName + LastName; } }

        [Key(3)]
        public List<string> Friends { get; set; } // Non-null value will be set by TinyhandSerializer.

        [Key(4)]
        public int[]? Ids { get; set; } // Default value is null.

        public MyClass()
        {
            this.MemberNotNull(); // optional: Informs the compiler that field or property members are set non-null values by TinyhandSerializer.
            // this.Reconstruct(TinyhandSerializerOptions.Standard); // optional: Call Reconstruct() to actually create instances of members.
        }
    }

    [TinyhandObject]
    public partial class EmptyClass
    {
    }
```



```csharp
var myClass = new MyClass() { Age = 10, FirstName = "hoge", LastName = "huga", };
            var b = TinyhandSerializer.Serialize(myClass);
            var myClass2 = TinyhandSerializer.Deserialize<MyClass>(b);

            b = TinyhandSerializer.Serialize(new EmptyClass()); // Empty data
            var myClass3 = TinyhandSerializer.Deserialize<MyClass>(b); // Create an instance and set non-null values of the members.
```



## Add new formatter

1. Implement the formatter class.

   ```csharp
   public sealed class VersionFormatter : ITinyhandFormatter<Version>
       {
           public static readonly ITinyhandFormatter<Version> Instance = new VersionFormatter();
   
           private VersionFormatter()
           {
           }
   
           public void Serialize(ref TinyhandWriter writer, Version? value, TinyhandSerializerOptions options)
           {
               if (value == null)
               {
                   writer.WriteNil();
               }
               else
               {
                   writer.Write(value.ToString());
               }
           }
   
           public Version? Deserialize(ref TinyhandReader reader, TinyhandSerializerOptions options)
           {
               if (reader.TryReadNil())
               {
                   return null;
               }
               else
               {
                   return new Version(reader.ReadString());
               }
           }
   
           public Version Reconstruct(TinyhandSerializerOptions options)
           {
               return new Version();
           }
       }
   ```

   

2. Add the formatter instance to resolver.

   ```csharp
   { typeof(Version), VersionFormatter.Instance },
   ```

   If the type is a generic class, add a code which creates the formatter instance to GenericsResolver.

    

3. 

