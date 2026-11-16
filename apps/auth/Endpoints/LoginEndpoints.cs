using Microsoft.AspNetCore.Identity;

public static class LoginEndpoints
{
    public static IEndpointRouteBuilder MapLoginEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/login/submit", SubmitLogin);
        endpoints.MapPost("/register/submit", SubmitRegister);
        endpoints.MapGet("/logout", Logout);
        endpoints.MapPost("/logout", Logout);

        return endpoints;
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

    private static async Task<IResult> Logout(
        SignInManager<ApplicationUser> signInManager)
    {
        await signInManager.SignOutAsync();
        return Results.Redirect("/");
    }

    private static bool IsLocalReturnUrl(string returnUrl)
    {
        return returnUrl.StartsWith("/", StringComparison.Ordinal) &&
            !returnUrl.StartsWith("//", StringComparison.Ordinal) &&
            !returnUrl.StartsWith("/\\", StringComparison.Ordinal);
    }
}
