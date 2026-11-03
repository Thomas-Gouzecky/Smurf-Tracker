using System.Security.Claims;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

public class AuthorizationController : Controller
{
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AuthorizationController(IOpenIddictApplicationManager applicationManager, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        _applicationManager = applicationManager;
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [HttpPost("~/connect/token"), Produces("application/json")]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest() ??
            throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        if (request.IsClientCredentialsGrantType())
        {
            var application = await _applicationManager.FindByClientIdAsync(request.ClientId!) ??
                throw new InvalidOperationException("The application cannot be found.");

            var identity = new ClaimsIdentity(
                TokenValidationParameters.DefaultAuthenticationType,
                OpenIddictConstants.Claims.Name,
                OpenIddictConstants.Claims.Role);

            identity.AddClaim(OpenIddictConstants.Claims.Subject, (await _applicationManager.GetClientIdAsync(application))!,
                OpenIddictConstants.Destinations.AccessToken);

            var principal = new ClaimsPrincipal(identity);

            principal.SetScopes(request.GetScopes());
            principal.SetResources("resource_server");

            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        if (request.IsAuthorizationCodeGrantType())
        {
            var result = await HttpContext.AuthenticateAsync(
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            if (!result.Succeeded || result.Principal is null)
            {
                throw new InvalidOperationException(
                    "The authorization code cannot be redeemed.");
            }

            return SignIn(
                result.Principal,
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        throw new InvalidOperationException("The specified grant type is not supported.");
    }

    [HttpGet("~/connect/authorize")]
    [HttpPost("~/connect/authorize")]
    public async Task<IActionResult> Authorize()
    {
        // Retrieve the authorization request validated by OpenIddict.
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException(
                "The OpenID Connect request cannot be retrieved.");

        // Check whether the user has an active Identity login session.
        var result = await HttpContext.AuthenticateAsync(
            IdentityConstants.ApplicationScheme);

        if (!result.Succeeded)
        {
            // The client requested authentication without user interaction.
            if (request.HasPromptValue(OpenIddictConstants.PromptValues.None))
            {
                return Forbid(
                    authenticationSchemes:
                        OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(
                        new Dictionary<string, string?>
                        {
                            [OpenIddictServerAspNetCoreConstants.Properties.Error] =
                                OpenIddictConstants.Errors.LoginRequired
                        }));
            }

            // Redirect to the Identity login page.
            return Challenge(
                authenticationSchemes: IdentityConstants.ApplicationScheme,
                properties: new AuthenticationProperties
                {
                    RedirectUri = Request.PathBase +
                        Request.Path +
                        QueryString.Create(
                            Request.HasFormContentType
                                ? Request.Form.ToList()
                                : Request.Query.ToList())
                });
        }

        // Retrieve the currently authenticated user.
        var user = await _userManager.GetUserAsync(result.Principal!);

        if (user is null)
        {
            return Forbid(
                authenticationSchemes:
                    OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        // Create a claims principal using ASP.NET Core Identity.
        var principal = await _signInManager.CreateUserPrincipalAsync(user);
        var sessionId = ISessionService.GetSessionId(result.Principal!);
        if (sessionId is null)
        {
            var sessionService = HttpContext.RequestServices
                .GetRequiredService<ISessionService>();
            var createdSessionId = await sessionService.CreateAsync(
                user.Id,
                request.ClientId ?? "oidc",
                Request.Headers.UserAgent.ToString(),
                HttpContext.Connection.RemoteIpAddress?.ToString());
            principal.AddIdentity(new ClaimsIdentity(
            [
                new Claim(
                    SessionClaimTypes.SessionId,
                    createdSessionId.ToString())
            ]));
        }

        // Set the subject claim identifying the user.
        principal.SetClaim(
            OpenIddictConstants.Claims.Subject,
            await _userManager.GetUserIdAsync(user));

        // Set the scopes requested by the client.
        principal.SetScopes(request.GetScopes());

        // Decide which claims should be included in which tokens.
        principal.SetDestinations(claim => claim.Type switch
        {
            OpenIddictConstants.Claims.Subject =>
            [
                OpenIddictConstants.Destinations.AccessToken,
                OpenIddictConstants.Destinations.IdentityToken
            ],

            OpenIddictConstants.Claims.Name
                when principal.HasScope(OpenIddictConstants.Scopes.Profile) =>
            [
                OpenIddictConstants.Destinations.AccessToken,
                OpenIddictConstants.Destinations.IdentityToken
            ],

            OpenIddictConstants.Claims.Email
                when principal.HasScope(OpenIddictConstants.Scopes.Email) =>
            [
                OpenIddictConstants.Destinations.AccessToken,
                OpenIddictConstants.Destinations.IdentityToken
            ],

            SessionClaimTypes.SessionId =>
            [
                OpenIddictConstants.Destinations.AccessToken
            ],

            _ => []
        });

        // Ask OpenIddict to generate the authorization response.
        return SignIn(
            principal,
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpGet("~/connect/logout")]
    [HttpPost("~/connect/logout")]
    public async Task<IActionResult> Logout()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException(
                "The OpenID Connect request cannot be retrieved.");

        await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);

        return SignOut(
            new AuthenticationProperties
            {
                RedirectUri = request.PostLogoutRedirectUri?.ToString()
            },
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }
}