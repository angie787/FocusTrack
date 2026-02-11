using FluentValidation;
using FluentValidation.AspNetCore;
using FocusTrack.Session.Api.Outbox;
using FocusTrack.Session.Api.Services;
using FocusTrack.Session.Application.Interfaces;
using FocusTrack.Session.Infrastructure.Handlers;
using FocusTrack.Session.Infrastructure.Messaging;
using FocusTrack.Session.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Add Controller support
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<FocusTrack.Session.Application.Validators.CreateSessionRequestValidator>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<SessionDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Sessions") ?? builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("FocusTrack.Session.Infrastructure")));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IMonthlyFocusProjection, MonthlyFocusProjection>();
builder.Services.AddScoped<IDomainEventPublisher, OutboxEventPublisher>();
builder.Services.AddSingleton<IOutboxMessagePublisher, RabbitMQOutboxPublisher>();
builder.Services.AddHostedService<OutboxProcessor>();

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateSessionCommandHandler).Assembly));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SessionDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 2. CRITICAL: Comment out or remove HttpsRedirection
// Inside Docker, services communicate over HTTP. This prevents the "Failed to determine https port" error
// app.UseHttpsRedirection(); 

app.UseAuthorization();

// 3. Map Controllers so the [Route] attributes work
app.MapControllers();

// 4. Update the Minimal API to match YARP route
app.MapGet("/api/sessions/test", (HttpContext context) =>
{
    // Extract the header injected by your Gateway
    var userId = context.Request.Headers["X-User-Id"].ToString();

    return Results.Ok(new
    {
        Message = "Session API is reaching the backend!",
        User = userId ?? "No User ID Header found",
        Time = DateTime.UtcNow
    });
});

app.Run();