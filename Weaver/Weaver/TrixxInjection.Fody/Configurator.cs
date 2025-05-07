using System;
using System.Collections.Generic;
using System.IO;
using static TrixxInjection.Config.Enums;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

// ReSharper disable once CheckNamespace
namespace TrixxInjection.Config
{
    /// <summary>
    /// Inherit this class and then override the values you want the weaver to use
    /// </summary>
    /// <remarks>This is your settings.</remarks>
    public class Configurator
    {
        public virtual string LogFileName { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Weaving",
            "LOG_",
            DateTimeOffset.Now.Unix().ToString(),
            ".log"
        );

        #region Squishing

        /// <summary>
        /// A list of the fully qualified names of objects to squish. See <see cref="DefaultRecommendedObjectsToSquish"/> for objects squished by default.
        /// </summary>
        /// <remarks>Squishing an object tells the weaver to only record the values of the data it should serialise for each occurence of the object, and then map them in the window viewer. </remarks>
        public virtual List<string> ObjectsToSquish { get; } = new List<string>();

        /// <summary>
        /// These are the default, recommended objects to squish. They will be automagically used unless <see cref="SourceSerialiseBehaviour.DoNotAlsoUseRecommendedDefaultSquishedObjects"/> is toggled.
        /// </summary>
        public static IReadOnlyCollection<string> DefaultRecommendedObjectsToSquish => _drots;

        // ReSharper disable once InconsistentNaming
        // ReSharper disable once IdentifierTypo
        private static readonly List<string> _drots = new List<string>
        {
            // Requires fully qualified name.
            "Mono.Cecil.Cil.Instruction",
            "Mono.Cecil.Cil.OpCode",
            "Mono.Cecil.Cil.SequencePoint",
            "Mono.Cecil.ParameterDefinition",
            "Mono.Cecil.MethodReturnType",
        };

        #endregion

        #region IgnoredItems

        /// <summary>
        /// A list of the fully qualified names of items to ignore. See <see cref="DefaultRecommendedItemsToIgnore"/> for objects ignored by default.
        /// </summary>
        /// <remarks> Ignoring an item will make the weaver skip its serialisation entirely.</remarks>
        public virtual List<string> ItemsToIgnore { get; } = new List<string>();

        /// <summary>
        /// These are the default, recommended items to ignore. They will be automagically used unless <see cref="SourceSerialiseBehaviour.DoNotAlsoUseRecommendedDefaultIgnoredItems"/> is toggled.
        /// </summary>
        public static IReadOnlyCollection<string> DefaultRecommendedItemsToIgnore => _droti;

        // ReSharper disable once InconsistentNaming
        // ReSharper disable once IdentifierTypo
        private static readonly List<string> _droti = new List<string>
        {
            "Mono.Cecil.Cil.Instruction.Next",
            "Mono.Cecil.Cil.Instruction.Previous",
            "MetadataToken",
            "Mono.Cecil.ModuleDefinition.TypeSystem",
            "Projections",
            "PublicKey"
        };

        #endregion

        #region Aliases
        /// <summary>
        /// A list of aliases. See <see cref="DefaultRecommendedAliases"/> for default aliases.
        /// </summary>
        /// <remarks> An alias is just where the weaver will replace all instances of the key with the value when serialising. i.e: Using defaults, 'System.Void' becomes 'void'.</remarks>
        public virtual Dictionary<string, string> Aliases { get; }
            = new Dictionary<string, string>();


        /// <summary>
        /// These are the default, recommended items to ignore. They will be automagically used unless <see cref="SourceSerialiseBehaviour.DoNotAlsoUseRecommendedDefaultAliases"/> is toggled.
        /// </summary>
        public static IReadOnlyDictionary<string, string> DefaultRecommendedAliases => _drota;

        // ReSharper disable once InconsistentNaming
        // ReSharper disable once IdentifierTypo
        private static readonly Dictionary<string, string> _drota = new Dictionary<string, string>()
        {
            { "System.Void", "void" },
            { "System.String", "string" },
            { "System.Double", "double" },
            { "System.Single", "float" },
            { "System.Byte", "byte" },
            { "System.Char", "char" },
            { "System.Int32", "int" },
            { "System.Int64", "long" },
            { "System.Int16", "short" },
            { "System.UInt32", "uint" },
            { "System.UInt64", "ulong" },
            { "System.UInt16", "ushort" },
        };

        #endregion

        #region Source Cereal

        /// <summary>
        /// Control when source is serialised. Use <see cref="SourceSerialiseBehaviour"/>
        /// </summary>
        public virtual SourceSerialisingTimingBehaviour SourceSerialisedTiming { get; } = SourceSerialisingTimingBehaviour.WrappedWeave | SourceSerialisingTimingBehaviour.Custom;

        /// <summary>
        /// Controlled Diagnostic behaviour.
        /// </summary>
        public virtual SourceSerialiseBehaviour SourceSerialiseSettings { get; }
            = SourceSerialiseBehaviour.SerialiseProperties | SourceSerialiseBehaviour.SerialiseFields | SourceSerialiseBehaviour.IncludeTypeCounts;

        #endregion

        /// <summary>
        /// Defines the general behaviour of the Weaver.
        /// </summary>
        public virtual GeneralBehaviours GeneralBehaviour { get; } = GeneralBehaviours.None;

        public bool Bwaaa = false;
    }

    internal static class Random
    {
        public static long Unix(this DateTimeOffset d)
            => (long)(d - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
    }
}
