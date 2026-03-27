namespace MetricsCollector.Lab;

public static class RepoLayout
{
    /// <summary>Raiz do repositório (diretório que contém .sln).</summary>
    public static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null)
        {
            if (dir.GetFiles("*.sln").Length > 0)
                return dir.FullName;
            dir = dir.Parent;
        }
        return Directory.GetCurrentDirectory();
    }

    public static string ArtifactsDir(string repoRoot) => Path.Combine(repoRoot, "artifacts");

    public static string SafeDirName(string nameWithOwner) =>
        string.Join("_", nameWithOwner.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries))
            .Replace('/', '_');
}
