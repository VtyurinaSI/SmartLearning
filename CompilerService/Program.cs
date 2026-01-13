using CompilerService;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

builder.Services.AddCompilerServiceOptions(builder.Configuration);
builder.Services.AddCompilerServiceSwagger();
builder.Services.AddCompilerServiceHttpClients(builder.Configuration);
builder.Services.AddCompilerServiceCore();
builder.Services.AddCompilerServiceMessaging(builder.Configuration);

var app = builder.Build();

app.UseCompilerServiceSwagger();
app.UseHttpsRedirection();

app.Run();

