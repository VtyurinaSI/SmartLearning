using MassTransit;
using ReflectionService.Application;
using ReflectionService.Domain.PipelineOfCheck;
using ReflectionService.Domain.Reporting;
using Serilog;
using Serilog.Events;


var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((ctx, lc) =>
    {
        lc.ReadFrom.Configuration(ctx.Configuration)
          .MinimumLevel.Debug()
          .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
          .MinimumLevel.Override("Microsoft.Extensions.Http", LogEventLevel.Warning)
          .MinimumLevel.Override("System.Net.Http", LogEventLevel.Warning)
          .MinimumLevel.Override("MassTransit", LogEventLevel.Information)
          .MinimumLevel.Override("RabbitMQ.Client", LogEventLevel.Warning)
          .Enrich.FromLogContext()
          .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}");
    });

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

//builder.Configuration.GetConnectionString("ObjectStorage");

builder.Services.AddHttpClient<ReflectionRequestedConsumer>(c =>
    c.BaseAddress = new Uri(builder.Configuration["Downstream:Storage"]!));
builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    x.AddConsumer<ReflectionRequestedConsumer>();

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
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddReflectionStepHandlers();
builder.Services.AddTransient<CheckingPipeline>();
builder.Services.AddTransient<ICheckingReportBuilder, CheckingReportBuilder>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();

app.Run();
