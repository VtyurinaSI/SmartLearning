using LlmService;
using MassTransit;
using MinIoStub;
using Npgsql;
using System.Data;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

var cs = builder.Configuration.GetConnectionString("DefaultConnection")
         ?? builder.Configuration.GetConnectionString("ObjectStorage");
Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
builder.Services.AddTransient<IDbConnection>(_ => new NpgsqlConnection(cs));

builder.Services.AddEndpointsApiExplorer();


builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient("Ollama", client =>
{
    var baseUrl = builder.Configuration.GetSection("Ollama")["BaseUrl"] ?? "http://ollama:11434/";
    client.BaseAddress = new Uri(baseUrl);
    client.DefaultRequestHeaders.Accept.Add(
        new MediaTypeWithQualityHeaderValue("application/json"));
});

builder.Services.AddObjectStorage(builder.Configuration);
builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    x.AddConsumer<ReviewRequestedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var mq = builder.Configuration.GetSection("RabbitMq");
        cfg.Host(mq["Host"] ?? "rabbitmq", mq["VirtualHost"] ?? "/", h =>
        {
            h.Username(mq["UserName"] ?? "guest");
            h.Password(mq["Password"] ?? "guest");
        });

        cfg.ConfigureEndpoints(context);
    });
});
//builder.Services.AddControllers();
//builder.Services.AddAuthorization();
var app = builder.Build();

app.UseSwagger();                
app.UseSwaggerUI();
//app.UseAuthorization();
//app.MapControllers();
app.Run();
