using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//Provides the AddFundooCorrelationIds() registration helper

//It registers IHttpContextAccessor and CorrelationIdDelegatingHandler with dependency injection.

namespace Fundoo.Observability;

public static class CorrelationIdServiceCollectionExtensions
{
    public static IServiceCollection AddFundooCorrelationIds(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddTransient<CorrelationIdDelegatingHandler>();
        return services;
    }
}