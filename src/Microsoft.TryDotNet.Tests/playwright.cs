using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Playwright;
using Xunit;

namespace Microsoft.TryDotNet.Tests;

public class playwright : IClassFixture<WebServerFixture>
{
    private readonly WebServerFixture fixture;

    public playwright(WebServerFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task Works()
    {
        var addresses = fixture.getStuff();
        var url = $"{addresses.FirstOrDefault()}";

       // await Task.Delay(60000);
        var page = await fixture.Browser.NewPageAsync();
        await page.GotoAsync(url);
        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = "screenshot.png"
        });


    }

}


// ReSharper disable once ClassNeverInstantiated.Global
public class WebServerFixture : IAsyncLifetime, IDisposable
{
    private readonly WebApplication host;
    private IPlaywright PlaywrightSession { get; set; }
    public IBrowser Browser { get; private set; }

    public ICollection<string> getStuff()
    {
        return host.Urls;
    }
    

    public WebServerFixture()
    {
        var webApplicationOptions = new WebApplicationOptions
        {
        };

        host = Program.CreateWebApplication(webApplicationOptions);
    }

  
  
    public async Task InitializeAsync()
    {
        Environment.SetEnvironmentVariable($"ASPNETCORE_{WebHostDefaults.PreventHostingStartupKey}", "true");

        var exitCode = Playwright.Program.Main(new[] { "install" });
        if (exitCode != 0)
        {
            throw new Exception($"Playwright exited with code {exitCode}");
        }

        PlaywrightSession = await Microsoft.Playwright.Playwright.CreateAsync();
        
        var browserTypeLaunchOptions = new BrowserTypeLaunchOptions();
        if (Debugger.IsAttached)
        {
            browserTypeLaunchOptions.Headless = false;
        }
        browserTypeLaunchOptions.Headless = false;

        Browser = await PlaywrightSession.Chromium.LaunchAsync(browserTypeLaunchOptions);
        host.Urls.Clear();
        host.Urls.Add("http://127.0.0.1:0");
        await host.StartAsync();

    }

    public async Task DisposeAsync()
    {
        await host.StopAsync();
        PlaywrightSession?.Dispose();
    }

    public void Dispose()
    {
        host?.StopAsync();
        PlaywrightSession?.Dispose();
    }

    private static int GetRandomUnusedPort()
    {
        var listener = new TcpListener(IPAddress.Any, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}