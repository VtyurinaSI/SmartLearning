namespace CompilerSevice
{
    public static class WebApplicationExtensions
    {
        public static void UseCompilerServiceSwagger(this WebApplication app)
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
    }
}
