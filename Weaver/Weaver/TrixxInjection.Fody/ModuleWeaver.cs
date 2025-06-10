//#define ATTACH_DEBUG
#define DEBUG
using System;
using Fody;
using Mono.Cecil;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text;
using TrixxInjection.Framework.FileHandling;
using TrixxInjection.Framework.Attributes;
using static TrixxInjection.Framework.Config.Enums;

namespace TrixxInjection.Fody
{

    public class ModuleWeaver : BaseModuleWeaver
    {
        public override bool ShouldCleanReference => true;
        public ConfiguratorAttribute Configuration = new ConfiguratorAttribute();
        public static ModuleWeaver That;
        internal L L;
        internal SerialiseConfig SSC;
        internal AssemblyTypeMethodTree TrixxInjection_Framework_ExpressionTree;

        public ModuleWeaver()
        {
            That = this;
            SourceSerialiser.Weaver = this;
        }

        public override void Execute()
        {
#if DEBUG && ATTACH_DEBUG
            System.Diagnostics.Debugger.Launch();
#endif
            #region Configuration

            CopyFrameworkFiles();
            ResolveTIF();
            var derived = LoadConfiguratorAttribute(ModuleDefinition, out var conf);
            if (derived)
                Configuration = CreateConfigFromDictionary(conf);
            if (Configuration.GeneralBehaviour.HasFlag(GeneralBehaviours.Breakpointer))
            {
                    System.Diagnostics.Debugger.Launch();
            }

            L = new L(Configuration.GeneralBehaviour.HasFlag(GeneralBehaviours.DebugLogging) ? Logging.LogLevel.Debug : Logging.LogLevel.Off);
            L.W("Starting Weaving");
            #endregion
            #region Setup
            var sb = new StringBuilder();
            

            var ignoredItems = new List<string>();
            if (!Configuration.SourceSerialiseSettings.HasFlag(SourceSerialiseBehaviour.DoNotIgnoreItems))
            {
                ignoredItems = Configuration.ItemsToIgnore.ToList();
                if (!Configuration.SourceSerialiseSettings.HasFlag(SourceSerialiseBehaviour
                        .DoNotAlsoUseRecommendedDefaultIgnoredItems))
                    ignoredItems = ignoredItems.Concat(ConfiguratorAttribute.DefaultRecommendedItemsToIgnore).ToList();
            }

            var squishedItems = new List<string>();
            if (!Configuration.SourceSerialiseSettings.HasFlag(SourceSerialiseBehaviour.DoNotUseSquishedObjects))
            {
                squishedItems = Configuration.ObjectsToSquish.ToList();
                if (!Configuration.SourceSerialiseSettings.HasFlag(SourceSerialiseBehaviour
                        .DoNotAlsoUseRecommendedDefaultSquishedObjects))
                    squishedItems = squishedItems.Concat(ConfiguratorAttribute.DefaultRecommendedObjectsToSquish).ToList();
            }

            var aliases = new Dictionary<string, string>();
            if (!Configuration.SourceSerialiseSettings.HasFlag(SourceSerialiseBehaviour.DoNotUseAliases))
            {
                aliases = Configuration.AliasKeys.Zip(Configuration.AliasValues, (k, v) => new { k, v })
                    .ToDictionary(x => x.k, x => x.v);
                if (!Configuration.SourceSerialiseSettings.HasFlag(SourceSerialiseBehaviour
                        .DoNotAlsoUseRecommendedDefaultAliases))
                    ConfiguratorAttribute.DefaultRecommendedAliases.Select(kvp => (kvp.Key, kvp.Value)).ToList().ForEach(kvp => aliases.Add(kvp.Key, kvp.Value));
            }

            SSC = new SerialiseConfig()
            {
                Aliases = aliases,
                Events = (Configuration.SourceSerialiseSettings & SourceSerialiseBehaviour.SerialiseEvents) != 0,
                PFields =
                    (Configuration.SourceSerialiseSettings & SourceSerialiseBehaviour.SerialisePrivateFields) != 0,
                Fields = (Configuration.SourceSerialiseSettings & SourceSerialiseBehaviour.SerialiseFields) != 0,
                Properties =
                    (Configuration.SourceSerialiseSettings & SourceSerialiseBehaviour.SerialiseProperties) != 0,
                PrettyPrint = (Configuration.SourceSerialiseSettings & SourceSerialiseBehaviour.PrettyPrint) != 0,
                TypeCounting =
                    (Configuration.SourceSerialiseSettings & SourceSerialiseBehaviour.IncludeTypeCounts) != 0,
                IgnoredObjects = ignoredItems,
                SimplexObjects = new List<string> { nameof(Mono.Cecil.ModuleDefinition.Assembly) },
                SquashedObjects = squishedItems
            };
            #endregion
            #region Preweave SSing
            if (Configuration.SourceSerialisedTiming.HasFlag(SourceSerialisingTimingBehaviour.PreWeave))
            {
                try
                {
                    sb.AppendLine("START OF PRE WEAVE DIAGNOSTICS");
                    //x sb._($"{nameof(Instruction)}");
                    WriteInfo("Starting Weaver Diagnostics");
                    var a = ModuleDefinition.Assembly;

                    sb.AppendLine(
                        new SourceSerialiser().Serialise(
                            a, SSC
                        )
                    );
                    sb.AppendLine("END OF DIAGNOSTICS");
                }
                catch (WeavingException wex)
                {
                    WriteError(wex.Message);
                }
                catch (InvalidOperationException ex)
                {
                    WriteError(
                        $"An invalid operation occured: {ex.Message}   @   {ex.Source}   stacked with   {ex.StackTrace}");
                }
                catch (Exception ex)
                {
                    WriteError(
                        $"A {ex.GetType().Name} occured: {ex.Message}   @   {ex.Source}   stacked with   {ex.StackTrace}");
                }
                finally
                {
                    WriteInfo("PreWeave Serialisation Complete.");
                    TryAppendFileString(sb.ToString());
                    sb.Clear();
                }
            }
            #endregion
            #region Weaving
            Weaving.Weave();
            #endregion
            #region PostWeave SSing
            if (Configuration.SourceSerialisedTiming.HasFlag(SourceSerialisingTimingBehaviour.PreWeave))
            {
                try
                {
                    sb.AppendLine("START OF POST WEAVE DIAGNOSTICS");
                    //x sb._($"{nameof(Instruction)}");
                    WriteInfo("Starting Weaver Diagnostics");
                    var a = ModuleDefinition.Assembly;

                    sb.AppendLine(
                        new SourceSerialiser().Serialise(
                            a, SSC
                        )
                    );
                    sb.AppendLine("END OF DIAGNOSTICS");
                }
                catch (InvalidOperationException ex)
                {
                    WriteError(
                        $"An invalid operation occured: {ex.Message}   @   {ex.Source}   stacked with   {ex.StackTrace}");
                }
                catch (Exception ex)
                {
                    WriteError(
                        $"A {ex.GetType().Name} occured: {ex.Message}   @   {ex.Source}   stacked with   {ex.StackTrace}");
                }
                finally
                {
                    WriteInfo("Weaving Complete.");
                    TryAppendFileString(sb.ToString());
                    sb.Clear();
                }
            }
            #endregion

        }

        private void ResolveTIF()
        {
            TrixxInjection_Framework_ExpressionTree = new AssemblyTypeMethodTree(ModuleDefinition.AssemblyResolver.Resolve(
                ModuleWeaver.That.ModuleDefinition.AssemblyReferences.First(asm =>
                    asm.Name == "TrixxInjection.Framework")));
        }

        internal MethodReference Import(MethodDefinition mdef)
            => ModuleDefinition.ImportReference(mdef);

        internal MethodReference Import(MethodReference mdef)
            => ModuleDefinition.ImportReference(mdef);

        private void CopyFrameworkFiles()
        {
            var frameworkRef = ModuleDefinition
                .AssemblyReferences
                .FirstOrDefault(r => r.Name == "TrixxInjection.Framework");
            if (frameworkRef == null)
                throw new InvalidOperationException("No reference to TrixxInjection.Framework found.");

            var resolvedAsm = ModuleDefinition
                .AssemblyResolver
                .Resolve(frameworkRef);
            var sourceDll = resolvedAsm.MainModule.FileName;
            var sourceXml = Path.ChangeExtension(sourceDll, ".xml");

            var intermediateDir = Path.GetDirectoryName(ModuleDefinition.FileName);
            Directory.CreateDirectory(intermediateDir);

            var destDllIntermediate = Path.Combine(intermediateDir, Path.GetFileName(sourceDll));
            File.Copy(sourceDll, destDllIntermediate, overwrite: true);

            if (File.Exists(sourceXml))
            {
                var destXmlIntermediate = Path.Combine(intermediateDir, Path.GetFileName(sourceXml));
                File.Copy(sourceXml, destXmlIntermediate, overwrite: true);
            }

            var outputDir = Environment.GetEnvironmentVariable("OutDir")
                            ?? Environment.GetEnvironmentVariable("TargetDir");
            if (!string.IsNullOrWhiteSpace(outputDir))
            {
                Directory.CreateDirectory(outputDir);

                var destDllOutput = Path.Combine(outputDir, Path.GetFileName(sourceDll));
                File.Copy(sourceDll, destDllOutput, overwrite: true);

                if (File.Exists(sourceXml))
                {
                    var destXmlOutput = Path.Combine(outputDir, Path.GetFileName(sourceXml));
                    File.Copy(sourceXml, destXmlOutput, overwrite: true);
                }
            }
        }


        public override IEnumerable<string> GetAssembliesForScanning()
        {
            yield return "netstandard";
            yield return "mscorlib";
        }

        internal void TryAppendFileString(string content)
        {
            using (var writer = FileH.WriterFor(Configuration.LogFileName))
            {
                try
                {
                    writer.WriteNoTime(content);
                }
                catch (Exception ex)
                {
                    WriteError(
                        $"A {ex.GetType().Name} occured: {ex.Message}   @   {ex.Source}   stacked with   {ex.StackTrace}");
                }
            }
        }

        public static bool LoadConfiguratorAttribute(ModuleDefinition md, out Dictionary<string, object> conf)
        {
            conf = new Dictionary<string, object>();
            var ca = md.Assembly.CustomAttributes.FirstOrDefault(c => c.AttributeType.FullName == "TrixxInjection.Framework.Attributes.ConfiguratorAttribute");
            if (ca is null)
                return false;
            foreach (var p in ca.Properties)
            {
                var n = p.Name;
                var a = (object)p.Argument.Value;
                switch (n)
                {
                    case "LogFileName":
                        conf[n] = (string)a;
                        break;
                    case "ObjectsToSquish":
                        var oa = (CustomAttributeArgument[])a;
                        var ol = new List<string>();
                        foreach (var x in oa) ol.Add((string)x.Value);
                        conf[n] = ol;
                        break;
                    case "ItemsToIgnore":
                        var ia = (CustomAttributeArgument[])a;
                        var il = new List<string>();
                        foreach (var x in ia) il.Add((string)x.Value);
                        conf[n] = il;
                        break;
                    case "AliasKeys":
                        var ka = (CustomAttributeArgument[])a;
                        var kl = new List<string>();
                        foreach (var x in ka) kl.Add((string)x.Value);
                        conf[n] = kl;
                        break;
                    case "AliasValues":
                        var va = (CustomAttributeArgument[])a;
                        var vl = new List<string>();
                        foreach (var x in va) vl.Add((string)x.Value);
                        conf[n] = vl;
                        break;
                    case "SourceSerialisedTiming":
                        conf[n] = (SourceSerialisingTimingBehaviour)Convert.ToInt64(a);
                        break;
                    case "SourceSerialiseSettings":
                        conf[n] = (SourceSerialiseBehaviour)Convert.ToInt64(a);
                        break;
                    case "GeneralBehaviour":
                        conf[n] = (GeneralBehaviours)Convert.ToInt64(a);
                        break;
                    case "DEV__AttributesToBreakTo":
                        conf[n] = (AttributeBreaking)Convert.ToInt64(a);
                        break;
                }
            }
            return true;
        }

        public static ConfiguratorAttribute CreateConfigFromDictionary(IDictionary<string, object> values)
        {
            var instance = new ConfiguratorAttribute();
            foreach (var kvp in values)
            {
                var property = typeof(ConfiguratorAttribute).GetProperty(kvp.Key);
                if (property != null && property.CanWrite)
                {
                    var value = kvp.Value;
                    if (value != null && property.PropertyType.IsAssignableFrom(value.GetType()))
                        property.SetValue(instance, value);
                    else if (value != null)
                        property.SetValue(instance, Convert.ChangeType(value, property.PropertyType));
                }
            }
            return instance;
        }

        private string GetNamespace()
        {
            var namespaceFromConfig = GetNamespaceFromConfig();
            var namespaceFromAttribute = GetNamespaceFromAttribute();
            if (namespaceFromConfig != null && namespaceFromAttribute != null)
            {
                throw new WeavingException("Configuring namespace from both Config and Attribute is not supported.");
            }

            return namespaceFromAttribute ?? namespaceFromConfig;
        }

        public static void ValidateNamespace(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new WeavingException("Invalid namespace");
        }

        private string GetNamespaceFromConfig()
        {
            var attribute = Config?.Attribute("Namespace");
            if (attribute == null)
            {
                return null;
            }

            var value = attribute.Value;
            ValidateNamespace(value);
            return value;
        }

        private string GetNamespaceFromAttribute()
        {
            var attributes = ModuleDefinition.Assembly.CustomAttributes;
            var namespaceAttribute = attributes
                .SingleOrDefault(x => x.AttributeType.FullName == "NamespaceAttribute");
            if (namespaceAttribute == null)
            {
                return null;
            }

            attributes.Remove(namespaceAttribute);
            var value = (string)namespaceAttribute.ConstructorArguments.First().Value;
            ValidateNamespace(value);
            return value;
        }
    }


    internal class CustomAssemblyHandling : IDisposable
    {
        private string _consumingDirectory;

        private CustomAssemblyHandling() { }

        private Assembly Resolver(object sender, ResolveEventArgs args)
        {
            var name = new AssemblyName(args.Name).Name + ".dll";
            var candidate = Path.Combine(_consumingDirectory, name);
            return File.Exists(candidate)
                ? Assembly.LoadFrom(candidate)
                : null;
        }

        internal static CustomAssemblyHandling Enable(string consumerAssemblyPath)
        {
            var CAH = new CustomAssemblyHandling();
            CAH._consumingDirectory = Path.GetDirectoryName(consumerAssemblyPath);
            AppDomain.CurrentDomain.AssemblyResolve += CAH.Resolver;
            return CAH;
        }

        public void Dispose()
        {
            AppDomain.CurrentDomain.AssemblyResolve -= Resolver;
        }
    }
}