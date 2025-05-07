using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fody;
using Mono.Cecil;
using Mono.Cecil.Cil;
using TrixxInjection.Config;

namespace TrixxInjection.Fody
{
    internal static class Extensions
    {
        public static string[] GetFlagNames<T>(object value) where T : Enum
        {
            var numericValue = Convert.ToUInt64(value);
            return Enum.GetValues(typeof(T)).Cast<T>()
                .Where(flag =>
                {
                    var flagValue = Convert.ToUInt64(flag);
                    return flagValue != 0 && (numericValue & flagValue) == flagValue;
                })
                .Select(flag => flag.ToString())
                .ToArray();
        }
    }

    internal class Logging
    {
        internal enum LogLevel : byte
        {
            Off,
            Debug,
            Low,
            Normal,
            High,
            Warning,
            Error
        }

        private readonly LogLevel _level;

        internal Logging(LogLevel ll = LogLevel.Off)
        {
            _level = ll;
        }

        internal string Log(string message, MethodDefinition md = null, SequencePoint sq = null)
        {
            switch (_level)
            {
                case LogLevel.Off:
                    break;
                case LogLevel.Debug:
                    if (SourceSerialiser.Weaver.Configuration.GeneralBehaviour.HasFlag(Enums.GeneralBehaviours.DebugLogging))
                        SourceSerialiser.Weaver.WriteDebug(message);
                    break;
                case LogLevel.Low:
                    SourceSerialiser.Weaver.WriteMessage(message, MessageImportance.Low);
                    break;
                case LogLevel.Normal:
                    SourceSerialiser.Weaver.WriteMessage(message, MessageImportance.Normal);
                    break;
                case LogLevel.High:
                    SourceSerialiser.Weaver.WriteMessage(message, MessageImportance.High);
                    break;
                case LogLevel.Warning:
                    if (md != null)
                        SourceSerialiser.Weaver.WriteWarning(message, md);
                    else
                        SourceSerialiser.Weaver.WriteWarning(message, sq);
                    break;
                case LogLevel.Error:
                    if (md != null)
                        SourceSerialiser.Weaver.WriteError(message, md);
                    else
                        SourceSerialiser.Weaver.WriteError(message, sq);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return message;
        }
    }

    internal sealed class L : Logging
    {
        internal string W(string message, MethodDefinition md = null, SequencePoint sq = null)
            => base.Log(message, md, sq);

        internal string this[string s]
        {
            get => W(s);
        }

        internal L(LogLevel ll = LogLevel.Off) : base(ll) { }
    }
}
