using System.Security.Claims;

public sealed class SessionValidationMiddleware
{
    private readonly RequestDelegate _next;

    public SessionValidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ISessionService sessionService)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var sessionId = ISessionService.GetSessionId(context.User);
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? context.User.FindFirstValue("sub");

            if (sessionId is null ||
                userId is null ||
                !Guid.TryParse(sessionId, out var parsedSessionId) ||
                !await sessionService.IsActiveAsync(
                    parsedSessionId,
                    userId,
                    context.RequestAborted))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }
        }

        await _next(context);
    }
}
