using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ApiGatewayTests.AuthenticationTests;

public sealed class AuthenticationTests : IClassFixture<GatewayFixture>
{
    private readonly GatewayFixture _fixture;

    public AuthenticationTests(GatewayFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ProxyRoute_WithoutCredentials_ReturnsUnauthorized()
    {
        using var client = _fixture.CreateClient();

        var response = await client.GetAsync("/test");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProxyRoute_WithAuthenticatedRequest_ForwardsToBackend()
    {
        using var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Authenticated", "true");

        var response = await client.GetAsync("/test");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            "test-backend",
            await response.Content.ReadAsStringAsync());
    }
}

public sealed class GatewayFixture : IAsyncLifetime
{
    private WebApplication? _backend;
    private GatewayFactory? _gateway;

    public async Task InitializeAsync()
    {
        var backendBuilder = WebApplication.CreateBuilder();
        backendBuilder.WebHost.UseKestrel().UseUrls("http://127.0.0.1:0");
        _backend = backendBuilder.Build();
        _backend.MapGet("/backend/echo", (HttpRequest request) =>
            Results.Json(new
            {
                Path = request.Path.Value,
                Query = request.Query.ToDictionary(
                    item => item.Key,
                    item => item.Value.ToString())
            }));
        _backend.MapPost("/backend/body", async (HttpRequest request) =>
            Results.Text(await new StreamReader(request.Body).ReadToEndAsync()));
        _backend.MapGet(
            "/backend/status/{statusCode:int}",
            (int statusCode) => Results.StatusCode(statusCode));
        _backend.Map("/{**path}", () => Results.Text("test-backend"));
        await _backend.StartAsync();

        var address = _backend.Urls.Single();
        _gateway = new GatewayFactory(address);
    }

    public HttpClient CreateClient() =>
        _gateway?.CreateClient()
        ?? throw new InvalidOperationException("The gateway is not initialized.");

    public async Task DisposeAsync()
    {
        if (_gateway is not null)
        {
            await _gateway.DisposeAsync();
        }

        if (_backend is not null)
        {
            await _backend.StopAsync();
            await _backend.DisposeAsync();
        }
    }
}

internal sealed class GatewayFactory : WebApplicationFactory<Program>
{
    private readonly string _backendAddress;

    public GatewayFactory(string backendAddress)
    {
        _backendAddress = backendAddress;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
        builder.UseSetting("Authentication:Authority", "https://auth.test");
        builder.UseSetting("Authentication:Audience", "smurftracker-api");
        builder.UseSetting(
            "ReverseProxy:Routes:TestRoute:ClusterId",
            "TestCluster");
        builder.UseSetting(
            "ReverseProxy:Routes:TestRoute:Match:Path",
            "/test/{**catch-all}");
        builder.UseSetting(
            "ReverseProxy:Routes:TestRoute:Transforms:0:PathRemovePrefix",
            "/test");
        builder.UseSetting(
            "ReverseProxy:Clusters:TestCluster:Destinations:Test:Address",
            _backendAddress + "/");
        builder.ConfigureTestServices(services =>
        {
            services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                    "Test", _ => { });
            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Test";
            });
        });
    }
}

internal sealed class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey("X-Test-Authenticated"))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "test-user")],
            Scheme.Name);
        return Task.FromResult(
            AuthenticateResult.Success(
                new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }
}
