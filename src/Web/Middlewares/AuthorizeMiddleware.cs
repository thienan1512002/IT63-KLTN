namespace Web.Middlewares;

public class AuthorizeMiddleware
{
    private readonly RequestDelegate _next;

    public AuthorizeMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { message = "Unauthorized" });
            return;
        }

        await _next(context);
    }
}
