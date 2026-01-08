using ProgressService;
using UserService;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

builder.Services.AddUserServiceOptions(builder.Configuration);
builder.Services.AddUserServiceCore();
builder.Services.AddUserProgressDb(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddUserServiceAuthentication(builder.Configuration);
builder.Services.AddAuthorization();
builder.Services.AddHealthChecks();
builder.Services.AddUserServiceMessaging(builder.Configuration);

var app = builder.Build();
await app.EnsureUserDbAsync();

app.UseAuthentication();
app.UseAuthorization();
app.MapUserServiceEndpoints();
await app.RunAsync();
