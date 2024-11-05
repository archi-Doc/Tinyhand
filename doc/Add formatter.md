## Add new formatter

1. Implement a formatter class.

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

    

3. Add the target type to the FormatterResolver.

