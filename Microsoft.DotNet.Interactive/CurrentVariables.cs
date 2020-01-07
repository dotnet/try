using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using Microsoft.DotNet.Interactive.Formatting;
using static Microsoft.DotNet.Interactive.Formatting.PocketViewTags;

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

        public CurrentVariables(Dictionary<string, CurrentVariable> variables, bool detailed)
            : this(detailed)
        {
            _variables = variables ?? throw new ArgumentNullException(nameof(variables));
        }

        private CurrentVariables(bool detailed)
        {
            Detailed = detailed;
        }

        public bool Detailed { get; }

        public IEnumerator<CurrentVariable> GetEnumerator() => _variables.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public static void RegisterFormatter()
        {
            Formatter<CurrentVariables>.Register((variables, writer) =>
            {
                PocketView output = null;

                if (variables.Detailed)
                {
                    output = table(
                        thead(
                            tr(
                                th("Variable"),
                                th("Type"),
                                th("Value"))),
                        tbody(
                            variables.Select(v =>
                                 tr(
                                     td(v.Name),
                                     td(v.Type),
                                     td(v.Value.ToDisplayString())
                                 ))));
                }
                else
                {
                    output = div(variables.Select(v => v.Name + "\t "));
                }

                output.WriteTo(writer, HtmlEncoder.Default);
            }, "text/html");
        }
    }
}
