using MassTransit;
using Npgsql;
using ProgressService;
using System.Data;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

var cs =  builder.Configuration.GetConnectionString("ObjectStorage");

Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
builder.Services.AddTransient<IDbConnection>(_ => new NpgsqlConnection(cs));

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();
builder.Services.AddUserProgressDb(builder.Configuration);
builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    x.AddConsumer<UpdateProgressConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var mq = builder.Configuration.GetSection("RabbitMq");
        cfg.Host(mq["Host"] ?? "rabbitmq", mq["VirtualHost"] ?? "/", h =>
        {
            h.Username(mq["UserName"] ?? "guest");
            h.Password(mq["Password"] ?? "guest");
        }); ;

        cfg.ConfigureEndpoints(context);
    });
});
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapGet("/", () => Results.Redirect("/swagger"));
}

app.MapGet("/userid/{userLogin}", async (string userLogin, IUserProgressRepository repo, CancellationToken ct) =>
{
    var userId = await repo.GetUserIdAsync(userLogin, ct);
    return userId is null ? Results.NotFound() : Results.Text(userId.ToString());
});

app.MapGet("/user_progress/{userId}", async (Guid userId, IUserProgressRepository repo, CancellationToken ct) =>
{
    var story = await repo.GetUserProgressAsync(userId, ct);
    ComplitedTasks[] compl = story.Where(r => r.Compile && r.Test && r.Review).Select(r => new ComplitedTasks(r.TaskId)).ToArray();
    InProcessTasks[] inp = story
            .Where(r => !r.Compile || !r.Test || !r.Review)
            .Select(r => new InProcessTasks(r.TaskId,
            (!r.Compile
                    ? CheckingStage.Compilation
                    : !r.Test
                        ? CheckingStage.Testing
                        : CheckingStage.Review).ToString())).ToArray();
    long next = 1;
    for (int i = 0; i < compl.Length; i++)
    {
        var expected = i + 1;
        if (compl[i].TaskId != expected) { next = expected; break; }
        next = expected + 1;
    }
    UserProgress prog = new(compl, inp, next);
    return Results.Json(prog);
});
app.Run();
public record UserProgress(ComplitedTasks[] ComplitedTasks, InProcessTasks[] InProcessTasks, long NextTask);
public record ComplitedTasks(long TaskId);
public record InProcessTasks(long TaskId, string NextCheckingStage);
public enum CheckingStage
{
    Compilation,
    Testing,
    Review
}
