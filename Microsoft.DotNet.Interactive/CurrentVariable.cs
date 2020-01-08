using System;

namespace Microsoft.DotNet.Interactive
{
    public class CurrentVariable
    {
        public CurrentVariable(string name, Type type, object value)
        {
            Name = name;
            Type = type;
            Value = value;
        }

        public object Value { get; }

        public Type Type { get; }

        public string Name { get; }
    }
}
