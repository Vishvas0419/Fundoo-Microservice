using Fundoo.Notes.Application.Interfaces;
using Fundoo.Notes.Infrastructure.Database;
using Fundoo.Notes.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Fundoo.Notes.Application.Services;
using Fundoo.Notes.Infrastructure.Caching;
using Microsoft.Extensions.Caching.StackExchangeRedis;

namespace Fundoo.Notes.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddNotesInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IDbConnectionFactory,
            NpgsqlConnectionFactory>();
        //Why AddSingleton? The factory stores the connection string, not an open database connection. Each repository operation will create and dispose its own connection.
        services.AddScoped<INoteRepository, NoteRepository>();
        services.AddScoped<INoteService, NoteService>();
        services.AddScoped<ILabelRepository, LabelRepository>();
        services.AddScoped<ILabelService, LabelService>();
        services.AddScoped<INoteLabelRepository, NoteLabelRepository>();

        var redisConnectionString =
    configuration["Redis:ConnectionString"];

        if (string.IsNullOrWhiteSpace(redisConnectionString))
        {
            throw new InvalidOperationException(
                "Redis connection string is not configured.");
        }

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "FundooNotes:";
        });

        services.AddScoped<INoteCache, RedisNoteCache>();

        return services;
    }
}
