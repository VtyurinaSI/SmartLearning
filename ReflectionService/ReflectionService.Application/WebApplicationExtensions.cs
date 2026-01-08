namespace ReflectionService.Application
{
    public static class WebApplicationExtensions
    {
        public static void UseReflectionServiceSwagger(this WebApplication app)
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
    }
}
