using Fundoo.Observability;
using Fundoo.User.Infrastructure;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using System.Text.Json;

const string ExternalCookieScheme = "ExternalCookie";
const string GoogleOidcScheme = "GoogleOIDC";
var builder = WebApplication.CreateBuilder(args);
//This line runs once when the User service starts, not once per request.

//serilog configuration
builder.Host.UseSerilog((context, _, loggerConfiguration) =>
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)//loads logging settings from the app’s configuration.
        .Enrich.FromLogContext()//adds properties placed in the logging context, such as CorrelationId and TraceId.
        .Enrich.WithProperty("ServiceName", "Fundoo.User.Api"));//adds ServiceName = Fundoo.User.Api to logs so you can tell which service produced them.
builder.Services.AddUserInfrastructure(builder.Configuration);//This calls a setup method in DependencyInjection.cs
builder.Services.AddControllers();//enables controller endpoints, including the actions in AuthController.
builder.Services.AddEndpointsApiExplorer();//makes endpoint information available to tools such as Swagger.

builder.Services.AddHealthChecks()  
    .AddNpgSql(
        connectionString: builder.Configuration.GetConnectionString("UserDb")!,
        name: "postgresql",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "ready" }
    )
    .AddCheck(
        "self",
        () => HealthCheckResult.Healthy(),
        tags: new[] { "live" }
    );

builder.Services.AddSwaggerGen();

var signingKey = builder.Configuration["Jwt:SigningKey"]
    ?? throw new InvalidOperationException("Jwt:SigningKey is not configured.");
var issuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");
var audience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("Jwt:Audience is not configured.");


builder.Services
    .AddAuthentication(options =>
    {
        // Default authentication for normal Fundoo API requests
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        // Default challenge for protected API endpoints
        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,

            ValidateAudience = true,
            ValidAudience = audience,

            ValidateLifetime = true,

            ValidateIssuerSigningKey = true,

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(signingKey))
        };
    })
    .AddCookie(ExternalCookieScheme, options =>
    {
        options.Cookie.Name = "Fundoo.External";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

        options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
    })
    .AddOpenIdConnect(GoogleOidcScheme, options =>
    {
        // Store the authenticated Google identity temporarily
        options.SignInScheme = ExternalCookieScheme;

        // Google OIDC provider
        options.Authority = "https://accounts.google.com";

        // Google OAuth credentials
        options.ClientId =
            builder.Configuration["Authentication:Google:ClientId"]
            ?? throw new InvalidOperationException(
                "Google ClientId is not configured.");

        options.ClientSecret =
            builder.Configuration["Authentication:Google:ClientSecret"]
            ?? throw new InvalidOperationException(
                "Google ClientSecret is not configured.");

        // OIDC callback
        options.CallbackPath = "/signin-oidc";

        // Authorization Code Flow with PKCE
        options.ResponseType = "code";
        options.UsePkce = true;

        options.SaveTokens = false;

        // Requested Google scopes
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");

        // Retrieve additional claims from Google's UserInfo endpoint
        options.GetClaimsFromUserInfoEndpoint = true;

        options.ClaimActions.MapJsonKey(
            "email_verified",
            "email_verified");
    });

builder.Services.AddAuthorization();


// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddFundooCorrelationIds();//registers the request accessor and outgoing correlation ID handler.
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();//registers the handler for unexpected errors.
builder.Services.AddProblemDetails();
var app = builder.Build();

//The request pipeline starts here:-
//They run in the order they appear. For a request to an endpoint, the request passes down through them; after the endpoint finishes, the response travels back through them in reverse order.
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<TraceIdLogContextMiddleware>();
app.UseExceptionHandler();
app.UseSerilogRequestLogging();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseHttpsRedirection();

app.UseRouting();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// here comes the health apis configuration after MapControllers()
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
