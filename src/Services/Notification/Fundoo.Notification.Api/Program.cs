
using Fundoo.Notification.Application.Interfaces;
using Fundoo.Notification.Application.Services;
using Fundoo.Notification.Infrastructure;
using Fundoo.Observability;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using HealthChecks.UI.Client;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, _, loggerConfiguration) =>
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("ServiceName", "Fundoo.Notification.Api"));

// Register controllers and Swagger.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddHealthChecks()
    .AddCheck(
        "liveness",
        () => HealthCheckResult.Healthy(),
        tags: new[] { "live" })
    .AddCheck(
        "smtp-configuration",
        () => !string.IsNullOrWhiteSpace(
                  builder.Configuration["SmtpSettings:Host"])
              && int.TryParse(
                  builder.Configuration["SmtpSettings:Port"],
                  out _)
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("SMTP settings are missing or invalid."),
        tags: new[] { "ready" });
builder.Services.AddSwaggerGen();

// Register Application services.
builder.Services.AddScoped<
    INotificationService,
    NotificationService>();

// Register Infrastructure services and SMTP settings.
builder.Services.AddNotificationInfrastructure(
    builder.Configuration);

builder.Services.AddFundooCorrelationIds();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<TraceIdLogContextMiddleware>();
app.UseExceptionHandler();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.Run();
