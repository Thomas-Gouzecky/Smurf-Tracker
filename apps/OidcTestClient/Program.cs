using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDistributedMemoryCache();
builder.Services.AddHttpClient("auth", client =>
{
    client.BaseAddress = new Uri("http://localhost:5260");
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    AllowAutoRedirect = false
});
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

var app = builder.Build();

app.UseSession();

app.MapGet("/", () => Results.Content("""
    <h1>OIDC Test Client</h1>
    <a href="/login">Log in with the auth service</a>
    <br>
    <a href="/register">Register a new user</a>
    """, "text/html"));

app.MapGet("/login", (HttpContext context) =>
{
    var authorizationPath = CreateAuthorizationPath(context);
    return Results.Content(CreateLoginPage(authorizationPath), "text/html");
});

app.MapGet("/register", (HttpContext context) =>
{
    var authorizationPath = CreateAuthorizationPath(context);
    var encodedAuthorizationPath =
        System.Net.WebUtility.HtmlEncode(authorizationPath);

    return Results.Content($$"""
        <!doctype html>
        <html>
        <head><title>Register</title></head>
        <body>
            <h1>Register</h1>
            <form method="post" action="http://localhost:5260/api/auth/register">
                <input type="hidden" name="ReturnUrl" value="{{encodedAuthorizationPath}}">
                <label>
                    Username
                    <input name="UserName" autocomplete="username" required>
                </label>
                <br>
                <label>
                    Email
                    <input name="Email" type="email" autocomplete="email" required>
                </label>
                <br>
                <label>
                    Password
                    <input name="Password" type="password" autocomplete="new-password" required>
                </label>
                <br>
                <label>
                    Phone number (optional)
                    <input name="PhoneNumber" type="tel" autocomplete="tel">
                </label>
                <br>
                <button type="submit">Register</button>
            </form>
        </body>
        </html>
        """, "text/html");
});

app.MapPost("/login", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var form = await context.Request.ReadFormAsync();
    var returnUrl = form["ReturnUrl"].ToString();

    if (!returnUrl.StartsWith("/connect/authorize?", StringComparison.Ordinal))
    {
        return Results.BadRequest("Invalid authorization return URL.");
    }

    var loginRequest = new
    {
        UserName = form["userName"].ToString(),
        Password = form["password"].ToString(),
        ReturnUrl = returnUrl,
        RememberMe = string.Equals(form["rememberMe"], "on", StringComparison.Ordinal)
    };

    var client = httpClientFactory.CreateClient("auth");
    using var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

    if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
    {
        foreach (var cookie in cookies)
        {
            context.Response.Headers.Append("Set-Cookie", cookie);
        }
    }

    if ((int)response.StatusCode is < 300 or >= 400)
    {
        var message = await response.Content.ReadAsStringAsync();
        return Results.Content(
            $"<h1>Login failed</h1><pre>{System.Net.WebUtility.HtmlEncode(message)}</pre>",
            "text/html",
            statusCode: (int)response.StatusCode);
    }

    return Results.Redirect($"http://localhost:5260{returnUrl}");
});

app.MapGet("/callback", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var error = context.Request.Query["error"].ToString();
    if (!string.IsNullOrEmpty(error))
    {
        return Results.BadRequest($"Authentication failed: {error}");
    }

    var expectedState = context.Session.GetString("oidc_state");
    var state = context.Request.Query["state"].ToString();
    if (string.IsNullOrEmpty(expectedState) || !CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expectedState),
            Encoding.UTF8.GetBytes(state)))
    {
        return Results.BadRequest("Invalid OAuth state.");
    }

    var code = context.Request.Query["code"].ToString();
    var codeVerifier = context.Session.GetString("oidc_code_verifier");
    if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(codeVerifier))
    {
        return Results.BadRequest("The authorization response is incomplete.");
    }

    var tokenRequest = new FormUrlEncodedContent(
    [
        new KeyValuePair<string, string>("grant_type", "authorization_code"),
        new KeyValuePair<string, string>("client_id", "service-worker"),
        new KeyValuePair<string, string>("code", code),
        new KeyValuePair<string, string>("redirect_uri", "http://localhost:5118/callback"),
        new KeyValuePair<string, string>("code_verifier", codeVerifier)
    ]);

    var response = await httpClientFactory.CreateClient().PostAsync(
        "http://localhost:5260/connect/token",
        tokenRequest);
    var responseBody = await response.Content.ReadAsStringAsync();

    context.Session.Remove("oidc_state");
    context.Session.Remove("oidc_code_verifier");

    if (!response.IsSuccessStatusCode)
    {
        return Results.Content(
            $"<h1>Token exchange failed</h1><pre>{System.Net.WebUtility.HtmlEncode(responseBody)}</pre>",
            "text/html",
            statusCode: (int)response.StatusCode);
    }

    using var tokenJson = JsonDocument.Parse(responseBody);
    var accessToken = tokenJson.RootElement.GetProperty("access_token").GetString();

    return Results.Content($"""
        <h1>Login successful</h1>
        <p>Access token:</p>
        <pre>{System.Net.WebUtility.HtmlEncode(accessToken)}</pre>
        """, "text/html");
});

app.Run();

static string CreateAuthorizationPath(HttpContext context)
{
    var state = CreateRandomValue();
    var codeVerifier = CreateRandomValue();
    var codeChallenge = WebEncoders.Base64UrlEncode(
        SHA256.HashData(Encoding.UTF8.GetBytes(codeVerifier)));

    context.Session.SetString("oidc_state", state);
    context.Session.SetString("oidc_code_verifier", codeVerifier);

    var authorizationUrl = QueryHelpers.AddQueryString(
        "http://localhost:5260/connect/authorize",
        new Dictionary<string, string?>
        {
            ["client_id"] = "service-worker",
            ["redirect_uri"] = "http://localhost:5118/callback",
            ["response_type"] = "code",
            ["state"] = state,
            ["code_challenge"] = codeChallenge,
            ["code_challenge_method"] = "S256"
        });

    return new Uri(authorizationUrl).PathAndQuery;
}

static string CreateLoginPage(string authorizationPath)
{
    var encodedAuthorizationPath =
        System.Net.WebUtility.HtmlEncode(authorizationPath);

    return $$"""
        <!doctype html>
        <html>
        <head><title>Log in</title></head>
        <body>
            <h1>Log in</h1>
            <form method="post" action="/login">
                <input type="hidden" name="ReturnUrl" value="{{encodedAuthorizationPath}}">
                <label>
                    Username
                    <input name="userName" autocomplete="username" required>
                </label>
                <br>
                <label>
                    Password
                    <input name="password" type="password" autocomplete="current-password" required>
                </label>
                <br>
                <label>
                    <input name="rememberMe" type="checkbox">
                    Remember me
                </label>
                <br>
                <button type="submit">Log in</button>
            </form>
        </body>
        </html>
        """;
}

static string CreateRandomValue()
{
    return WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
}
