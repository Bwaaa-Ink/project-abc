using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrixxInjection.Config;

// ReSharper disable once CheckNamespace
namespace TrixxInjection.Config
{
    /// <summary>
    /// Defines enumerations used between the Weaver and Consuming Packager.
    /// </summary>
    public static class Enums
    {
        /// <summary>
        /// Defines general behaviours for weaver execution.
        /// </summary>
        [Flags]
        public enum GeneralBehaviours : byte
        {
            /// <summary>
            /// Default Value, no general behaviours specified
            /// </summary>
            None = 0b0,
            /// <summary>
            /// Flags the execution mode for printing debug messages to MSBuild
            /// </summary>
            DebugLogging = 0b1,
            /// <summary>
            /// Enables the breakpointing during execution
            /// </summary>
            Breakpointer = 0b10,
            /// <summary>
            /// Logging is also recorded in the Log file, and not just printed.
            /// </summary>
            RecordLogs = 0b100,
            /// <summary>
            /// Toggles all above debugging flags
            /// </summary>
            Debugging = 0b111,
        }

        /// <summary>
        /// Values to use with <see cref="Configurator.SourceSerialisedTiming"/> to control when diagnostics are logged.
        /// </summary>
        [Flags]
        public enum SourceSerialisingTimingBehaviour : byte
        {
            /// <summary>
            /// Do not do base source serialisation.
            /// </summary>
            /// <remarks>Does not affect <see cref="Custom"/>.</remarks>
            None = 0b0,

            /// <summary>
            /// Serialise before weaving
            /// </summary>
            PreWeave = 0b1,

            /// <summary>
            /// Serialise after weaving
            /// </summary>
            PostWeave = 0b10,

            /// <summary>
            /// Do both <see cref="PreWeave"/> and <see cref="PostWeave"/>
            /// </summary>
            WrappedWeave = 0b11,

            /// <summary>
            /// Serialise all object
            /// </summary>
            Custom = 0b100
        }


        /// <summary>
        /// Values to use with <see cref="Configurator.SourceSerialiseSettings"/> to control when diagnostics are logged.
        /// </summary>
        [Flags]
        public enum SourceSerialiseBehaviour : ulong
        {
            /// <summary>
            /// None.
            /// </summary>
            None = 0x0,
            /// <summary>
            /// Used for whitespace and indenting on the resultant JSON file.
            /// </summary>
            PrettyPrint = 0x1,
            /// <summary>
            /// Serialise Properties
            /// </summary>
            SerialiseProperties = 0x2,
            /// <summary>
            /// Serialise Fields
            /// </summary>
            SerialiseFields = 0x4,
            /// <summary>
            /// [UNSTABLE] Will not work in 99% of situations.
            /// </summary>
            /// <remarks>If you ignore the right fields, you can skip the recursion and thus make this setting viable, otherwise do not use.</remarks>
            SerialisePrivateFields = 0x8,
            /// <summary>
            /// [ SLIGHTLY UNSTABLE ] full implementation not complete.
            /// </summary>
            SerialiseEvents = 0x10,
            /// <summary>
            /// Turn off object squishing entirely.
            /// </summary>
            DoNotUseSquishedObjects = 0x20,
            /// <summary>
            /// Disable the recommended default objects suggested to squish.
            /// </summary>
            /// <remarks>Not recommended.</remarks>
            DoNotAlsoUseRecommendedDefaultSquishedObjects = 0x40,
            /// <summary>
            /// Disable item ignoring entirely.
            /// </summary>
            /// <remarks>Not Recommended.</remarks>
            DoNotIgnoreItems = 0x80,
            /// <summary>
            /// Disable the recommended default items suggested to ignore.
            /// </summary>
            /// <remarks>Not Recommended.</remarks>
            DoNotAlsoUseRecommendedDefaultIgnoredItems = 0x100,
            /// <summary>
            /// Disable Aliases entirely
            /// </summary>
            DoNotUseAliases = 0x200,
            /// <summary>
            /// Do not use the recommended default aliases.
            /// </summary>
            DoNotAlsoUseRecommendedDefaultAliases = 0x400,
            /// <summary>
            /// Includes a list of types and how many of that type the weaver serialised.
            /// </summary>
            IncludeTypeCounts = 0x800,
        }
    }
}
