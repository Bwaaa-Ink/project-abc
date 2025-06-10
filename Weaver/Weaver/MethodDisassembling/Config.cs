using TrixxInjection.Framework.Config;

[assembly: TrixxInjection.Framework.Attributes.Configurator(
    //GeneralBehaviour =
    //    //Enums.GeneralBehaviours.InjectDebugger 
    //    //               | 
    //    Enums.GeneralBehaviours.Breakpointer
    //,
    SourceSerialisedTiming = Enums.SourceSerialisingTimingBehaviour.None,
    SourceSerialiseSettings = Enums.SourceSerialiseBehaviour.PrettyPrint | Enums.SourceSerialiseBehaviour.IncludeTypeCounts |
                              Enums.SourceSerialiseBehaviour.SerialiseProperties | Enums.SourceSerialiseBehaviour.SerialiseFields,
    LogFileName = @"C:\Logs\BUILD_LATEST.log"
    //, DEV__AttributesToBreakTo = Enums.AttributeBreaking.Creation | Enums.AttributeBreaking.Deletion
)]
