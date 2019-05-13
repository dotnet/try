// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Try.Protocol;
using Recipes;
using Xunit;
using Xunit.Abstractions;

namespace MLS.Agent.Tests.ApiContracts
{
    public class ApiInputContractChangeTests : ApiViaHttpTestsBase
    {
        public ApiInputContractChangeTests(ITestOutputHelper output) : base(output)
        {
        }

        [Theory]
        [InlineData("console")]
        [InlineData("script")]
        public async Task Changing_the_completion_request_format_does_not_change_the_response(string workspaceType)
        {
            var oldFormatRequest =
                $@"{{
  ""workspace"": {{
    ""workspaceType"": ""{workspaceType}"",
    ""files"": [],
    ""buffers"": [
      {{
        ""id"": """",
        ""content"": ""using System;\nusing System.Collections.Generic;\nusing System.Linq;\n\npublic class Program\n{{\n  public static void Main()\n  {{\n    foreach (var i in Fibonacci().Take(20))\n    {{\n      Console.\n    }}\n  }}\n\n  private static IEnumerable<int> Fibonacci()\n  {{\n    int current = 1, next = 1;\n\n    while (true) \n    {{\n      yield return current;\n      next = current + (current = next);\n    }}\n  }}\n}}\n"",
        ""position"": 0
      }}
    ],
    ""usings"": []
  }},
  ""activeBufferId"": """",
  ""position"": 187
}}";

            var newFormatRequest =
                $@"{{
  ""workspace"": {{
    ""workspaceType"": ""{workspaceType}"",
    ""files"": [],
    ""buffers"": [
      {{
        ""id"": """",
        ""content"": ""using System;\nusing System.Collections.Generic;\nusing System.Linq;\n\npublic class Program\n{{\n  public static void Main()\n  {{\n    foreach (var i in Fibonacci().Take(20))\n    {{\n      Console.\n    }}\n  }}\n\n  private static IEnumerable<int> Fibonacci()\n  {{\n    int current = 1, next = 1;\n\n    while (true) \n    {{\n      yield return current;\n      next = current + (current = next);\n    }}\n  }}\n}}\n"",
        ""position"": 187
      }}
    ],
    ""usings"": []
  }},
  ""activeBufferId"": """"
}}";

            var responseToOldFormatRequest = await CallCompletion(oldFormatRequest);
            var responseToNewFormatRequest = await CallCompletion(newFormatRequest);

            responseToNewFormatRequest.Should().BeSuccessful();

            responseToNewFormatRequest.Should().BeEquivalentTo(responseToOldFormatRequest);

            var resultOfOldFormatRequest = await responseToOldFormatRequest.DeserializeAs<CompletionResult>();
            var resultOfNewFormatRequest = await responseToNewFormatRequest.DeserializeAs<CompletionResult>();

            resultOfNewFormatRequest.Items.Should().Contain(i => i.DisplayText == "WriteLine");

            resultOfOldFormatRequest.Should().BeEquivalentTo(resultOfNewFormatRequest);
        }

        [Theory]
        [InlineData("console")]
        [InlineData("script")]
        public async Task Changing_the_signature_help_request_format_does_not_change_the_response(string workspaceType)
        {
            var oldFormatRequest =
                $@"{{
  ""workspace"": {{
    ""workspaceType"": ""{workspaceType}"",
    ""files"": [],
    ""buffers"": [
      {{
        ""id"": """",
        ""content"": ""using System;\nusing System.Collections.Generic;\nusing System.Linq;\n\npublic class Program\n{{\n  public static void Main()\n  {{\n    foreach (var i in Fibonacci().Take(20))\n    {{\n      Console.WriteLine()\n    }}\n  }}\n\n  private static IEnumerable<int> Fibonacci()\n  {{\n    int current = 1, next = 1;\n\n    while (true) \n    {{\n      yield return current;\n      next = current + (current = next);\n    }}\n  }}\n}}\n"",
        ""position"": 0
      }}
    ],
    ""usings"": []
  }},
  ""activeBufferId"": """",
  ""position"": 197
}}";

            var newFormatRequest =
                $@"{{
  ""workspace"": {{
    ""workspaceType"": ""{workspaceType}"",
    ""files"": [],
    ""buffers"": [
      {{
        ""id"": """",
        ""content"": ""using System;\nusing System.Collections.Generic;\nusing System.Linq;\n\npublic class Program\n{{\n  public static void Main()\n  {{\n    foreach (var i in Fibonacci().Take(20))\n    {{\n      Console.WriteLine()\n    }}\n  }}\n\n  private static IEnumerable<int> Fibonacci()\n  {{\n    int current = 1, next = 1;\n\n    while (true) \n    {{\n      yield return current;\n      next = current + (current = next);\n    }}\n  }}\n}}\n"",
        ""position"": 197
      }}
    ],
    ""usings"": []
  }},
  ""activeBufferId"": """"
}}";

            var responseToOldFormatRequest = await CallSignatureHelp(oldFormatRequest);
            var responseToNewFormatRequest = await CallSignatureHelp(newFormatRequest);

            responseToNewFormatRequest.Should().BeSuccessful();

            responseToNewFormatRequest.Should().BeEquivalentTo(responseToOldFormatRequest);

            var resultOfOldFormatRequest = await responseToOldFormatRequest.DeserializeAs<SignatureHelpResult>();
            var resultOfNewFormatRequest = await responseToNewFormatRequest.DeserializeAs<SignatureHelpResult>();

            resultOfNewFormatRequest.Signatures.Should().Contain(i => i.Label == "void Console.WriteLine()");

            resultOfOldFormatRequest.Should().BeEquivalentTo(resultOfNewFormatRequest);
        }

        [Theory]
        [InlineData("console")]
        [InlineData("script")]
        public async Task Changing_the_run_request_format_does_not_change_the_response(string workspaceType)
        {
            var oldFormatRequest =
                $@"{{
  ""workspace"": {{
    ""workspaceType"": ""{workspaceType}"",
    ""files"": [],
    ""buffers"": [
      {{
        ""id"": ""Program.cs"",
        ""content"": ""using System;\nusing System.Collections.Generic;\nusing System.Linq;\n\npublic class Program\n{{\n  public static void Main()\n  {{\n    foreach (var i in Fibonacci().Take(10))\n    {{\n      Console.WriteLine(i);\n    }}\n  }}\n\n  private static IEnumerable<int> Fibonacci()\n  {{\n    int current = 1, next = 1;\n\n    while (true) \n    {{\n      yield return current;\n      next = current + (current = next);\n    }}\n  }}\n}}\n"",
        ""position"": 0
      }}
    ],
    ""usings"": []
  }},
  ""activeBufferId"": """",
  ""position"": 197
}}";

            var newFormatRequest =
                $@"{{
  ""workspace"": {{
    ""workspaceType"": ""{workspaceType}"",
    ""files"": [],
    ""buffers"": [
      {{
        ""id"": ""Program.cs"",
        ""content"": ""using System;\nusing System.Collections.Generic;\nusing System.Linq;\n\npublic class Program\n{{\n  public static void Main()\n  {{\n    foreach (var i in Fibonacci().Take(10))\n    {{\n      Console.WriteLine(i);\n    }}\n  }}\n\n  private static IEnumerable<int> Fibonacci()\n  {{\n    int current = 1, next = 1;\n\n    while (true) \n    {{\n      yield return current;\n      next = current + (current = next);\n    }}\n  }}\n}}\n"",
        ""position"": 197
      }}
    ],
    ""usings"": []
  }},
  ""activeBufferId"": """"
}}";

            var responseToOldFormatRequest = await CallRun(oldFormatRequest);
            var responseToNewFormatRequest = await CallRun(newFormatRequest);

            responseToNewFormatRequest.Should().BeSuccessful();

            responseToNewFormatRequest.Should().BeEquivalentTo(responseToOldFormatRequest);

            var resultOfOldFormatRequest = await responseToOldFormatRequest.DeserializeAs<RunResult>();
            var resultOfNewFormatRequest = await responseToNewFormatRequest.DeserializeAs<RunResult>();

            resultOfNewFormatRequest.Output.Should().BeEquivalentTo(
                "1",
                "1",
                "2",
                "3",
                "5",
                "8",
                "13",
                "21",
                "34",
                "55",
                "");

            resultOfOldFormatRequest.Should().BeEquivalentTo(resultOfNewFormatRequest);
        }
    }
}
