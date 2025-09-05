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

app.UseHttpsRedirection();
app.MapPost("/userid", async (string UserLogin, IUserProgressRepository repo, CancellationToken ct) =>
{
    var userId = await repo.GetUserIdAsync(UserLogin, ct);
    return userId is null ? Results.Empty : Results.Ok(userId);
});
app.Run("http://localhost:6010");
