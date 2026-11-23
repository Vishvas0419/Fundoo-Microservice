using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;


//This file logs unexpected exceptions and returns a standard problem response containing a trace ID.

//Handles unexpected exceptions that reach ASP.NET’s exception handling pipeline. It logs the exception, HTTP method, request path, and trace ID. If the response has not started, it returns a standard HTTP 500 ProblemDetails response with the trace ID included.
namespace Fundoo.Observability;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    //ASP.NET Core calls this method when an exception reaches the configured exception handling system
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, //current request
        Exception exception, //error that occured
        CancellationToken cancellationToken) //lets the response-writing operation stop if the request is cancelled.
    {
        var traceId = Activity.Current?.TraceId.ToString() //finds the trace id of current request
            ?? httpContext.TraceIdentifier;

        _logger.LogError(
            exception,
            "Unhandled exception for {RequestMethod} {RequestPath}. TraceId: {TraceId}",
            httpContext.Request.Method,
            httpContext.Request.Path,
            traceId);

        if (httpContext.Response.HasStarted)
        {
            return false;
        }

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred.",
            Detail = "The request could not be completed. Use the trace ID when contacting support.",
            Instance = httpContext.Request.Path
        };

        problem.Extensions["traceId"] = traceId;

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(
            problem,
            cancellationToken);

        return true;
    }
}