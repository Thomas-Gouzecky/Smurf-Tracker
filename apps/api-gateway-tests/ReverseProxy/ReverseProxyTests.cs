using System.Net;
using System.Text;
using ApiGatewayTests.AuthenticationTests;

namespace ApiGatewayTests.ReverseProxy;

public sealed class ReverseProxyTests : IClassFixture<GatewayFixture>
{
    private readonly GatewayFixture _fixture;

    public ReverseProxyTests(GatewayFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ProxyRoute_ForwardsPathAndQueryString()
    {
        using var client = CreateAuthenticatedClient();

        var response = await client.GetAsync(
            "/test/backend/echo?search=smurfs&limit=10");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"path\":\"/backend/echo\"", body);
        Assert.Contains("\"search\":\"smurfs\"", body);
        Assert.Contains("\"limit\":\"10\"", body);
    }

    [Fact]
    public async Task ProxyRoute_ForwardsRequestBodyAndMethod()
    {
        using var client = CreateAuthenticatedClient();
        using var content = new StringContent(
            "{\"name\":\"blue\"}",
            Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync("/test/backend/body", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            "{\"name\":\"blue\"}",
            await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ProxyRoute_ForwardsUpstreamStatusCode()
    {
        using var client = CreateAuthenticatedClient();

        var response = await client.GetAsync("/test/backend/status/202");

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    private HttpClient CreateAuthenticatedClient()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Authenticated", "true");
        return client;
    }
}