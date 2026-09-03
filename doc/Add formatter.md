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

           public void Deserialize(ref TinyhandReader reader, ref Version? value, TinyhandSerializerOptions options)
           {
               if (reader.TryReadNil())
               {
                   value = null;
               }
               else
               {
                   value = new Version(reader.ReadString()!);
               }
           }

           public Version? Clone(Version? value, TinyhandSerializerOptions options) => value; // Version is immutable.

           public Version Reconstruct(TinyhandSerializerOptions options)
           {
               return new Version();
           }
       }
   ```



2. For a built-in formatter, assign the singleton to the typed cache in `BuiltinResolver` and add a closed `TinyhandTypeIdentifier.Register<T>()` call to `RegisterInstantiableTypes`.

3. For a generic formatter, add a typed registration method to `GeneratedResolver.Collections.cs` and its metadata-name mapping to `TinyhandGenerator/FormatterCatalog.cs`. The generator will emit calls with concrete type arguments; do not construct formatter types through reflection. Include any additional formatter dependencies in `StaticRegistrationGenerator`.

4. Add the target type to `FormatterResolver` so generated model members can use it. Verify a round trip in the NativeAOT test project.

Application-defined formatters can be registered directly:

```csharp
GeneratedResolver.Instance.SetFormatter<MyValue<int>>(new MyValueFormatter<int>());
```

Use a custom resolver to override a built-in formatter. See [NativeAOT](NativeAOT.md) for explicit closed-type roots and migration notes.
