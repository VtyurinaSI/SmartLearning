using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient("Ollama", client =>
{
    client.BaseAddress = new Uri("http://localhost:11434/");
    client.DefaultRequestHeaders.Accept.Add(
        new MediaTypeWithQualityHeaderValue("application/json"));
});

builder.Services.AddControllers();
var app = builder.Build();

app.UseSwagger();                
app.UseSwaggerUI();
app.UseAuthorization();
app.MapControllers();
app.Run("http://localhost:6003/");
