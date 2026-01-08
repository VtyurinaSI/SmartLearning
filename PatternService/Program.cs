using PatternService;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;
var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

builder.Services.AddPatternServiceOptions(builder.Configuration);
builder.Services.AddPatternServiceDb(builder.Configuration);
builder.Services.AddPatternServiceStorage(builder.Configuration);
builder.Services.AddPatternServiceCore();
builder.Services.AddPatternServiceSwagger();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UsePatternServiceSwagger();
}

app.MapPatternServiceEndpoints();
app.Run();
