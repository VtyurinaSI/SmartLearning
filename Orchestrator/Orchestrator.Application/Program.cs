using Orchestrator.Application;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;
var builder = WebApplication.CreateBuilder(args);

builder.Host.UseOrchestratorSerilog();
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

builder.Services.AddOrchestratorOptions(builder.Configuration);
builder.Services.AddOrchestratorHttpClients(builder.Configuration);
builder.Services.AddOrchestratorHealthChecks();
builder.Services.AddOrchestratorSwagger();
builder.Services.AddOrchestratorCore();
builder.Services.AddOrchestratorMessaging(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseOrchestratorSwagger();
}

app.MapOrchestratorEndpoints();
app.Run();

