using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrixxInjection
{
    /// <summary>
    /// Attribute marking a method for debug information weaving
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class MethodLogger : Attribute
    {
        /// <summary>
        /// 
        /// </summary>
        public bool LogExceptions { get; set; } = true;
        /// <summary>
        /// 
        /// </summary>
        public bool LogEntryTime { get; set; } = true;
        /// <summary>
        /// 
        /// </summary>
        public bool LogExitTime { get; set; } = true;
        /// <summary>
        /// 
        /// </summary>
        public bool LogParameters { get; set; } = true;
        /// <summary>
        /// 
        /// </summary>
        public bool LogCallSite { get; set; } = true;
        /// <summary>
        /// 
        /// </summary>
        public bool LogContext { get; set; } = true;
        /// <summary>
        /// 
        /// </summary>
        public bool LogReturn { get; set; } = true;
        /// <summary>
        /// 
        /// </summary>
        public bool LogTime { get; set; } = true;
    }
}
