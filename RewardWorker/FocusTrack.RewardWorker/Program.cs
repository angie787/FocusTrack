using FocusTrack.RewardWorker;
using FocusTrack.RewardWorker.Persistence;
using FocusTrack.RewardWorker.Services;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<SessionApiOptions>(builder.Configuration.GetSection(SessionApiOptions.SectionName));

builder.Services.AddDbContext<RewardsDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Rewards") ?? "Host=localhost;Port=5433;Database=rewards;Username=focustrack;Password=focustrack_secret",
        b => b.MigrationsAssembly(typeof(RewardsDbContext).Assembly.GetName().Name)));

builder.Services.AddHttpClient<ISessionApiClient, SessionApiClient>((sp, client) =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<SessionApiOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseAddress);
    client.DefaultRequestHeaders.Add("X-Internal-Api-Key", options.InternalApiKey);
});

builder.Services.AddSingleton<IDailyGoalEventPublisher, RabbitMQDailyGoalPublisher>();
builder.Services.AddScoped<IDailyGoalService, DailyGoalService>();
builder.Services.AddHostedService<SessionEventConsumer>();

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RewardsDbContext>();
    db.Database.Migrate();
}

host.Run();
