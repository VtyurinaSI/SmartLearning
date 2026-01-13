using Gateway;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

builder.Host.UseGatewaySerilog();

builder.Services.AddGatewayOptions(builder.Configuration);
builder.Services.AddGatewaySwagger();
builder.Services.AddGatewayHeaderPropagation();
builder.Services.AddGatewayLogging();
builder.Services.AddGatewayHttpClients(builder.Configuration);
builder.Services.AddGatewayHealthChecks(builder.Configuration);
builder.Services.AddGatewayAuthentication(builder.Configuration);

var app = builder.Build();

app.UseGatewaySwagger();
app.UseGatewayRequestLogging();
app.UseGatewayCorrelationId();
app.UseHeaderPropagation();
app.UseAuthentication();
app.UseAuthorization();
app.UseGatewayUserIdHeader();
app.MapGatewayEndpoints();
app.UseGatewayUiFiles();

app.Run();

