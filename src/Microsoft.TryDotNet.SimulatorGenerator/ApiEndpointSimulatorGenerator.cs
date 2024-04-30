// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.CSharpProject;
using Microsoft.DotNet.Interactive.CSharpProject.Commands;
using Microsoft.TryDotNet.Tests;

namespace Microsoft.TryDotNet.SimulatorGenerator;

public class ApiEndpointSimulatorGenerator
{
    static ApiEndpointSimulatorGenerator() 
    {
        CSharpProjectKernel.RegisterEventsAndCommands();
    }

    public static async Task CreateScenarioFiles(DirectoryInfo destinationFolder)
    {
        foreach (var apiContractScenario in ApiContractScenarios())
        {
            var simulatorConfiguration = await ExecuteScenario(apiContractScenario);

            var filename = Path.Combine(destinationFolder.FullName, apiContractScenario.Label + ".json");
            Console.WriteLine($"Creating configuration for scenario {apiContractScenario.Label} at '{filename}'");
            await File.WriteAllTextAsync(filename,simulatorConfiguration);
        }
    }

    private static IEnumerable<ApiContractScenario> ApiContractScenarios()
    {
        {
            yield return new ApiContractScenario(
                "open_project",
                new[]
                {
                    new[]
                    {
                        new OpenProject(new Project(new[]
                        {
                            new ProjectFile("./Program.cs",
                                @"
public class Program
{
    public static void Main(string[] args)
    {
        #region REGION_1
        var a = 123;
        #endregion

        #region REGION_2
        var b = 123;
        #endregion
    }
}")
                        }))
                    }
                }
            );

            yield return new ApiContractScenario(
                "open_document",
                new[]
                {
                    new[]
                    {
                        new OpenProject(new Project(new[]
                        {
                            new ProjectFile("./Program.cs",
                                @"
public class Program
{
    public static void Main(string[] args)
    {
        #region REGION_1
        var a = 123;
        #endregion

        #region REGION_2
        var b = 123;
        #endregion
    }
}")
                        }))
                    },
                    new KernelCommand[]
                    {
                        new OpenProject(new Project(new[]
                        {
                            new ProjectFile("./Program.cs",
                                @"
public class Program
{
    public static void Main(string[] args)
    {
        #region REGION_1
        var a = 123;
        #endregion

        #region REGION_2
        var b = 123;
        #endregion
    }
}")
                        })),
                        new OpenDocument("./Program.cs")
                    }
                }
            );

            yield return new ApiContractScenario(
                "open_document_with_region",
                new[]
                {
                    new[]
                    {
                        new OpenProject(new Project(new[]
                        {
                            new ProjectFile("./Program.cs",
                                @"
public class Program
{
    public static void Main(string[] args)
    {
        #region REGION_1
        var a = 123;
        #endregion

        #region REGION_2
        var b = 123;
        #endregion
    }
}")
                        }))
                    },
                    new KernelCommand[]
                    {
                        new OpenProject(new Project(new[]
                        {
                            new ProjectFile("./Program.cs",
                                @"
public class Program
{
    public static void Main(string[] args)
    {
        #region REGION_1
        var a = 123;
        #endregion

        #region REGION_2
        var b = 123;
        #endregion
    }
}")
                        })),
                        new OpenDocument("./Program.cs", "REGION_2")
                    }
                }
            );

            yield return new ApiContractScenario(
                "compiles_with_no_warning",
                new[]
                {
                    new KernelCommand[]
                    {
                        new OpenProject(new Project(new[]
                        {
                            new ProjectFile("Program.cs", @"
public class Program
{
    public static void Main(string[] args)
    {
        #region test-region
        #endregion
    }
}
")
                        }))
                    },
                    new KernelCommand[]
                    {
                        new OpenProject(new Project(new[]
                        {
                            new ProjectFile("Program.cs", @"
public class Program
{
    public static void Main(string[] args)
    {
        #region test-region
        #endregion
    }
}
")
                        })),
                        new OpenDocument("Program.cs", regionName: "test-region")
                    },
                    new KernelCommand[]
                    {
                        new OpenProject(new Project(new[]
                        {
                            new ProjectFile("Program.cs", @"
public class Program
{
    public static void Main(string[] args)
    {
        #region test-region
        #endregion
    }
}
")
                        })),
                        new OpenDocument("Program.cs", regionName: "test-region"),
                        new SubmitCode("System.Console.WriteLine(2);"),
                        new CompileProject()
                    }
                }
            );

            yield return new ApiContractScenario(
                "compiles_with_error",
                new[]
                {
                    new KernelCommand[]
                    {
                        new OpenProject(new Project(new[]
                        {
                            new ProjectFile("Program.cs", @"
public class Program
{
    public static void Main(string[] args)
    {
        int someInt = 1;
        #region test-region
        #endregion
    }
}
")
                        }))
                    },
                    new KernelCommand[]
                    {
                        new OpenProject(new Project(new[]
                        {
                            new ProjectFile("Program.cs", @"
public class Program
{
    public static void Main(string[] args)
    {
        int someInt = 1;
        #region test-region
        #endregion
    }
}
")
                        })),
                        new OpenDocument("Program.cs", regionName: "test-region")
                    },
                    new KernelCommand[]
                    {
                        new OpenProject(new Project(new[]
                        {
                            new ProjectFile("Program.cs", @"
public class Program
{
    public static void Main(string[] args)
    {
        int someInt = 1;
        #region test-region
        #endregion
    }
}
")
                        })),
                        new OpenDocument("Program.cs", regionName: "test-region"),
                        new SubmitCode("someInt = \"NaN\";"),
                        new CompileProject()
                    }
                }
            );

            var markedCode = @"fileInfo.$$";
            MarkupTestFile.GetLineAndColumn(markedCode, out var code, out var line, out var character);
            yield return new ApiContractScenario(
                "completions_produced_from_regions",
                new[]
                {
                    new KernelCommand[]
                    {
                        new OpenProject(new Project(new[]
                        {
                            new ProjectFile("Program.cs", @"
public class Program
{
    public static void Main(string[] args)
    {
        var fileInfo = new System.IO.FileInfo(""test.file"");
        #region TEST_REGION
        #endregion
    }
}
")
                        }))
                    },
                    new KernelCommand[]
                    {
                        new OpenProject(new Project(new[]
                        {
                            new ProjectFile("Program.cs", @"
public class Program
{
    public static void Main(string[] args)
    {
        var fileInfo = new System.IO.FileInfo(""test.file"");
        #region TEST_REGION
        #endregion
    }
}
")
                        })),
                        new OpenDocument("Program.cs", regionName: "TEST_REGION")
                    },

                    new KernelCommand[]
                    {
                        new OpenProject(new Project(new[]
                        {
                            new ProjectFile("Program.cs", @"
public class Program
{
    public static void Main(string[] args)
    {
        var fileInfo = new System.IO.FileInfo(""test.file"");
        #region TEST_REGION
        #endregion
    }
}
")
                        })),
                        new OpenDocument("Program.cs", regionName: "TEST_REGION"),
                        new RequestCompletions(code, new LinePosition(line, character))
                    }
                }

            );

            yield return new ApiContractScenario(
                "diagnostics_produced_with_errors_in_code",
                new[]
                {
                    new KernelCommand[]
                    {
                        new OpenProject(new Project(new[]
                        {
                            new ProjectFile("Program.cs", @"
public class Program
{
    public static void Main(string[] args)
    {
        int someInt = 1;
        #region test-region
        #endregion
    }
}
")
                        }))
                    },
                    new KernelCommand[]
                    {
                        new OpenProject(new Project(new[]
                        {
                            new ProjectFile("Program.cs", @"
public class Program
{
    public static void Main(string[] args)
    {
        int someInt = 1;
        #region test-region
        #endregion
    }
}
")
                        })),
                        new OpenDocument("Program.cs", regionName: "test-region")
                    },
                    new KernelCommand[]
                    {
                        new OpenProject(new Project(new[]
                        {
                            new ProjectFile("Program.cs", @"
public class Program
{
    public static void Main(string[] args)
    {
        int someInt = 1;
        #region test-region
        #endregion
    }
}
")
                        })),
                        new OpenDocument("Program.cs", regionName: "test-region"),
                        new RequestDiagnostics("someInt = \"NaN\";")
                    }
                }
            );

            yield return new ApiContractScenario(
                "diagnostics_produced_with_hidden_severity",
                new[]
                {
                    new KernelCommand[]
                    {
                        new OpenProject(new Project(new[]
                        {
                            new ProjectFile("Program.cs", @"
public class Program
{
    public static void Main(string[] args)
    {
        int someInt = 1;
        #region test-region
        #endregion
    }
}
")
                        }))
                    },
                    new KernelCommand[]
                    {
                        new OpenProject(new Project(new[]
                        {
                            new ProjectFile("Program.cs", @"
public class Program
{
    public static void Main(string[] args)
    {
        int someInt = 1;
        #region test-region
        #endregion
    }
}
")
                        })),
                        new OpenDocument("Program.cs", regionName: "test-region")
                    },
                    new KernelCommand[]
                    {
                        new OpenProject(new Project(new[]
                        {
                            new ProjectFile("Program.cs", @"
using System.Linq;
using System;

public class Program
{
    public static void Main(string[] args)
    {
        int someInt = 1;
        #region test-region
        #endregion
    }
}
")
                        })),
                        new OpenDocument("Program.cs", regionName: "test-region"),
                        new RequestDiagnostics("someInt = 4;")
                    }
                }
            );

            yield return new ApiContractScenario(
                "update_project",
                new[]
                {
                    new KernelCommand[]
                    {
                        new OpenProject(new Project(new[]
                        {
                            new ProjectFile("program.cs", @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;
namespace Program {
    class Program {
        static void Main(string[] args){
            #region controller
            #endregion
        }
    }
}")}))
                    },
                    new KernelCommand[]
                    {
                        new OpenProject(new Project(new[]
                        {
                            new ProjectFile("program.cs", @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;
namespace Program {
    class Program {
        static void Main(string[] args){
            #region controller
            #endregion
        }
    }
}")})),
                        new OpenDocument("program.cs", "controller")
                    },
                    new KernelCommand[]
                    {
                        new OpenProject(new Project(new[]
                        {
                            new ProjectFile("program.cs", @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;
namespace Program {
    class Program {
        static void Main(string[] args){
            #region controller
            Console.WriteLine(123);
            #endregion
        }
    }
}")}))
                    },
                    new KernelCommand[]
                    {
                        new OpenProject(new Project(new[]
                        {
                            new ProjectFile("program.cs", @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;
namespace Program {
    class Program {
        static void Main(string[] args){
            #region controller
            Console.WriteLine(123);
            #endregion
        }
    }
}")})),
                        new OpenDocument("program.cs", "controller")
                    }

                });
        }

    }


    private static async Task<string> ExecuteScenario(ApiContractScenario scenario)
    {
        await using var applicationBuilderFactory = new WebApplicationFactory<Program>();

        var c = applicationBuilderFactory.CreateDefaultClient();
        var transcript = new
        {
            requests = new List<object>()
        };

        foreach (var commandBatch in scenario.CommandBatches)
        {
            var request = new
            {
                commands = commandBatch.Select(KernelCommandEnvelope.Create).Select(e => e.ToJsonElement())
            };
            
            var requestBody = JsonContent.Create(request);
            
            var response = await c.PostAsync("commands", requestBody);

            var responseJson = JsonDocument.Parse(await response.Content.ReadAsStringAsync(CancellationToken.None)).RootElement;

            transcript.requests.Add(new
            {
                commands = request.commands,
                events = responseJson.GetProperty("events")
            });
        }

        var options = new JsonSerializerOptions(JsonSerializerDefaults.General)
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        return JsonSerializer.Serialize(transcript, options).Fixed();
    }
}