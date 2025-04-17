using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrixxInjection
{
    /// <summary>
    /// Allows for configuration of the Weaver, such as Logging path and ID.
    /// </summary>
    public static class WeavingContext
    {
        private  static StreamWriter LogOutputFile = null;

        internal static bool? GetOutput(out StreamWriter sw)
        {
            sw = null;
            return null;
        }

        /// <summary>
        /// Controls the use of writing logs to the default System.out stream.
        /// </summary>
        /// <remarks>
        /// Use <see cref="false"/>
        /// </remarks>
        public static bool? UseConsole { get; set; } = false;

        /// <summary>
        /// Basic Top-level folder for debug logs.
        /// </summary>
        public static string BaseEnvDir { get; } = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        /// <summary>
        /// The ID for this logging session, will default to a new Guid if none is provided.
        /// </summary>
        public static string ContextId { get; set; } = Guid.NewGuid().ToString();
        /// <summary>
        /// The Filepath to which the logs are saved.
        /// </summary>
        /// <remarks>Defaults to the <see cref="BaseEnvDir"/>/WeavingLogs/<see cref="ContextId"/> directory.</remarks>
        public static string FilePath { get; set; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "WeavingLogs",
            (string.IsNullOrWhiteSpace(ContextId) ? Guid.NewGuid().ToString() : ContextId)
        );

        /// <summary>
        /// Initialises the weaver settings, this must be called to set any settings you want to change from defaults.
        /// </summary>
        public  static void Initialise()
        {
            
        }
    }
}
