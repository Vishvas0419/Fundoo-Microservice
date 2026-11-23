using Fundoo.ApiGateway.Middleware;
using Fundoo.Observability;
using HealthChecks.UI.Client;
//using Microsoft.AspNetCore.RateLimiting;
//using System.Threading.RateLimiting;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using MMLib.SwaggerForOcelot.DependencyInjection;
using MMLib.SwaggerForOcelot.Middleware;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, _, loggerConfiguration) =>
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("ServiceName", "Fundoo.ApiGateway"));

builder.Services.AddHealthChecks()
    .AddCheck(
        "self",
        () => HealthCheckResult.Healthy(),
        tags: new[] { "live", "ready" }
    );


builder.Configuration.AddJsonFile(
    "ocelot.json",
    optional: false,
    reloadOnChange: true);

builder.Services.AddOcelot(builder.Configuration) //add ocelot.json
 .AddDelegatingHandler<CorrelationIdDelegatingHandler>(global: true);

builder.Services.AddSwaggerForOcelot(builder.Configuration);  // swagger endpoints seek

var jwtSigningKey = builder.Configuration["Jwt:SigningKey"]
    ?? throw new InvalidOperationException("JWT signing key is missing.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "fundoo-user-api",

            ValidateAudience = true,
            ValidAudience = "fundoo-clients",

            ValidateLifetime = true,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSigningKey))
        };
    });

builder.Services.AddAuthorization();

//asp.net built in ralte limiter

//builder.Services.AddRateLimiter(options =>
//{
//    options.GlobalLimiter =
//        PartitionedRateLimiter.Create<HttpContext, string>(context =>
//            RateLimitPartition.GetFixedWindowLimiter(
//                partitionKey: context.Connection.RemoteIpAddress?.ToString()
//                    ?? "unknown",
//                factory: _ => new FixedWindowRateLimiterOptions
//                {
//                    PermitLimit = 5,
//                    Window = TimeSpan.FromSeconds(30),
//                    AutoReplenishment = true,
//                    QueueLimit = 0
//                }));
//    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
//});

builder.Services.AddFundooCorrelationIds();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();



var app = builder.Build();

app.UseAuthentication(); //actually validates incoming bearer tokens and populates HttpContext.User with the validated claims.
app.UseMiddleware<OcelotClientIdMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<TraceIdLogContextMiddleware>();
app.UseExceptionHandler();
app.UseSerilogRequestLogging();

//app.UseRateLimiter(); //built in rate limiter of asp.net

// Serve aggregated Swagger UI for downstream services
app.UseSwaggerForOcelotUI(options =>
{
    options.PathToSwaggerGenerator = "/swagger/docs";
});

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

// Enable Ocelot routing
await app.UseOcelot();

app.Run();
