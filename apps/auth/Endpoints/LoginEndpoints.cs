using Microsoft.AspNetCore.Identity;

public static class LoginEndpoints
{
    public static IEndpointRouteBuilder MapLoginEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/login", ShowLogin);
        endpoints.MapPost("/login", SubmitLogin);

        return endpoints;
    }

    private static IResult ShowLogin(HttpContext context)
    {
        var returnUrl = context.Request.Query["ReturnUrl"].ToString();
        if (!IsLocalReturnUrl(returnUrl))
        {
            returnUrl = "/";
        }

        return Results.Content($"""
            <!doctype html>
            <html>
            <head><title>Log in</title></head>
            <body>
                <h1>Log in</h1>
                <form method="post" action="/login">
                    <input type="hidden" name="returnUrl"
                           value="{System.Net.WebUtility.HtmlEncode(returnUrl)}">
                    <label>
                        Username
                        <input name="userName" autocomplete="username" required>
                    </label>
                    <br>
                    <label>
                        Password
                        <input name="password" type="password"
                               autocomplete="current-password" required>
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
            """, "text/html");
    }

    private static async Task<IResult> SubmitLogin(
        HttpContext context,
        SignInManager<ApplicationUser> signInManager)
    {
        var form = await context.Request.ReadFormAsync();
        var returnUrl = form["returnUrl"].ToString();
        if (!IsLocalReturnUrl(returnUrl))
        {
            return Results.BadRequest("Invalid return URL.");
        }

        var result = await signInManager.PasswordSignInAsync(
            form["userName"].ToString(),
            form["password"].ToString(),
            string.Equals(form["rememberMe"], "on", StringComparison.Ordinal),
            lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            return Results.Content(
                "<h1>Login failed</h1><p>Invalid username or password.</p>",
                "text/html",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        return Results.Redirect(returnUrl);
    }

    private static bool IsLocalReturnUrl(string returnUrl)
    {
        return returnUrl.StartsWith("/", StringComparison.Ordinal) &&
            !returnUrl.StartsWith("//", StringComparison.Ordinal) &&
            !returnUrl.StartsWith("/\\", StringComparison.Ordinal);
    }
}
