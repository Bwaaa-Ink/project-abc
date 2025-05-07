//#define ATTACH_DEBUG
#define DEBUG

using System;
using Fody;
using Mono.Cecil.Rocks;
using Mono.Cecil;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text;
using Mono.Cecil.Cil;
using TrixxInjection.Config;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using static TrixxInjection.Config.Enums;

namespace TrixxInjection.Fody
{

    public class ModuleWeaver : BaseModuleWeaver
    {
        public override bool ShouldCleanReference => true;
        public Config.Configurator Configuration = new Configurator();

        public ModuleWeaver()
        {
            SourceSerialiser.Weaver = this;
        }

        public override void Execute()
        {
#if DEBUG && ATTACH_DEBUG
            System.Diagnostics.Debugger.Launch();
#endif
            var (derived, conf) = LoadConfigurator(ModuleDefinition);
            if (derived)
                ParseConfiguration(conf);
            if (Configuration.GeneralBehaviour.HasFlag(GeneralBehaviours.Breakpointer))
            {
                if (System.Diagnostics.Debugger.IsAttached)
                    System.Diagnostics.Debugger.Launch();
                else
                    System.Diagnostics.Debugger.Break();
            }

            var L = new L(Configuration.GeneralBehaviour.HasFlag(GeneralBehaviours.DebugLogging) ? Logging.LogLevel.Debug : Logging.LogLevel.Off);
            L.W("Starting Weaving");
            #region Setup
            var sb = new StringBuilder();


            var ignoredItems = new List<string>();
            if (!Configuration.SourceSerialiseSettings.HasFlag(SourceSerialiseBehaviour.DoNotIgnoreItems))
            {
                ignoredItems = Configuration.ItemsToIgnore;
                if (!Configuration.SourceSerialiseSettings.HasFlag(SourceSerialiseBehaviour
                        .DoNotAlsoUseRecommendedDefaultIgnoredItems))
                    ignoredItems = ignoredItems.Concat(Configurator.DefaultRecommendedItemsToIgnore).ToList();
            }

            var squishedItems = new List<string>();
            if (!Configuration.SourceSerialiseSettings.HasFlag(SourceSerialiseBehaviour.DoNotUseSquishedObjects))
            {
                squishedItems = Configuration.ObjectsToSquish;
                if (!Configuration.SourceSerialiseSettings.HasFlag(SourceSerialiseBehaviour
                        .DoNotAlsoUseRecommendedDefaultSquishedObjects))
                    squishedItems = squishedItems.Concat(Configurator.DefaultRecommendedObjectsToSquish).ToList();
            }

            var aliases = new Dictionary<string, string>();
            if (!Configuration.SourceSerialiseSettings.HasFlag(SourceSerialiseBehaviour.DoNotUseAliases))
            {
                aliases = Configuration.Aliases;
                if (!Configuration.SourceSerialiseSettings.HasFlag(SourceSerialiseBehaviour
                        .DoNotAlsoUseRecommendedDefaultAliases))
                    Configurator.DefaultRecommendedAliases.Select(kvp => (kvp.Key, kvp.Value)).ToList().ForEach(kvp => aliases.Add(kvp.Key, kvp.Value));
            }


            #endregion
            if (Configuration.SourceSerialisedTiming.HasFlag(SourceSerialisingTimingBehaviour.PreWeave))
            {
                try
                {
                    sb.AppendLine("START OF DIAGNOSTICS");
                    //x sb._($"{nameof(Instruction)}");
                    WriteInfo("Starting Weaver Diagnostics");
                    var a = ModuleDefinition.Assembly;

                    sb.AppendLine(
                        new SourceSerialiser().Serialise(
                            a, ignoredItems,
                            new List<string> { nameof(Mono.Cecil.ModuleDefinition.Assembly) },
                            squishedItems,
                            aliases
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
                    WriteInfo("Weaving Complete.");
                    TryAppendFileString(sb.ToString());
                    sb.Clear();
                }
            }



            try
            {
                File.WriteAllText("C:/Logs/Diagnostic_Test.txt", sb.ToString());
            }
            catch (Exception ex)
            {
                WriteError(
                    $"A {ex.GetType().Name} occured: {ex.Message}   @   {ex.Source}   stacked with   {ex.StackTrace}");
            }
        }

        public override IEnumerable<string> GetAssembliesForScanning()
        {
            yield return "netstandard";
            yield return "mscorlib";
        }

        private void TryAppendFileString(string content)
        {
            try
            {
                File.AppendAllText("C:/Logs/Diagnostic_Test.txt", content);
            }
            catch (Exception ex)
            {
                WriteError(
                    $"A {ex.GetType().Name} occured: {ex.Message}   @   {ex.Source}   stacked with   {ex.StackTrace}");
            }
        }

        public static (bool, Dictionary<string, object>) LoadConfigurator(ModuleDefinition md)
        {
            var path = md.FileName;

            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return (false, null);
            var derived = md.Types.FirstOrDefault(t => t?.BaseType != null && t.BaseType.FullName == "TrixxInjection.Config.Configurator" && !t.IsAbstract);

            if (derived == null)
            {
#if DEBUG
                var message = "THISISASYMBOL - Derived Classes base Types in executing module\n" + string.Join(",\n",
                    md.Types.Where(t => t.BaseType != null)
                        .Select(t => $"{t.Name}: {t.BaseType.FullName} ({t.BaseType.Name})"));
                SourceSerialiser.Weaver.WriteInfo(message);
#endif
                return (false, null);
            }

            Dictionary<string, object> dict;
            using (var cah = CustomAssemblyHandling.Enable(path))
            {
                var assembly = Assembly.LoadFrom(path);
                var resolvedType = assembly.GetType(derived.FullName, true);
                var instance = Activator.CreateInstance(resolvedType ?? throw new WeavingException(
                    "Failed to resolve config type"
                ));
                dict = resolvedType
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.GetMethod.IsVirtual && !p.GetMethod.IsStatic)
                    .ToDictionary(p => p.Name, p => p.GetValue(instance));
            }

            return (true, dict);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Properties"></param>
        private void ParseConfiguration(Dictionary<string, object> Properties)
        {

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
#if DEBUG
            
#endif
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