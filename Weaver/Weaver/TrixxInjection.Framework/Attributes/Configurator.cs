using System;
using System.Collections.Generic;
using System.IO;
using TrixxInjection.Config;

namespace TrixxInjection.Framework.Attributes
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
    public class ConfiguratorAttribute : Attribute
    {
        public string LogFileName { get; set; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Weaving",
            "LOG_" + DateTimeOffset.Now.Unix() + ".log"
        );

        public string[] ObjectsToSquish { get; set; } = new string[0];
        public string[] ItemsToIgnore { get; set; } = new string[0];
        public string[] AliasKeys { get; set; } = new string[0];
        public string[] AliasValues { get; set; } = new string[0];

        public Enums.SourceSerialisingTimingBehaviour SourceSerialisedTiming { get; set; }
            = Enums.SourceSerialisingTimingBehaviour.WrappedWeave | Enums.SourceSerialisingTimingBehaviour.Custom;

        public Enums.SourceSerialiseBehaviour SourceSerialiseSettings { get; set; }
            = Enums.SourceSerialiseBehaviour.SerialiseProperties
              | Enums.SourceSerialiseBehaviour.SerialiseFields
              | Enums.SourceSerialiseBehaviour.IncludeTypeCounts;

        public Enums.GeneralBehaviours GeneralBehaviour { get; set; } = Enums.GeneralBehaviours.None;

        public static IReadOnlyCollection<string> DefaultRecommendedObjectsToSquish
            => new[]
            {
                "Mono.Cecil.Cil.Instruction",
                "Mono.Cecil.Cil.OpCode",
                "Mono.Cecil.Cil.SequencePoint",
                "Mono.Cecil.ParameterDefinition",
                "Mono.Cecil.MethodReturnType"
            };

        public static IReadOnlyCollection<string> DefaultRecommendedItemsToIgnore
            => new[]
            {
                "Mono.Cecil.Cil.Instruction.Next",
                "Mono.Cecil.Cil.Instruction.Previous",
                "MetadataToken",
                "Mono.Cecil.ModuleDefinition.TypeSystem",
                "Projections",
                "PublicKey"
            };

        public static IReadOnlyDictionary<string, string> DefaultRecommendedAliases
            => new Dictionary<string, string>
            {
                { "System.Void",   "void"   },
                { "System.String", "string" },
                { "System.Double", "double" },
                { "System.Single", "float"  },
                { "System.Byte",   "byte"   },
                { "System.Char",   "char"   },
                { "System.Int32",  "int"    },
                { "System.Int64",  "long"   },
                { "System.Int16",  "short"  },
                { "System.UInt32", "uint"   },
                { "System.UInt64", "ulong"  },
                { "System.UInt16", "ushort" }
            };
    }

    static class Bwaaa
    {
        public static long Unix(this DateTimeOffset d)
            => (long)(d - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
    }
}
