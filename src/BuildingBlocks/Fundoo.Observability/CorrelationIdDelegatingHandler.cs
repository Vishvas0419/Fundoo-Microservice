using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;

//Use this for outgoing HTTP requests made while handling an incoming request.
namespace Fundoo.Observability;

public sealed class CorrelationIdDelegatingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CorrelationIdDelegatingHandler(
        IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var context = _httpContextAccessor.HttpContext;

        if (context?.Items[CorrelationIdMiddleware.ItemKey] is string correlationId)
        {
            request.Headers.Remove(CorrelationIdMiddleware.HeaderName);
            request.Headers.TryAddWithoutValidation(
                CorrelationIdMiddleware.HeaderName,
                correlationId);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
