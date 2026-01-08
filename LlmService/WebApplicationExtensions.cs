namespace LlmService
{
    public static class WebApplicationExtensions
    {
        public static void UseLlmServiceSwagger(this WebApplication app)
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
    }
}
