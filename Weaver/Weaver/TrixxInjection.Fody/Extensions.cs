using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Fody;
using Mono.Cecil;
using Mono.Cecil.Cil;
using TrixxInjection.Config;
using MethodAttributes = Mono.Cecil.MethodAttributes;

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

        public static MethodDefinition EnsureDefaultConstructor(this TypeDefinition type, bool @static = false)
        {
            var name = @static ? ".cctor" : ".ctor";
            var existing = type.Methods.FirstOrDefault(m =>
                m.Name == name && m.IsStatic == @static && m.Parameters.Count == 0);
            if (existing != null) return existing;

            var module = type.Module;
            var ctor = new MethodDefinition(
                name,
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                module.TypeSystem.Void);

            MethodReference baseCtorRef;
            if (type.BaseType?.Resolve() is TypeDefinition baseDef)
            {
                var baseParamless = baseDef.Methods.FirstOrDefault(m =>
                    m.Name == name && m.IsStatic == @static && m.Parameters.Count == 0);
                if (baseParamless != null)
                    baseCtorRef = module.ImportReference(baseParamless);
                else
                    baseCtorRef = module.ImportReference(
                        typeof(object).GetConstructor(Array.Empty<Type>()));
            }
            else
            {
                baseCtorRef = module.ImportReference(
                    typeof(object).GetConstructor(Array.Empty<Type>()));
            }

            var il = ctor.Body.GetILProcessor();
            il.Append(il.Create(OpCodes.Ldarg_0));
            il.Append(il.Create(OpCodes.Call, baseCtorRef));
            il.Append(il.Create(OpCodes.Ret));

            type.Methods.Add(ctor);
            return ctor;
        }

        public static MethodDefinition EnsureDefaultDestructor(this TypeDefinition type)
        {
            var existing = type.Methods.FirstOrDefault(m =>
                m.Name == "Finalize" && m.Parameters.Count == 0);
            if (existing != null) return existing;

            var module = type.Module;
            var fin = new MethodDefinition(
                "Finalize",
                MethodAttributes.Family | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.SpecialName,
                module.TypeSystem.Void);

            MethodReference baseFinRef;
            if (type.BaseType?.Resolve() is TypeDefinition baseDef)
            {
                var baseFinalizer = baseDef.Methods.FirstOrDefault(m =>
                    m.Name == "Finalize" && m.Parameters.Count == 0);
                baseFinRef = baseFinalizer != null
                    ? module.ImportReference(baseFinalizer)
                    : module.ImportReference(
                        typeof(object).GetMethod("Finalize", BindingFlags.Instance | BindingFlags.NonPublic));
            }
            else
            {
                baseFinRef = module.ImportReference(
                    typeof(object).GetMethod("Finalize", BindingFlags.Instance | BindingFlags.NonPublic));
            }

            fin.Overrides.Add(baseFinRef);
            var il = fin.Body.GetILProcessor();
            il.Append(il.Create(OpCodes.Ldarg_0));
            il.Append(il.Create(OpCodes.Call, baseFinRef));
            il.Append(il.Create(OpCodes.Ret));

            type.Methods.Add(fin);
            return fin;
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
                    if (ModuleWeaver.That.Configuration.GeneralBehaviour.HasFlag(Enums.GeneralBehaviours.DebugLogging))
                        ModuleWeaver.That.WriteDebug(message);
                    break;
                case LogLevel.Low:
                    ModuleWeaver.That.WriteMessage(message, MessageImportance.Low);
                    break;
                case LogLevel.Normal:
                    ModuleWeaver.That.WriteMessage(message, MessageImportance.Normal);
                    break;
                case LogLevel.High:
                    ModuleWeaver.That.WriteMessage(message, MessageImportance.High);
                    break;
                case LogLevel.Warning:
                    if (md != null)
                        ModuleWeaver.That.WriteWarning(message, md);
                    else
                        ModuleWeaver.That.WriteWarning(message, sq);
                    break;
                case LogLevel.Error:
                    if (md != null)
                        ModuleWeaver.That.WriteError(message, md);
                    else
                        ModuleWeaver.That.WriteError(message, sq);
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

        internal void FW(string message) 
            => ModuleWeaver.That.WriteWarning(message);

        internal L(LogLevel ll = LogLevel.Off) : base(ll) { }
    }
}
