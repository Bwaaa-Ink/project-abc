using TrixxInjection;
using TrixxInjection.Config;
using static TrixxInjection.Config.Enums;

namespace MethodDisassembling
{
    internal class Config : Configurator
    {
        public override GeneralBehaviours GeneralBehaviour { get; } =
            GeneralBehaviours.DebugLogging | GeneralBehaviours.Breakpointer;

        public override SourceSerialisingTimingBehaviour SourceSerialisedTiming { get; } =
            SourceSerialisingTimingBehaviour.WrappedWeave;

        public override SourceSerialiseBehaviour SourceSerialiseSettings { get; } =
            SourceSerialiseBehaviour.PrettyPrint | SourceSerialiseBehaviour.IncludeTypeCounts |
            SourceSerialiseBehaviour.SerialiseProperties | SourceSerialiseBehaviour.SerialiseFields;
    }
}
