#define DEBUG_BREAK_ON_OVERFLOW
#define TYPE_COUNTING
//#define SIZING

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
    internal class SourceSerialiser
    {
        internal static ModuleWeaver Weaver;
        internal List<string> IgnoredObjects;
        internal List<(string, string)> Aliases;
        internal List<string> SquashedObjects;
        private readonly List<TypeMeta> squashedObjectsToRecord = new List<TypeMeta>();
        public List<(string ObjectName, string Mapping)> BooleanSchemas { get; } = new List<(string, string)>();
        public List<string> BannedNamespaces { get; } = new List<string>()
        {
            "System"
        };
        private readonly Dictionary<Type, int> squashTypeIds = new Dictionary<Type, int>();
        private readonly Dictionary<int, List<string>> squashMappings = new Dictionary<int, List<string>>();
        private int _nextSquashId = 1;
#if TYPE_COUNTING
        private readonly Dictionary<(string, string), int> TypeCounts = new Dictionary<(string, string), int>();
#endif

        internal bool PrettyPrint { get; set; } = false;
        private string Comma => PrettyPrint ? ", " : ",";
        private string Colon => PrettyPrint ? ": " : ":";
        private string NewLine => PrettyPrint ? "\n" : "";

        internal bool HandleProperties { get; set; } = true;
        internal bool HandleFields { get; set; } = true;
        internal bool HandlePrivateFields { get; set; } = false;
        internal bool HandleEvents { get; set; } = false;
        internal int MaxRecursionDepth { get; set; } = 300;

        private class TypeMeta
        {
            public PropertyInfo[] Props;
            public FieldInfo[] Fields;
            public EventInfo[] Events;
            public (PropertyInfo Prop, int ElemSize, Func<object, object> Getter)[] PrimEnumProps;
            public (PropertyInfo Prop, Func<object, object> Getter)[] BoolProps;
            public Func<object, object>[] PropGetters;
            public Func<object, object>[] FieldGetters;
            public string Name;
            public bool Squashed = false;
        }

        private readonly Dictionary<Type, TypeMeta> MetaCache = new Dictionary<Type, TypeMeta>();
        private readonly Dictionary<Type, int> PrimitiveSizes = new Dictionary<Type, int>
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

        private readonly List<string> VisitedStatics = new List<string>();

        public string Serialise(object obj, List<string> ignoredObjects = null, List<string> simplifiedComplexObjects = null, List<string> squashedObjects = null, Dictionary<string, string> aliases = null)
        {
            var stopwatch = Stopwatch.StartNew();
            string ind = PrettyPrint ? "    " : " ";
            string ind2 = ind + ind;
            var visited = new HashSet<object>(new ReferenceEqualityComparer());
            var sb = new StringBuilder();
            if (ignoredObjects != null)
                IgnoredObjects = ignoredObjects;
            if (squashedObjects != null)
                SquashedObjects = squashedObjects;
            if (aliases != null)
                Aliases = aliases.Select(kvp => (kvp.Key, kvp.Value)).ToList();
            AAppend(sb, "{");
            AAppend(sb, NewLine);
            AAppend(sb, ind);
            AAppend(sb, "\"data\"");
            AAppend(sb, Colon);
            SerializeValue(obj, simplifiedComplexObjects, visited, sb, 1, 0);

            AAppend(sb, $"{NewLine}{ind}\"metadata\"{Colon}{{");
            AAppend(sb, $"{NewLine}{ind2}\"squashMappings\"{Colon}{{");
            foreach (var kvp in squashMappings)
            {
                AAppend(sb, $"{NewLine}{ind2}{ind}{kvp.Key}{Colon}[{string.Join(Comma, kvp.Value.Select(n => $"\"{n}\""))}]");
                if (!kvp.Key.Equals(squashMappings.Last().Key)) AAppend(sb, Comma);
            }
            AAppend(sb, $"{NewLine}{ind2}}}{Comma}");
            AAppend(sb, $"{NewLine}{ind2}\"aliases\"{Colon}[{string.Join(Comma, Aliases.Select(a => $"{NewLine}{ind2}{ind}\"-{a.Item2}\"{Colon}\"{a.Item1}\""))}{NewLine}{ind2}]{Comma}", false);
#if TYPE_COUNTING
            AAppend(sb, 
                $"{NewLine}{ind2}Type Counts{Colon}{{{string.Join(Comma, TypeCounts.Select(kvp => $"{NewLine}{ind2}{ind}{(kvp.Key.Item1 ?? "NFN") + $"({kvp.Key.Item1})"}: {kvp.Value}"))}{NewLine}{ind2}}}{Comma}");
#endif
            stopwatch.Stop();
            sb.Append($"{NewLine}{ind2}\"time\"{Colon}{stopwatch.ElapsedMilliseconds}{NewLine}{ind}}}");
            AAppend(sb, NewLine);
            AAppend(sb, "}");
            return sb.ToString();
        }

        private void SerializeValue(object value, List<string> ignoreComplex,
            HashSet<object> visited, StringBuilder sb, int indent, int depth)
        {
            RuntimeHelpers.EnsureSufficientExecutionStack();
#if DEBUG_BREAK_ON_OVERFLOW
            if (depth >= MaxRecursionDepth) Debugger.Break();
#endif
            if (depth >= MaxRecursionDepth) throw new WeavingException($"Max recursion depth {MaxRecursionDepth} exceeded");

            if (value == null)
            {
                AAppend(sb, "null");
                return;
            }

            var type = value.GetType();
            if (type.Namespace != null && BannedNamespaces.Contains(type.Namespace))
            {
                AAppend(sb, '"');
                AAppend(sb, EscapeString(SafeToString(value)));
                AAppend(sb, '"');
                return;
            }

            if (type.IsEnum)
            {
                var underlying = Enum.GetUnderlyingType(type);
                var numeric = Convert.ChangeType(value, underlying, CultureInfo.InvariantCulture);
                AAppend(sb, Convert.ToString(numeric, CultureInfo.InvariantCulture));
                return;
            }

            if (type == typeof(string) || type == typeof(char))
            {
                AAppend(sb, '"');
                AAppend(sb, EscapeString(value.ToString()));
                AAppend(sb, '"');
                return;
            }
            if (type == typeof(bool))
            {
                AAppend(sb, value.ToString().ToLowerInvariant());
                return;
            }
            if (type.IsPrimitive || type == typeof(decimal))
            {
                AAppend(sb, Convert.ToString(value, CultureInfo.InvariantCulture));
                return;
            }

            if (!visited.Add(value))
            {
                AAppend(sb, '"');
                AAppend(sb, EscapeString(value.ToString()));
                AAppend(sb, '"');
                return;
            }

            if (value is IEnumerable ie)
            {
                AAppend(sb, '[');
                bool first = true;
                foreach (var item in ie)
                {
                    if (!first) AAppend(sb, Comma);
                    try { SerializeValue(item, ignoreComplex, visited, sb, indent + 1, depth + 1); }
                    catch (Exception ex)
                    {
                        Weaver.WriteInfo($"Error serializing element: {ex}");
                        AAppend(sb, '"');
                        AAppend(sb, "<Failed to parse value>");
                        AAppend(sb, '"');
                    }
                    first = false;
                }
                AAppend(sb, ']');
                return;
            }

            SerializeObject(value, ignoreComplex, visited, sb, indent, depth + 1);
        }

        private TypeMeta GetMeta(Type type)
        {
#if TYPE_COUNTING
            var tup = (type.FullName, type.Name);
            if (TypeCounts.TryGetValue(tup, out _))
                TypeCounts[tup]++;
            else
                TypeCounts.Add(tup, 1);
#endif
            if (MetaCache.TryGetValue(type, out var meta)) return meta;

            var props = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                            .Where(p => p.CanRead && IgnoredObjects.All(i => !p.Name.Equals(i, StringComparison.InvariantCultureIgnoreCase) && !p.Name.Contains(i)))
                            .ToArray();
            var fields = type.GetFields(HandlePrivateFields ? BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static : BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                             .Where(f => IgnoredObjects.All(i => !f.Name.Equals(i, StringComparison.InvariantCultureIgnoreCase) && !f.Name.Contains(i)))
                             .ToArray();
            var events = type.GetEvents(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                             .Where(e => IgnoredObjects.All(i => !e.Name.Equals(i, StringComparison.InvariantCultureIgnoreCase) && !e.Name.Contains(i)))
                             .ToArray();

            var propGetters = props.Select(CreateGetter).ToArray();
            var fieldGetters = fields.Select(CreateGetter).ToArray();

            var primEnum = props.Where(p => typeof(IEnumerable).IsAssignableFrom(p.PropertyType) && p.PropertyType.IsGenericType && PrimitiveSizes.ContainsKey(p.PropertyType.GetGenericArguments()[0]))
                                 .Select(p => (Prop: p, ElemSize: PrimitiveSizes[p.PropertyType.GetGenericArguments()[0]], Getter: CreateGetter(p)))
                                 .ToArray();

            var bools = props.Where(p => p.PropertyType == typeof(bool)).Select(p => (Prop: p, Getter: CreateGetter(p))).ToArray();
            //x Weaver.WriteDebug($"GENERATING META FOR {type.FullName ?? ("S:" + type.Name) ?? "Name was unresolved."}");
            meta = new TypeMeta { Squashed = SquashedObjects.Contains(type.FullName), Name = type.FullName ?? type.Name ?? "Name was unresolved.", Props = props, Fields = fields, Events = events, PropGetters = propGetters, FieldGetters = fieldGetters, PrimEnumProps = primEnum, BoolProps = bools };
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


        private void SerializeObject(object obj, List<string> ignoreComplex,
            HashSet<object> visited, StringBuilder sb, int indent, int depth)
        {
            RuntimeHelpers.EnsureSufficientExecutionStack();
#if DEBUG_BREAK_ON_OVERFLOW
            if (depth >= MaxRecursionDepth) Debugger.Break();
#endif
            if (depth >= MaxRecursionDepth) throw new WeavingException($"Max recursion depth {MaxRecursionDepth} exceeded");

            var type = obj.GetType();
            var meta = GetMeta(type);
            if (meta.Squashed)
            {
                SerializeSquashedValue(obj, ignoreComplex, visited, sb, indent, depth);
                return;
            }

            AAppend(sb, '{');AAppend(sb, NewLine);
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
                    if (!first)
                    {
                        AAppend(sb, Comma);
                        AAppend(sb, NewLine);
                    }
                    AAppend(sb, Indent(indent));
                    AAppend(sb, "\"flags\"");
                    AAppend(sb, Colon);
                    AAppend(sb, '"');
                    AAppend(sb, b64);
                    AAppend(sb, '"');
                    first = false;
                }

                foreach (var (prop, elemSize, getter) in meta.PrimEnumProps)
                {
                    IEnumerable ie2 = SafeGet(getter, obj) as IEnumerable;
                    if (ie2 == null) continue;
                    var items = ie2.Cast<object>().ToArray();
                    byte[] raw = BuildRaw(items, elemSize, prop.PropertyType.GetGenericArguments()[0]);
                    if (!first)
                    {
                        AAppend(sb, Comma);
                        AAppend(sb, NewLine);
                    }

                    AAppend(sb, Indent(indent));
                    AAppend(sb, '"');
                    AAppend(sb, prop.Name);
                    AAppend(sb, '"');
                    AAppend(sb, Colon);
                    AAppend(sb, '"');
                    AAppend(sb, Convert.ToBase64String(raw));
                    AAppend(sb, '"');
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
                            AAppend(sb, SafeQuote(staticVal)); first = false; continue;
                        }
                        VisitedStatics.Add(key);
                    }
                    if (meta.PrimEnumProps.Any(x => x.Prop == p) || p.PropertyType == typeof(bool)) continue;
                    var v = SafeGet(meta.PropGetters[i], obj);
                    if (!first)
                    {
                        AAppend(sb, Comma);
                        AAppend(sb, NewLine);
                    }

                    AAppend(sb, Indent(indent));
                    AAppend(sb, '"');
                    AAppend(sb, p.Name);
                    AAppend(sb, '"');
                    AAppend(sb, Colon);
                    if (v == null) AAppend(sb, "null");
                    else if (ignoreComplex.Contains(p.Name))
                    {
                        AAppend(sb, '"');
                        AAppend(sb, EscapeString(v.ToString()));
                        AAppend(sb, '"');
                    }
                    else try { SerializeValue(v, ignoreComplex, visited, sb, indent + 1, depth + 1); } catch (Exception ex) { Weaver.WriteInfo($"Error recursing {p.Name}: {ex}"); AAppend(sb, "\"<Failed to parse value>\""); }
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
                        if (VisitedStatics.Contains(key)) { AAppend(sb, SafeQuote(staticVal)); first = false; continue; }
                        VisitedStatics.Add(key);
                    }
                    var v = SafeGet(meta.FieldGetters[i], obj);
                    if (!first)
                    {
                        AAppend(sb, Comma);
                        AAppend(sb, NewLine);
                    }

                    AAppend(sb, Indent(indent));
                    AAppend(sb, '"');
                    AAppend(sb, meta.Fields[i].Name);
                    AAppend(sb, '"');
                    AAppend(sb, Colon);
                    if (v == null) AAppend(sb, "null"); else try { SerializeValue(v, ignoreComplex, visited, sb, indent + 1, depth + 1); } catch (Exception ex) { Weaver.WriteInfo($"Error recursing field {meta.Fields[i].Name}: {ex}"); AAppend(sb, "\"<Failed to parse value>\""); }
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
                    if (!first)
                    {
                        AAppend(sb, Comma);
                        AAppend(sb, NewLine);
                    }

                    AAppend(sb, Indent(indent));
                    AAppend(sb, '"');
                    AAppend(sb, e.Name);
                    AAppend(sb, '"');
                    AAppend(sb, Colon);
                    try { SerializeValue(subs, ignoreComplex, visited, sb, indent + 1, depth + 1); } catch (Exception ex) { Weaver.WriteInfo($"Error serializing event {e.Name}: {ex}"); AAppend(sb, "\"<Failed to parse value>\""); }
                    first = false;
                }
            }
#if SIZING
            AAppend(sb, Comma).AAppend(sb, NewLine).AAppend(sb, Indent(indent))
                .AAppend(sb, "\"weaver__sizing\"").AAppend(sb, Colon).AAppend(sb, 
                    //ComputeSize(obj, new HashSet<object>(new ReferenceEqualityComparer()), depth)
                    "Unimplemented"
                    );
#endif
            AAppend(sb, NewLine);
            AAppend(sb, new string(' ', indent * 4));
            AAppend(sb, '}');
        }

        private void SerializeSquashedValue(object value, List<string> ignoreComplex, HashSet<object> visited, StringBuilder sb, int indent, int depth)
        {
            var type = value.GetType();
            var meta = GetMeta(type);
            if (!squashTypeIds.TryGetValue(type, out var id))
            {
                id = _nextSquashId++;
                var names = new List<string>();
                if (HandleProperties)
                {
                    if (meta.BoolProps.Length > 3) names.Add("flags");
                    names.AddRange(meta.PrimEnumProps.Select(p => p.Prop.Name));
                    names.AddRange(meta.Props
                        .Where(p => meta.PrimEnumProps.All(x => x.Prop != p) && p.PropertyType != typeof(bool))
                        .Select(p => p.Name));
                }
                if (HandleFields) names.AddRange(meta.Fields.Select(f => f.Name));
                if (HandleEvents) names.AddRange(meta.Events.Select(e => e.Name));
                squashTypeIds[type] = id;
                squashMappings[id] = names;
            }
            AAppend(sb, '[');
            AAppend(sb, id.ToString());

            if (HandleProperties)
                for (int i = 0; i < meta.Props.Length; i++)
                {
                    var p = meta.Props[i];
                    var getter = meta.PropGetters[i];
                    if (p.GetMethod.IsStatic)
                    {
                        var key = $"{type.FullName}.{p.Name}";
                        var staticVal = SafeGet(getter, null);
                        if (VisitedStatics.Contains(key))
                        {
                            AAppend(sb, Comma);
                            AAppend(sb, SafeQuote(staticVal));
                            continue;
                        }
                        VisitedStatics.Add(key);
                    }
                    var v = SafeGet(getter, value);
                    AAppend(sb, Comma);
                    SerializeValue(v, ignoreComplex, visited, sb, indent, depth + 1);
                }

            if (HandleFields)
                for (int i = 0; i < meta.Fields.Length; i++)
                {
                    var f = meta.Fields[i];
                    var getter = meta.FieldGetters[i];
                    if (f.IsStatic)
                    {
                        var key = $"{type.FullName}.{f.Name}";
                        var staticVal = SafeGet(getter, null);
                        if (VisitedStatics.Contains(key))
                        {
                            AAppend(sb, Comma);
                            AAppend(sb, SafeQuote(staticVal));
                            continue;
                        }
                        VisitedStatics.Add(key);
                    }
                    var v = SafeGet(getter, value);
                    AAppend(sb, Comma);
                    SerializeValue(v, ignoreComplex, visited, sb, indent, depth + 1);
                }

            if (HandleEvents)
                foreach (var e in meta.Events)
                {
                    var fi = type.GetField(e.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                    if (fi.IsStatic)
                    {
                        var key = $"{type.FullName}.{e.Name}";
                        var dlgStatic = SafeGet(o => fi.GetValue(null), null) as Delegate;
                        var staticSubs = dlgStatic?.GetInvocationList().Select(d => d.Method.Name).ToArray() ?? Array.Empty<string>();
                        if (VisitedStatics.Contains(key))
                        {
                            AAppend(sb, Comma);
                            AAppend(sb, SafeQuote(staticSubs));
                            continue;
                        }
                        VisitedStatics.Add(key);
                    }
                    var dlg = SafeGet(o => fi.GetValue(value), value) as Delegate;
                    var subs = dlg?.GetInvocationList().Select(d => d.Method.Name).ToArray() ?? Array.Empty<string>();
                    AAppend(sb, Comma);
                    SerializeValue(subs, ignoreComplex, visited, sb, indent, depth + 1);
                }

            AAppend(sb, ']');
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

        [Obsolete]
        private long ComputeSize(object obj, HashSet<object> visited, int depth)
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

        public string Indent(int indent)
            => PrettyPrint ? new string(' ', (indent + 1) * 4) : "";

        public StringBuilder AAppend(StringBuilder sb, string message, bool alias = true)
        {
            if (alias)
                Aliases.ForEach(replacement => message = message.Replace(replacement.Item1, replacement.Item2));
            return sb.Append(message);
        }

        public static StringBuilder AAppend(StringBuilder sb, char message)
        {
            return sb.Append(message);
        }
    }

    internal sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public new bool Equals(object x, object y) => ReferenceEquals(x, y);
        public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
    }
}
