using System.Net;
using System.Text;
using Newtonsoft.Json;
using Peaky;

namespace Microsoft.TryDotNet.PeakyTests;

public class AgentTests : IPeakyTest, IApplyToApplication
{
    private readonly HttpClientWithTelemetry _httpClient;

    public AgentTests(HttpClientWithTelemetry httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task Valid_script_code_sent_to_run_endpoint_returns_200()
    {
        await RunCode(
            "/workspace/run",
            "Console.WriteLine(\"{0}!\");");
    }

    public async Task Valid_console_app_code_sent_to_run_endpoint_returns_200()
    {
        await RunCode(
            "/workspace/run",
            "using System;\n namespace Test {{public class Program\n {{\n public static void Main()\n {{\n Console.WriteLine(\"{0}!\");\n }}\n }}}}",
            "console");
    }

    private async Task<object> RunCode(string uri, string sourceCode, string workspaceType = null)
    {
        var guidForConsoleOutput = Guid.NewGuid().ToString();

        var sourceCodeJson = JsonConvert.SerializeObject(new
        {
            source = string.Format(sourceCode, guidForConsoleOutput),
            workspaceType
        });

        var contentPost = new StringContent(sourceCodeJson, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage
        {
            Content = contentPost,
            Method = HttpMethod.Post,
            RequestUri = new Uri(uri, UriKind.RelativeOrAbsolute)
        };

        var response = await _httpClient.SendAsync(request);

        response.ShouldSucceed(HttpStatusCode.OK);

        return await response.Content.ReadAsStringAsync();
    }

    public bool AppliesToApplication(string application)
    {
        return application == "trydotnet";
    }
}