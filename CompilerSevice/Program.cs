using CompilerSevice;
using MassTransit;
using System.Data;


var builder = WebApplication.CreateBuilder(args);
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);
var cs = builder.Configuration.GetConnectionString("ObjectStorage");


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient<CompileRequestedConsumer>(c =>
    c.BaseAddress = new Uri(builder.Configuration["Downstream:Storage"]!));


builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    x.AddConsumer<CompileRequestedConsumer>();

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
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();

app.Run();
