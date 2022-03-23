// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using Assent;
using FluentAssertions;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharpProject;
using Microsoft.DotNet.Interactive.CSharpProject.Commands;
using Microsoft.DotNet.Interactive.Server;

using Xunit;

namespace Microsoft.TryDotNet.Tests;

public class ApiEndpointContractTests
{
    
    static ApiEndpointContractTests()
    {
        CSharpProjectKernel.RegisterEventsAndCommands();
    }
    
    public static IEnumerable<object[]> ApiContractScenarios()
    {
        foreach (var apiContractScenario in scenarios())
        {
            yield return new object[] {apiContractScenario};
        }

        IEnumerable<ApiContractScenario> scenarios()
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
                            new OpenProject(new Project(new[] { new ProjectFile("Program.cs", @"
public class Program
{
    public static void Main(string[] args)
    {
        #region test-region
        #endregion
    }
}
") }))
                        },
                        new KernelCommand[]
                        {
                            new OpenProject(new Project(new[] { new ProjectFile("Program.cs", @"
public class Program
{
    public static void Main(string[] args)
    {
        #region test-region
        #endregion
    }
}
") })),
                            new OpenDocument("Program.cs", regionName: "test-region")
                        },
                        new KernelCommand[]
                        {
                            new OpenProject(new Project(new[] { new ProjectFile("Program.cs", @"
public class Program
{
    public static void Main(string[] args)
    {
        #region test-region
        #endregion
    }
}
") })),
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
                        new OpenProject(new Project(new[] { new ProjectFile("Program.cs", @"
public class Program
{
    public static void Main(string[] args)
    {
        int someInt = 1;
        #region test-region
        #endregion
    }
}
") }))
                    },
                    new KernelCommand[]
                    {
                        new OpenProject(new Project(new[] { new ProjectFile("Program.cs", @"
public class Program
{
    public static void Main(string[] args)
    {
        int someInt = 1;
        #region test-region
        #endregion
    }
}
") })),
                        new OpenDocument("Program.cs", regionName: "test-region")
                    },
                    new KernelCommand[]
                    {
                        new OpenProject(new Project(new[] { new ProjectFile("Program.cs", @"
public class Program
{
    public static void Main(string[] args)
    {
        int someInt = 1;
        #region test-region
        #endregion
    }
}
") })),
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
                new []
                {
                    new KernelCommand[]
                    {
                        new OpenProject(new Project(new[] { new ProjectFile("Program.cs", @"
public class Program
{
    public static void Main(string[] args)
    {
        var fileInfo = new System.IO.FileInfo(""test.file"");
        #region TEST_REGION
        #endregion
    }
}
") }))
                    },
                    new KernelCommand[]
                    {
                        new OpenProject(new Project(new[] { new ProjectFile("Program.cs", @"
public class Program
{
    public static void Main(string[] args)
    {
        var fileInfo = new System.IO.FileInfo(""test.file"");
        #region TEST_REGION
        #endregion
    }
}
") })),
                        new OpenDocument("Program.cs", regionName: "TEST_REGION")
                    },

                    new KernelCommand[]
                    {
                        new OpenProject(new Project(new[] { new ProjectFile("Program.cs", @"
public class Program
{
    public static void Main(string[] args)
    {
        var fileInfo = new System.IO.FileInfo(""test.file"");
        #region TEST_REGION
        #endregion
    }
}
") })),
                        new OpenDocument("Program.cs", regionName: "TEST_REGION"),
                        new RequestCompletions(code, new LinePosition(line, character))
                    }
                }

            );

            yield return new ApiContractScenario(
                "diagnostics_produced_with_errors_in_code",
                new []
                {
                    new KernelCommand[]
                    {
                        new OpenProject(new Project(new[] { new ProjectFile("Program.cs", @"
public class Program
{
    public static void Main(string[] args)
    {
        int someInt = 1;
        #region test-region
        #endregion
    }
}
") }))
                    },
                    new KernelCommand[]
                    {
                        new OpenProject(new Project(new[] { new ProjectFile("Program.cs", @"
public class Program
{
    public static void Main(string[] args)
    {
        int someInt = 1;
        #region test-region
        #endregion
    }
}
") })),
                        new OpenDocument("Program.cs", regionName: "test-region")
                    },
                    new KernelCommand[]
                    {
                        new OpenProject(new Project(new[] { new ProjectFile("Program.cs", @"
public class Program
{
    public static void Main(string[] args)
    {
        int someInt = 1;
        #region test-region
        #endregion
    }
}
") })),
                        new OpenDocument("Program.cs", regionName: "test-region"),
                        new RequestDiagnostics("someInt = \"NaN\";")
                    }
                }
            );
        }
    }

    [Theory]
    [MemberData(nameof(ApiContractScenarios))]
    public async Task ContractIsNotBroken(ApiContractScenario scenario)
    {
        var configuration = new Configuration()
            .UsingExtension($"{scenario.Label}.json")
            .SetInteractive(Debugger.IsAttached);
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

            response.StatusCode.Should().Be(HttpStatusCode.OK);

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

        this.Assent(JsonSerializer.Serialize(transcript, options).Fixed(), configuration);
    }
}

public static class StringExtensions
{
    public static string FixedToken(this string source)
    {
        return Regex.Replace(source, @"""token""\s*:\s*""(?<token>([^""\\]|(\\.))*)""", @"""token"": ""command-token""", RegexOptions.IgnoreCase);
    }

    public static string FixedId(this string source)
    {
        return Regex.Replace(source, @"""id""\s*:\s*""(?<id>([^""\\]|(\\.))*)""", @"""id"": ""command-id""", RegexOptions.IgnoreCase);
    }

    public static string FixedNewLine(this string source)
    {
        return Regex.Replace(source, @"\\r\\n", @"\n");
    }

    public static string FixedAssembly(this string source)
    {
        var r = new Regex(@"(?<start>""assembly""\s*:\s*\{\s*""value""\s*:\s*"")(?<value>([^""\\]|(\\.))*)(?<end>""\s*\}\s*)",
             RegexOptions.Multiline| RegexOptions.IgnoreCase);
        var m = r.Matches(source);
        return Regex.Replace(source, @"(?<start>""assembly""\s*:\s*\{\s*""value""\s*:\s*"")(?<value>([^""\\]|(\\.))*)(?<end>""\s*\}\s*)", "${start}AABBCC${end}", RegexOptions.Multiline);
    }


    public static string Fixed(this string source)
    {
        return source.FixedId().FixedToken().FixedNewLine().FixedAssembly();
    }
}

public record ApiContractScenario(string Label, KernelCommand[][] CommandBatches);