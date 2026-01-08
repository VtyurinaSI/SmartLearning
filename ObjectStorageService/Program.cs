using ObjectStorageService;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddObjectStorageOptions(builder.Configuration);
builder.Services.AddObjectStorageSwagger();
builder.Services.AddObjectStorageLogging();
builder.Services.AddObjectStorageMinio();

var app = builder.Build();

await app.EnsureObjectStorageAsync();

app.UseSwagger();
app.UseSwaggerUI();
app.MapObjectStorageEndpoints();

app.Run();
