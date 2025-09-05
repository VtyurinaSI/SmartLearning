using ProgressService;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddUserProgressDb(builder.Configuration);
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapGet("/", () => Results.Redirect("/swagger"));
}

app.MapGet("/userid/{userLogin}", async (string userLogin, IUserProgressRepository repo, CancellationToken ct) =>
{
    var userId = await repo.GetUserIdAsync(userLogin, ct);
    return userId is null ? Results.NotFound() : Results.Text(userId.ToString());
});
app.Run("http://localhost:6010");
