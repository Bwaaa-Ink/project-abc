using TrixxInjection;
using TrixxInjection.Config;

namespace MethodDisassembling
{
    internal class Config : Configurator
    {
        public override Enums.GeneralBehaviours GeneralBehaviour { get; } =
            Enums.GeneralBehaviours.DebugLogging;
    }
}
