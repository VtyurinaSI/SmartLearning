using Microsoft.Extensions.Configuration;

namespace UserService
{
    public static class UserDbConnectionStrings
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

        public static string GetDefaultOrObjectStorage(IConfiguration cfg)
        {
            var cs = cfg.GetConnectionString("DefaultConnection");
            return string.IsNullOrWhiteSpace(cs) ? GetObjectStorage(cfg) : cs;
        }
    }
}

