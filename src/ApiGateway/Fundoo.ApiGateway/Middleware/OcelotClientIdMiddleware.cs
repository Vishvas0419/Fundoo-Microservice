using Kros.Utils;
using Ocelot.Middleware;
using System.Security.Claims;

namespace Fundoo.ApiGateway.Middleware;
//It will execute for every incoming HTTP request that passes through the Gateway middleware pipeline
//What this file does:
//1. Removes any Oc - Client header supplied by the external caller.
//2. Checks whether the JWT has been authenticated.
//3. Extracts the user's sub claim.
//4. Sets Oc-Client to that user ID.
public class OcelotClientIdMiddleware
{
    private readonly RequestDelegate _next;

    public OcelotClientIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Remove any client ID supplied by the caller.
        context.Request.Headers.Remove("Oc-Client");

        if (context.User.Identity?.IsAuthenticated == true)
        {
            // Authenticated request: identify by JWT user ID.
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
             ?? context.User.FindFirst("sub")?.Value;

            if (!string.IsNullOrWhiteSpace(userId))
            {
                context.Request.Headers["Oc-Client"] = userId;
            }
        }

        // If no authenticated user ID was found, use the client IP.
        if (!context.Request.Headers.ContainsKey("Oc-Client"))
        {
            var clientIp = context.Connection.RemoteIpAddress?.ToString();

            context.Request.Headers["Oc-Client"] =
                !string.IsNullOrWhiteSpace(clientIp) ? clientIp : "unknown";
        }
        Console.WriteLine(
            $"Authenticated: {context.User.Identity?.IsAuthenticated}, " +
            $"NameIdentifier: {context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value}, " +
            $"Sub: {context.User.FindFirst("sub")?.Value}, " +
            $"Oc-Client: {context.Request.Headers["Oc-Client"]}");
        await _next(context);
    }
}