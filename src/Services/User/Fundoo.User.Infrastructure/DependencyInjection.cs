        using Fundoo.User.Application.Interfaces;
        using Fundoo.User.Application.Services;
        using Fundoo.User.Infrastructure.Notifications;
        using Fundoo.User.Infrastructure.Persistence;
        using Fundoo.User.Infrastructure.Repositories;
        using Fundoo.User.Infrastructure.Security;
        using Microsoft.AspNetCore.Builder;
        using Microsoft.AspNetCore.RateLimiting;
        using Microsoft.EntityFrameworkCore;
        using Microsoft.Extensions.Configuration;
        using Microsoft.Extensions.DependencyInjection;
        using Microsoft.Extensions.Http.Resilience;
        using Polly;

        using System.Threading.RateLimiting;
        namespace Fundoo.User.Infrastructure;

        public static class DependencyInjection
        {
        public static IServiceCollection AddUserInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
        {
        var connectionString =
        configuration.GetConnectionString("UserDb");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
        throw new InvalidOperationException(
            "Connection string 'UserDb' was not configured.");
        }

        services.AddDbContext<UserDbContext>(options =>
        options.UseNpgsql(
            connectionString,
            npgsqlOptions =>
                npgsqlOptions.MigrationsAssembly(
                    typeof(UserDbContext).Assembly.FullName)));

        //dependencies
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPasswordResetTokenService,PasswordResetTokenService>();

        // Incoming API Rate Limiter: Fixed Window
        services.AddRateLimiter(options =>
        {
        options.AddPolicy<string>("fixed", context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: "global",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromSeconds(30),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));

        options.RejectionStatusCode = 429;
        });
        // Outgoing HTTP Client + Resilience
        services.AddHttpClient<INotificationClient,NotificationApiClient>
            (client =>
        {
            var baseUrl = configuration["Notification:BaseUrl"];

            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                throw new InvalidOperationException(
                    "Notification API base URL is not configured.");
            }

            client.BaseAddress = new Uri(baseUrl);
        })
        .AddStandardResilienceHandler(options =>
        {
            // Step 1: Rate Limiter set to 10 requests
            //options.RateLimiter.DefaultRateLimiterOptions.PermitLimit = 10;
            //options.RateLimiter.DefaultRateLimiterOptions.QueueLimit = 0;
                    

            // Step 2: Total Timeout of 30 or 5 sec, this means How long am I willing to spend on this entire operation, regardless of how many attempts happen?
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);

            // 3. Retry = automatically attempts an HTTP request again when it encounters a failure considered transient (temporary).
            options.Retry.MaxRetryAttempts = 3;
            options.Retry.Delay = TimeSpan.FromSeconds(2);
            options.Retry.BackoffType = DelayBackoffType.Exponential;
            options.Retry.UseJitter = true;

            // Avoid duplicate emails while retrying
            //options.Retry.DisableFor(HttpMethod.Post);

            // 4. Circuit Breaker = Unlike Retry, which keeps trying a failed request, Circuit Breaker temporarily stops requests from reaching a service that is continuously failing.
            //control when the Circuit Breaker opens, how long it monitors failures, and how long it stays open.
            //options.CircuitBreaker.FailureRatio = 0.5; //50 % failure ratio means 50% of the requests must fail for the Circuit Breaker to consider opening.
            //options.CircuitBreaker.MinimumThroughput = 4;//At least 4 requests must be observed during the sampling duration before the Circuit Breaker evaluates the failure ratio.
            //options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);//The Circuit Breaker examines the requests and their outcomes over a rolling 30-second time period window.
            //options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(15);//Once the Circuit Breaker opens, it stays open for 15 seconds before transitioning to Half-Open and while the circuit is open requests are rejected for 15 seconds.

           /*
             * Monitor requests over a rolling 30-second window.
             * Wait until at least 4 requests have been observed.
             * If at least 50% fail, open the circuit.
             * Reject requests for 15 seconds.
             * Move to Half-Open and check whether Notification API has recovered.
           */

            // 5. Attempt Timeout = How long am I willing to wait for this one attempt?
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);
        });
       

        return services;
        }
        }
