
using Fundoo.Notification.Application.Interfaces;
using Fundoo.Notification.Application.Services;
using Fundoo.Notification.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Register controllers and Swagger.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register Application services.
builder.Services.AddScoped<
    INotificationService,
    NotificationService>();

// Register Infrastructure services and SMTP settings.
builder.Services.AddNotificationInfrastructure(
    builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
