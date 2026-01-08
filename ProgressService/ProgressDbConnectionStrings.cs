using Microsoft.Extensions.Configuration;

namespace ProgressService
{
    public static class ProgressDbConnectionStrings
    {
        public static string GetObjectStorage(IConfiguration cfg)
        {
            var cs = cfg.GetConnectionString("ObjectStorage");
            if (string.IsNullOrWhiteSpace(cs))
            {
                throw new InvalidOperationException("ConnectionStrings:ObjectStorage is not configured");
            }

            return cs;
        }
    }
}
