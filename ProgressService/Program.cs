using ProgressService;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

builder.Services.AddProgressServiceOptions(builder.Configuration);
builder.Services.AddProgressServiceSwagger();
builder.Services.AddUserProgressDb(builder.Configuration);
builder.Services.AddProgressServiceCore();
builder.Services.AddProgressServiceMessaging(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseProgressServiceSwagger();
}

app.MapProgressServiceEndpoints();
app.Run();
