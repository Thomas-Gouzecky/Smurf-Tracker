using Microsoft.AspNetCore.Identity;

public static class LoginEndpoints
{
    public static IEndpointRouteBuilder MapLoginEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/login", ShowLogin);
        endpoints.MapPost("/login", SubmitLogin);
        endpoints.MapGet("/register", ShowRegister);
        endpoints.MapPost("/register", SubmitRegister);

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
                <p>
                    <a href="/register?ReturnUrl={Uri.EscapeDataString(returnUrl)}">
                        Register a new account
                    </a>
                </p>
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

    private static IResult ShowRegister(HttpContext context)
    {
        var returnUrl = context.Request.Query["ReturnUrl"].ToString();
        if (!IsLocalReturnUrl(returnUrl))
        {
            returnUrl = "/";
        }

        return Results.Content($"""
            <!doctype html>
            <html>
            <head><title>Register</title></head>
            <body>
                <h1>Register</h1>
                <form method="post" action="/register">
                    <input type="hidden" name="returnUrl"
                           value="{System.Net.WebUtility.HtmlEncode(returnUrl)}">
                    <label>
                        Username
                        <input name="userName" autocomplete="username" required>
                    </label>
                    <br>
                    <label>
                        Email
                        <input name="email" type="email" autocomplete="email" required>
                    </label>
                    <br>
                    <label>
                        Password
                        <input name="password" type="password"
                               autocomplete="new-password" required>
                    </label>
                    <br>
                    <label>
                        Phone number (optional)
                        <input name="phoneNumber" type="tel" autocomplete="tel">
                    </label>
                    <br>
                    <button type="submit">Register</button>
                </form>
                <p>
                    <a href="/login?ReturnUrl={Uri.EscapeDataString(returnUrl)}">
                        Back to login
                    </a>
                </p>
            </body>
            </html>
            """, "text/html");
    }

    private static async Task<IResult> SubmitRegister(
        HttpContext context,
        IAuthService authService)
    {
        var form = await context.Request.ReadFormAsync();
        var returnUrl = form["returnUrl"].ToString();
        if (!IsLocalReturnUrl(returnUrl))
        {
            return Results.BadRequest("Invalid return URL.");
        }

        var result = await authService.RegisterAsync(new UserRegisterRequest
        {
            UserName = form["userName"].ToString(),
            Email = form["email"].ToString(),
            Password = form["password"].ToString(),
            PhoneNumber = form["phoneNumber"].ToString()
        });

        if (!result.IsSuccess)
        {
            var errors = string.Join(
                "<br>",
                (result.Errors ?? []).Select(error =>
                    System.Net.WebUtility.HtmlEncode(error.Description)));

            return Results.Content(
                $"<h1>Registration failed</h1><p>{errors}</p>",
                "text/html",
                statusCode: StatusCodes.Status400BadRequest);
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
