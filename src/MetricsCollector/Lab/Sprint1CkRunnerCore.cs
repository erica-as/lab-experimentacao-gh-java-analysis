using MetricsCollector.Models;

namespace MetricsCollector.Lab;

/// <summary>Clone + CK + métricas num único repositório (amostra ou batch).</summary>
public static class Sprint1CkRunnerCore
{
    public static async Task RunSingleAsync(
        RepositoryData repo,
        string repoRoot,
        string ckJar,
        bool writeEvidencePack,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(repo.Url) || string.IsNullOrEmpty(repo.Name))
            throw new InvalidOperationException($"Repositório sem Url/Name ({repo.Name}).");

        var safe = RepoLayout.SafeDirName(repo.Name);
        var art = RepoLayout.ArtifactsDir(repoRoot);
        var cloneDir = Path.Combine(art, "clones", safe);
        var ckOutDir = Path.Combine(art, "ck_out", safe);

        await GitRepositoryCloner.CloneIfNeededAsync(repo.Url, cloneDir, cancellationToken);
        await CkJarRunner.RunCkAsync(ckJar, cloneDir, ckOutDir, cancellationToken);

        var agg = CkJarRunner.AggregateFromClassCsv(ckOutDir);
        repo.AvgCbo = agg.AvgCbo;
        repo.AvgDit = agg.AvgDit;
        repo.AvgLcom = agg.AvgLcom;
        repo.CkClassRows = agg.ClassRows;

        if (writeEvidencePack)
            CkJarRunner.WriteEvidencePack(repoRoot, repo.Name, ckOutDir);

        Console.WriteLine(
            $"CK {repo.Name}: CBO={agg.AvgCbo:F4}, DIT={agg.AvgDit:F4}, LCOM={agg.AvgLcom:F4} ({agg.ClassRows} classes).");

        if (!LabArtifactPolicy.KeepArtifacts)
        {
            LabArtifactPolicy.TryDeleteTree(cloneDir, "clone");
            LabArtifactPolicy.TryDeleteTree(ckOutDir, "ck_out");
        }
    }
}
