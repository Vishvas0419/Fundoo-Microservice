using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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