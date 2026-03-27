using System.Globalization;
using MetricsCollector.Models;

namespace MetricsCollector.Lab;

public static class Sprint1LabWorkflow
{
    /// <summary>Índice 0 = mais estrelas. Negativo conta a partir do fim (ex.: -1 = último, tende a ser clone/CK mais leve).</summary>
    public static int ResolveCkSampleIndex(int repositoryCount)
    {
        var raw = Environment.GetEnvironmentVariable("LAB02_CK_REPO_INDEX")?.Trim();
        if (string.IsNullOrEmpty(raw))
            return Math.Max(0, repositoryCount - 1);

        if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
            return Math.Max(0, repositoryCount - 1);

        if (i < 0)
            return Math.Max(0, repositoryCount + i);
        return Math.Min(i, repositoryCount - 1);
    }

    public static async Task RunCkSampleAsync(List<RepositoryData> repositories, string repoRoot,
        CancellationToken cancellationToken = default)
    {
        var ckJar = Environment.GetEnvironmentVariable("CK_JAR")?.Trim();
        if (string.IsNullOrEmpty(ckJar))
            throw new InvalidOperationException(
                "Defina CK_JAR com o caminho absoluto do JAR (ex.: ck-0.7.0-jar-with-dependencies.jar). " +
                "Obtenção: https://github.com/mauricioaniche/ck ou Maven Central.");

        if (repositories.Count == 0)
            throw new InvalidOperationException("Nenhum repositório na lista para amostra CK.");

        var idx = ResolveCkSampleIndex(repositories.Count);
        var sample = repositories[idx];
        if (string.IsNullOrEmpty(sample.Url) || string.IsNullOrEmpty(sample.Name))
            throw new InvalidOperationException($"Linha {idx} sem Url/Name.");

        var safe = RepoLayout.SafeDirName(sample.Name);
        var art = RepoLayout.ArtifactsDir(repoRoot);
        var cloneDir = Path.Combine(art, "clones", safe);
        var ckOutDir = Path.Combine(art, "ck_out", safe);

        await GitRepositoryCloner.CloneIfNeededAsync(sample.Url, cloneDir, cancellationToken);
        await CkJarRunner.RunCkAsync(ckJar, cloneDir, ckOutDir, cancellationToken);

        var agg = CkJarRunner.AggregateFromClassCsv(ckOutDir);
        sample.AvgCbo = agg.AvgCbo;
        sample.AvgDit = agg.AvgDit;
        sample.AvgLcom = agg.AvgLcom;

        CkJarRunner.WriteEvidencePack(repoRoot, sample.Name, ckOutDir);

        Console.WriteLine(
            $"Lab02S01 CK: {sample.Name} — médias classe: CBO={agg.AvgCbo:F4}, DIT={agg.AvgDit:F4}, LCOM={agg.AvgLcom:F4} ({agg.ClassRows} classes).");
    }
}
