using TrixxInjection.Config;

[assembly: TrixxInjection.Framework.Attributes.Configurator(
    GeneralBehaviour = Enums.GeneralBehaviours.DebugLogging,
    SourceSerialisedTiming = Enums.SourceSerialisingTimingBehaviour.None,
    SourceSerialiseSettings = Enums.SourceSerialiseBehaviour.PrettyPrint | Enums.SourceSerialiseBehaviour.IncludeTypeCounts |
                              Enums.SourceSerialiseBehaviour.SerialiseProperties | Enums.SourceSerialiseBehaviour.SerialiseFields,
    LogFileName = @"C:\Logs\BUILD_LATEST.log"
)]
