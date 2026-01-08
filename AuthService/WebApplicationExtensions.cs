namespace AuthService
{
    public static class WebApplicationExtensions
    {
        public static async Task EnsureAuthDbAsync(this WebApplication app, CancellationToken ct = default)
        {
            using var scope = app.Services.CreateScope();
            var migrator = scope.ServiceProvider.GetRequiredService<AuthService.Services.AuthDbMigrator>();
            await migrator.EnsureMigratedAsync(ct);
        }

        public static void UseAuthServiceSwagger(this WebApplication app)
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Auth Service v1");
                c.RoutePrefix = "swagger";
            });
        }
    }
}
