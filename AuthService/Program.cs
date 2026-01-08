using AuthService;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

builder.Services.AddAuthServiceOptions(builder.Configuration);
builder.Services.AddAuthServiceDb(builder.Configuration);
builder.Services.AddAuthServiceMessaging(builder.Configuration);
builder.Services.AddAuthServiceIdentity();
builder.Services.AddAuthServiceAuthentication(builder.Configuration);
builder.Services.AddAuthServiceSwagger();
builder.Services.AddAuthServiceCore();

var app = builder.Build();
await app.EnsureAuthDbAsync();

if (app.Environment.IsDevelopment())
{
    app.UseAuthServiceSwagger();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
