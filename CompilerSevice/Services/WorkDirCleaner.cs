namespace CompilerSevice;

public sealed class WorkDirCleaner
{
    public void TryDelete(string? path)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
        }
    }
}
