using ReflectionService.Application;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseReflectionServiceSerilog();
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

builder.Services.AddReflectionServiceOptions(builder.Configuration);
builder.Services.AddReflectionServiceHttpClients(builder.Configuration);
builder.Services.AddReflectionServiceMessaging(builder.Configuration);
builder.Services.AddReflectionServiceSwagger();
builder.Services.AddReflectionServicePipeline();

var app = builder.Build();

app.UseReflectionServiceSwagger();
app.UseHttpsRedirection();

app.Run();
