using LlmService;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

builder.Services.AddLlmServiceOptions(builder.Configuration);
builder.Services.AddLlmServiceSwagger();
builder.Services.AddLlmServiceHttpClients(builder.Configuration);
builder.Services.AddLlmServiceCore();
builder.Services.AddLlmServiceMessaging(builder.Configuration);

var app = builder.Build();

app.UseLlmServiceSwagger();
app.Run();
