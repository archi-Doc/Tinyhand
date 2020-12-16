using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tinyhand;

namespace Sandbox
{
    [TinyhandObject] // Annote a [TinyhandObject] attribute.
    public partial class MyClass // partial class is required for source generator.
    {
        // Key attributes take a serialization index (or string name)
        // The values must be unique and versioning has to be considered as well.
        [Key(0)]
        [DefaultValue(99)]
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
        public List<string> Friends { get; set; } = default!; // Non-null value will be set by TinyhandSerializer.

        [Key(4)]
        public int[]? Ids { get; set; } // Nullable value will be set null.

        public MyClass()
        {
        }
    }
}
