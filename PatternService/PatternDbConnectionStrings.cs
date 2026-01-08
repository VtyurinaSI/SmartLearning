using Microsoft.Extensions.Configuration;

namespace PatternService;

public static class PatternDbConnectionStrings
{
    public static string GetCatalog(IConfiguration cfg)
    {
        var cs = cfg.GetConnectionString("PatternsCatalog");
        if (!string.IsNullOrWhiteSpace(cs))
        {
            return cs;
        }

        cs = cfg.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(cs))
        {
            throw new InvalidOperationException("ConnectionStrings:PatternsCatalog is not configured");
        }

        return cs;
    }
}
