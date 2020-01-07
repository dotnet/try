using System.Collections;
using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive
{
    public class CurrentVariables : IEnumerable<CurrentVariable>
    {
        private readonly Dictionary<string, CurrentVariable> _variables = new Dictionary<string, CurrentVariable>();

        public CurrentVariables(IEnumerable<CurrentVariable> variables, bool detailed)
            : this(detailed)
        {
            foreach (var variable in variables)
            {
                _variables[variable.Name] = variable;
            }
        }

        private CurrentVariables(bool detailed)
        {
            Detailed = detailed;
        }

        public bool Detailed { get; }

        public IEnumerator<CurrentVariable> GetEnumerator() => _variables.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
