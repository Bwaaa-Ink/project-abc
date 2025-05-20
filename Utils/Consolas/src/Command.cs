using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utilities.Console
{
    public sealed class Command(string name, Action<string> func, string description)
    {
        public string Name { get; init; } = name;
        public string Description { get; init; } = description;
        public Action<string> Func { get; init; } = func;

        public override string ToString()
            => Name + ": " + Description;

        public override bool Equals(object? obj)
            => obj is Command C ? string.Equals(Name, C.Name) : Equals(this, obj);
    }
}
