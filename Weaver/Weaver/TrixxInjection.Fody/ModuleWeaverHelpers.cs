#define DEBUG_BREAK_ON_OVERFLOW

using Fody;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Diagnostics;

namespace TrixxInjection.Fody
{
    internal static class ModuleWeaverHelpers
    {
        internal static ModuleWeaver Weaver;
        internal static List<string> IgnoredStuff;
        public static List<(string ObjectName, string Mapping)> BooleanSchemas { get; } = new List<(string, string)>();
        public static List<string> BannedNamespaces { get; } = new List<string>()
        {
            "System"
        };

        private const bool PrettyPrint = true;
        private static string Comma => PrettyPrint ? ", " : ",";
        private static string Colon => PrettyPrint ? ": " : ":";
        private static string NewLine => PrettyPrint ? "\n" : "";

        private const bool HandleProperties = true;
        private const bool HandleFields = true;
        private const bool HandlePrivateFields = false;
        private const bool HandleEvents = true;
        private const int MaxRecursionDepth = 300;

        private class TypeMeta
        {
            public PropertyInfo[] Props;
            public FieldInfo[] Fields;
            public EventInfo[] Events;
            public (PropertyInfo Prop, int ElemSize, Func<object, object> Getter)[] PrimEnumProps;
            public (PropertyInfo Prop, Func<object, object> Getter)[] BoolProps;
            public Func<object, object>[] PropGetters;
            public Func<object, object>[] FieldGetters;
        }

        private static readonly Dictionary<Type, TypeMeta> MetaCache = new Dictionary<Type, TypeMeta>();
        private static readonly Dictionary<Type, int> PrimitiveSizes = new Dictionary<Type, int>
        {
            { typeof(bool), sizeof(bool) },
            { typeof(byte), sizeof(byte) },
            { typeof(sbyte), sizeof(sbyte) },
            { typeof(char), sizeof(char) },
            { typeof(short), sizeof(short) },
            { typeof(ushort), sizeof(ushort) },
            { typeof(int), sizeof(int) },
            { typeof(uint), sizeof(uint) },
            { typeof(long), sizeof(long) },
            { typeof(ulong), sizeof(ulong) },
            { typeof(float), sizeof(float) },
            { typeof(double), sizeof(double) },
            { typeof(decimal), 16 }
        };

        private static readonly List<string> VisitedStatics = new List<string>();

        public static string SerializeToJson(object obj, List<string> ignoreProps, List<string> ignoreComplex)
        {
            var visited = new HashSet<object>(new ReferenceEqualityComparer());
            var sb = new StringBuilder();
            IgnoredStuff = ignoreProps;
            SerializeValue(obj, ignoreComplex, visited, sb, 0, 0);
            return sb.ToString();
        }

        private static void SerializeValue(object value, List<string> ignoreComplex,
            HashSet<object> visited, StringBuilder sb, int indent, int depth)
        {
            RuntimeHelpers.EnsureSufficientExecutionStack();
#if DEBUG_BREAK_ON_OVERFLOW
            if (depth >= MaxRecursionDepth) Debugger.Break();
#endif
            if (depth >= MaxRecursionDepth) throw new WeavingException($"Max recursion depth {MaxRecursionDepth} exceeded");

            if (value == null)
            {
                sb.Append("null");
                return;
            }

            var type = value.GetType();
            if (type.Namespace != null && BannedNamespaces.Contains(type.Namespace))
            {
                sb.Append('"').Append(EscapeString(SafeToString(value))).Append('"');
                return;
            }

            if (type.IsEnum)
            {
                var underlying = Enum.GetUnderlyingType(type);
                var numeric = Convert.ChangeType(value, underlying, CultureInfo.InvariantCulture);
                sb.Append(Convert.ToString(numeric, CultureInfo.InvariantCulture));
                return;
            }

            if (type == typeof(string) || type == typeof(char))
            {
                sb.Append('"').Append(EscapeString(value.ToString())).Append('"');
                return;
            }
            if (type == typeof(bool))
            {
                sb.Append(value.ToString().ToLowerInvariant());
                return;
            }
            if (type.IsPrimitive || type == typeof(decimal))
            {
                sb.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
                return;
            }

            if (!visited.Add(value))
            {
                sb.Append('"').Append(EscapeString(value.ToString())).Append('"');
                return;
            }

            if (value is IEnumerable ie)
            {
                sb.Append('[');
                bool first = true;
                foreach (var item in ie)
                {
                    if (!first) sb.Append(Comma);
                    try { SerializeValue(item, ignoreComplex, visited, sb, indent + 1, depth + 1); }
                    catch (Exception ex)
                    {
                        Weaver.WriteInfo($"Error serializing element: {ex}");
                        sb.Append('"').Append("<Failed to parse value>").Append('"');
                    }
                    first = false;
                }
                sb.Append(']');
                return;
            }

            SerializeObject(value, ignoreComplex, visited, sb, indent, depth + 1);
        }

        private static TypeMeta GetMeta(Type type)
        {
            if (MetaCache.TryGetValue(type, out var meta)) return meta;

            var props = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                            .Where(p => p.CanRead && IgnoredStuff.All(i => !p.Name.Equals(i, StringComparison.InvariantCultureIgnoreCase) && !p.Name.Contains(i)))
                            .ToArray();
            var fields = type.GetFields(HandlePrivateFields ? BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static : BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                             .Where(f => IgnoredStuff.All(i => !f.Name.Equals(i, StringComparison.InvariantCultureIgnoreCase) && !f.Name.Contains(i)))
                             .ToArray();
            var events = type.GetEvents(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                             .Where(e => IgnoredStuff.All(i => !e.Name.Equals(i, StringComparison.InvariantCultureIgnoreCase) && !e.Name.Contains(i)))
                             .ToArray();

            var propGetters = props.Select(CreateGetter).ToArray();
            var fieldGetters = fields.Select(CreateGetter).ToArray();

            var primEnum = props.Where(p => typeof(IEnumerable).IsAssignableFrom(p.PropertyType) && p.PropertyType.IsGenericType && PrimitiveSizes.ContainsKey(p.PropertyType.GetGenericArguments()[0]))
                                 .Select(p => (Prop: p, ElemSize: PrimitiveSizes[p.PropertyType.GetGenericArguments()[0]], Getter: CreateGetter(p)))
                                 .ToArray();

            var bools = props.Where(p => p.PropertyType == typeof(bool)).Select(p => (Prop: p, Getter: CreateGetter(p))).ToArray();

            meta = new TypeMeta { Props = props, Fields = fields, Events = events, PropGetters = propGetters, FieldGetters = fieldGetters, PrimEnumProps = primEnum, BoolProps = bools };
            MetaCache[type] = meta;
            return meta;
        }

        private static Func<object, object> CreateGetter(PropertyInfo p)
        {
            var target = Expression.Parameter(typeof(object), "o");
            Expression instance;
            if (p.GetMethod.IsStatic)
                instance = null;
            else if (p.DeclaringType.IsValueType)
                instance = Expression.Convert(target, p.DeclaringType);
            else
                instance = Expression.TypeAs(target, p.DeclaringType);

            Expression call = p.GetMethod.IsStatic
                ? Expression.Call(p.GetMethod)
                : Expression.Call(instance, p.GetMethod);

            var result = Expression.Convert(call, typeof(object));

            Expression body;
            body = p.GetMethod.IsStatic || p.DeclaringType.IsValueType
                ? (Expression)result
                : Expression.Condition(
                    Expression.Equal(instance, Expression.Constant(null, p.DeclaringType)),
                    Expression.Constant(null, typeof(object)),
                    result);

            return Expression.Lambda<Func<object, object>>(body, target).Compile();
        }

        private static Func<object, object> CreateGetter(FieldInfo f)
        {
            var target = Expression.Parameter(typeof(object), "o");
            Expression instance;
            if (f.IsStatic)
                instance = null;
            else if (f.DeclaringType.IsValueType)
                instance = Expression.Convert(target, f.DeclaringType);
            else
                instance = Expression.TypeAs(target, f.DeclaringType);

            Expression expr = f.IsStatic
                ? Expression.Field(null, f)
                : Expression.Field(instance, f);

            var result = Expression.Convert(expr, typeof(object));

            Expression body = f.IsStatic || f.DeclaringType.IsValueType
                ? (Expression)result
                : Expression.Condition(
                    Expression.Equal(instance, Expression.Constant(null, f.DeclaringType)),
                    Expression.Constant(null, typeof(object)),
                    result);

            return Expression.Lambda<Func<object, object>>(body, target).Compile();
        }


        private static void SerializeObject(object obj, List<string> ignoreComplex,
            HashSet<object> visited, StringBuilder sb, int indent, int depth)
        {
            RuntimeHelpers.EnsureSufficientExecutionStack();
#if DEBUG_BREAK_ON_OVERFLOW
            if (depth >= MaxRecursionDepth) Debugger.Break();
#endif
            if (depth >= MaxRecursionDepth) throw new WeavingException($"Max recursion depth {MaxRecursionDepth} exceeded");

            var type = obj.GetType();
            var meta = GetMeta(type);

            sb.Append('{').Append(NewLine);
            bool first = true;

            if (HandleProperties)
            {
                var boolProps = meta.BoolProps;
                if (boolProps.Length > 3)
                {
                    var map = string.Join(",", boolProps.Select((b, i) => $"{i}:{b.Prop.Name}"));
                    BooleanSchemas.Add((type.FullName, map));
                    var buffer = new byte[(boolProps.Length + 7) / 8];
                    for (int i = 0; i < boolProps.Length; i++)
                        if ((bool)SafeGet(boolProps[i].Getter, obj)) buffer[i / 8] |= (byte)(1 << (i % 8));
                    var b64 = Convert.ToBase64String(buffer);
                    if (!first) sb.Append(Comma).Append(NewLine);
                    sb.Append(new string(' ', (indent + 1) * 4)).Append("\"flags\"").Append(Colon).Append('"').Append(b64).Append('"');
                    first = false;
                }

                foreach (var (prop, elemSize, getter) in meta.PrimEnumProps)
                {
                    IEnumerable ie2 = SafeGet(getter, obj) as IEnumerable;
                    if (ie2 == null) continue;
                    var items = ie2.Cast<object>().ToArray();
                    byte[] raw = BuildRaw(items, elemSize, prop.PropertyType.GetGenericArguments()[0]);
                    if (!first) sb.Append(Comma).Append(NewLine);
                    sb.Append(new string(' ', (indent + 1) * 4)).Append('"').Append(prop.Name).Append('"').Append(Colon).Append('"').Append(Convert.ToBase64String(raw)).Append('"');
                    first = false;
                }
            }

            if (HandleProperties)
            {
                for (int i = 0; i < meta.Props.Length; i++)
                {
                    var p = meta.Props[i];
                    if (p.GetMethod.IsStatic)
                    {
                        var key = $"{type.FullName}.{p.Name}";
                        var staticVal = SafeGet(meta.PropGetters[i], null);
                        if (VisitedStatics.Contains(key))
                        {
                            sb.Append(SafeQuote(staticVal)); first = false; continue;
                        }
                        VisitedStatics.Add(key);
                    }
                    if (meta.PrimEnumProps.Any(x => x.Prop == p) || p.PropertyType == typeof(bool)) continue;
                    var v = SafeGet(meta.PropGetters[i], obj);
                    if (!first) sb.Append(Comma).Append(NewLine);
                    sb.Append(new string(' ', (indent + 1) * 4)).Append('"').Append(p.Name).Append('"').Append(Colon);
                    if (v == null) sb.Append("null");
                    else if (ignoreComplex.Contains(p.Name)) sb.Append('"').Append(EscapeString(v.ToString())).Append('"');
                    else try { SerializeValue(v, ignoreComplex, visited, sb, indent + 1, depth + 1); } catch (Exception ex) { Weaver.WriteInfo($"Error recursing {p.Name}: {ex}"); sb.Append("\"<Failed to parse value>\""); }
                    first = false;
                }
            }

            if (HandleFields)
            {
                for (int i = 0; i < meta.Fields.Length; i++)
                {
                    var f = meta.Fields[i];
                    if (f.IsStatic)
                    {
                        var key = $"{type.FullName}.{f.Name}";
                        var staticVal = SafeGet(meta.FieldGetters[i], null);
                        if (VisitedStatics.Contains(key)) { sb.Append(SafeQuote(staticVal)); first = false; continue; }
                        VisitedStatics.Add(key);
                    }
                    var v = SafeGet(meta.FieldGetters[i], obj);
                    if (!first) sb.Append(Comma).Append(NewLine);
                    sb.Append(new string(' ', (indent + 1) * 4)).Append('"').Append(meta.Fields[i].Name).Append('"').Append(Colon);
                    if (v == null) sb.Append("null"); else try { SerializeValue(v, ignoreComplex, visited, sb, indent + 1, depth + 1); } catch (Exception ex) { Weaver.WriteInfo($"Error recursing field {meta.Fields[i].Name}: {ex}"); sb.Append("\"<Failed to parse value>\""); }
                    first = false;
                }
            }

            if (HandleEvents)
            {
                foreach (var e in meta.Events)
                {
                    var fi = type.GetField(e.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                    var dlg = SafeGet(o => fi.GetValue(obj), obj) as Delegate;
                    var subs = dlg?.GetInvocationList().Select(d => d.Method.Name).ToArray() ?? Array.Empty<string>();
                    if (!first) sb.Append(Comma).Append(NewLine);
                    sb.Append(new string(' ', (indent + 1) * 4)).Append('"').Append(e.Name).Append('"').Append(Colon);
                    try { SerializeValue(subs, ignoreComplex, visited, sb, indent + 1, depth + 1); } catch (Exception ex) { Weaver.WriteInfo($"Error serializing event {e.Name}: {ex}"); sb.Append("\"<Failed to parse value>\""); }
                    first = false;
                }
            }

            sb.Append(Comma).Append(NewLine).Append(new string(' ', (indent + 1) * 4))
                .Append("\"weaver_sizing\"").Append(Colon).Append(
                    //ComputeSize(obj, new HashSet<object>(new ReferenceEqualityComparer()), depth)
                    "Unimplemented"
                    );
            sb.Append(NewLine).Append(new string(' ', indent * 4)).Append('}');
        }

        private static object SafeGet(Func<object, object> getter, object instance)
        {
            try { return getter(instance); }
            catch (Exception ex)
            {
                Weaver?.WriteInfo($"Error in getter: {ex}");
                try { return instance?.ToString(); } catch { return "<Failed to parse value>"; }
            }
        }

        private static byte[] BuildRaw(object[] items, int elemSize, Type elemType)
        {
            var raw = new byte[items.Length * elemSize];
            int off = 0;
            foreach (var it in items)
            {
                byte[] b;
                switch (Type.GetTypeCode(elemType))
                {
                    case TypeCode.Byte: b = new[] { (byte)it }; break;
                    case TypeCode.SByte: b = new[] { (byte)(sbyte)it }; break;
                    case TypeCode.Char: b = BitConverter.GetBytes((char)it); break;
                    case TypeCode.Int16: b = BitConverter.GetBytes((short)it); break;
                    case TypeCode.UInt16: b = BitConverter.GetBytes((ushort)it); break;
                    case TypeCode.Int32: b = BitConverter.GetBytes((int)it); break;
                    case TypeCode.UInt32: b = BitConverter.GetBytes((uint)it); break;
                    case TypeCode.Int64: b = BitConverter.GetBytes((long)it); break;
                    case TypeCode.UInt64: b = BitConverter.GetBytes((ulong)it); break;
                    case TypeCode.Single: b = BitConverter.GetBytes((float)it); break;
                    case TypeCode.Double: b = BitConverter.GetBytes((double)it); break;
                    case TypeCode.Decimal:
                        var bits = decimal.GetBits((decimal)it);
                        b = new byte[16]; Buffer.BlockCopy(bits, 0, b, 0, 16);
                        break;
                    default:
                        b = BitConverter.GetBytes(Convert.ToDouble(it, CultureInfo.InvariantCulture));
                        break;
                }
                Array.Copy(b, 0, raw, off, b.Length);
                off += b.Length;
            }
            return raw;
        }

        private static long ComputeSize(object obj, HashSet<object> visited, int depth)
        {
            RuntimeHelpers.EnsureSufficientExecutionStack();
#if DEBUG_BREAK_ON_OVERFLOW
            if (depth >= MaxRecursionDepth) Debugger.Break();
#endif
            if (depth >= MaxRecursionDepth) throw new WeavingException($"Max recursion depth {MaxRecursionDepth} exceeded");

            if (obj == null || !visited.Add(obj)) return 0;
            var type = obj.GetType();
            if (type == typeof(string)) return 16 + ((string)obj).Length * sizeof(char);
            if (PrimitiveSizes.TryGetValue(type, out var ps)) return 16 + ps;

            var meta = GetMeta(type);
            long size = 16;
            foreach (var (p, elemSize, getter) in meta.PrimEnumProps)
            {
                var ie = SafeGet(getter, obj) as IEnumerable;
                if (ie != null) return size + ie.Cast<object>().Count() * elemSize;
            }
            if (obj is IEnumerable ie2) foreach (var x in ie2) size += ComputeSize(x, visited, depth + 1);
            else foreach (var g in meta.PropGetters) size += ComputeSize(SafeGet(g, obj), visited, depth + 1);
            foreach (var g in meta.FieldGetters) size += ComputeSize(SafeGet(g, obj), visited, depth + 1);
            return size;
        }

        private static string EscapeString(string s) =>
            s.Replace("\\", "\\\\")
             .Replace("\"", "\\\"")
             .Replace("\n", "\\n")
             .Replace("\r", "\\r")
             .Replace("\t", "\\t");

        private static string SafeToString(object o)
        {
            try { return o.ToString(); }
            catch { return "<Failed to parse value>"; }
        }

        private static string SafeQuote(object o)
        {
            var s = SafeToString(o);
            return '"' + EscapeString(s) + '"';
        }

        public static void ValidateNamespace(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new WeavingException("Invalid namespace");
        }
    }

    internal sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public new bool Equals(object x, object y) => ReferenceEquals(x, y);
        public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
    }
}
