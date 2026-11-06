using Fundoo.Notes.Application.Interfaces;
using Fundoo.Notes.Infrastructure.Database;
using Fundoo.Notes.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Fundoo.Notes.Application.Services;

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

        return services;
    }
}
