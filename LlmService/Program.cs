using LlmService;
using MassTransit;
using System.Data;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

var cs = builder.Configuration.GetConnectionString("DefaultConnection")
         ?? builder.Configuration.GetConnectionString("ObjectStorage");

builder.Services.AddEndpointsApiExplorer();


builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient("Ollama", client =>
{
    var baseUrl = builder.Configuration.GetSection("Ollama")["BaseUrl"] ?? "http://ollama:11434/";
    client.BaseAddress = new Uri(baseUrl);
    client.DefaultRequestHeaders.Accept.Add(
        new MediaTypeWithQualityHeaderValue("application/json"));
    client.Timeout = TimeSpan.FromMinutes(10);
});
builder.Services.AddHttpClient("MinioStorage",c =>
    c.BaseAddress = new Uri(builder.Configuration["Downstream:Storage"]!));
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
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.Run();
