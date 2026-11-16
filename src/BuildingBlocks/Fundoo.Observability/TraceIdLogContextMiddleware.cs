using Microsoft.AspNetCore.Http;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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