using Microsoft.AspNetCore.Http;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


//Reads the diagnostic trace ID from the current Activity. If there is no active activity, it uses ASP.NET’s request identifier instead. It adds the chosen value to the Serilog log context as TraceId while the request is being processed.
namespace Fundoo.Observability;

public sealed class TraceIdLogContextMiddleware
{
    private readonly RequestDelegate _next;

    public TraceIdLogContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var traceId = Activity.Current?.TraceId.ToString()
            ?? context.TraceIdentifier;

        using (LogContext.PushProperty("TraceId", traceId))
        {
            await _next(context);
        }
    }
}