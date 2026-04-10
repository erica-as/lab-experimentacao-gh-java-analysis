using System.Globalization;
using MetricsCollector;
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

    static string RequireCkJar()
    {
        var ckJar = Environment.GetEnvironmentVariable("CK_JAR")?.Trim();
        if (string.IsNullOrEmpty(ckJar))
            throw new InvalidOperationException(
                "Defina CK_JAR com o caminho absoluto do JAR (ex.: ck-0.7.0-jar-with-dependencies.jar). " +
                "Obtenção: https://github.com/mauricioaniche/ck ou Maven Central.");
        return ckJar;
    }

    public static async Task RunCkSampleAsync(List<RepositoryData> repositories, string repoRoot,
        CancellationToken cancellationToken = default)
    {
        if (repositories.Count == 0)
            throw new InvalidOperationException("Nenhum repositório na lista para amostra CK.");

        var idx = ResolveCkSampleIndex(repositories.Count);
        var sample = repositories[idx];
        if (string.IsNullOrEmpty(sample.Url) || string.IsNullOrEmpty(sample.Name))
            throw new InvalidOperationException($"Linha {idx} sem Url/Name.");

        await Sprint1CkRunnerCore.RunSingleAsync(sample, repoRoot, RequireCkJar(), writeEvidencePack: true, cancellationToken);

        Console.WriteLine($"Lab02S01 (amostra): linha índice {idx} ({sample.Name}).");
    }

    /// <summary>
    /// Grau de paralelismo lido de <c>LAB02_CK_PARALLEL</c> (padrão 1 = sequencial).
    /// Valores típicos: 2–4. Cada worker clona e executa Java em paralelo.
    /// </summary>
    public static int ResolveParallelism() =>
        int.TryParse(Environment.GetEnvironmentVariable("LAB02_CK_PARALLEL"), out var p) && p > 0 ? p : 1;

    /// <summary>
    /// Conta LOC/.java em todos os repositórios via clone shallow; grava CSV após cada sucesso.
    /// <paramref name="resume"/> ignora linhas com <see cref="RepositoryData.TotalLoc"/> &gt; 0.
    /// </summary>
    public static async Task<(int done, int skipped, int failed)> RunLocAllAsync(
        List<RepositoryData> repositories,
        string repoRoot,
        bool resume,
        CancellationToken cancellationToken = default)
    {
        var total = repositories.Count;
        var done = 0; var skipped = 0; var failed = 0;
        var saveLock = new SemaphoreSlim(1, 1);
        var parallelism = ResolveParallelism();

        await Parallel.ForEachAsync(
            repositories.Select((repo, i) => (repo, i)),
            new ParallelOptions { MaxDegreeOfParallelism = parallelism, CancellationToken = cancellationToken },
            async (item, ct) =>
            {
                var (repo, i) = item;
                if (string.IsNullOrEmpty(repo.Url) || string.IsNullOrEmpty(repo.Name))
                {
                    Console.Error.WriteLine($"[{i + 1}/{total}] Ignorado: sem Url/Name.");
                    Interlocked.Increment(ref failed);
                    return;
                }
                if (resume && repo.TotalLoc > 0) { Interlocked.Increment(ref skipped); return; }

                Console.WriteLine($"[{i + 1}/{total}] LOC: {repo.Name}");
                try
                {
                    var cloneDir = Path.Combine(RepoLayout.ArtifactsDir(repoRoot), "clones", RepoLayout.SafeDirName(repo.Name));
                    await GitRepositoryCloner.CloneIfNeededAsync(repo.Url, cloneDir, ct);
                    var (loc, comments) = LocCounter.CountJavaFiles(cloneDir);
                    repo.TotalLoc = loc;
                    repo.CommentLines = comments;
                    Interlocked.Increment(ref done);
                    Console.WriteLine($"LOC {repo.Name}: {loc} linhas, {comments} comentários.");

                    if (!LabArtifactPolicy.KeepArtifacts)
                        LabArtifactPolicy.TryDeleteTree(cloneDir, "clone");

                    await saveLock.WaitAsync(ct);
                    try { CsvExporter.SaveToCsv(repositories, "repositorios_processo.csv"); }
                    finally { saveLock.Release(); }
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref failed);
                    Console.Error.WriteLine($"  falhou [{repo.Name}]: {ex.Message}");
                }
            });

        Console.WriteLine($"LOC batch: OK={done}, ignorados (resume)={skipped}, falhas={failed}.");
        return (done, skipped, failed);
    }

    /// <summary>
    /// CK em todos os repositórios; grava CSV após cada sucesso.
    /// <paramref name="resume"/> ignora linhas com <see cref="RepositoryData.CkClassRows"/> &gt; 0.
    /// Grau de paralelismo via <c>LAB02_CK_PARALLEL</c>.
    /// </summary>
    public static async Task<(int done, int skipped, int failed)> RunCkAllAsync(
        List<RepositoryData> repositories,
        string repoRoot,
        bool resume,
        bool evidencePerRepo,
        CancellationToken cancellationToken = default)
    {
        var ckJar = RequireCkJar();
        var total = repositories.Count;
        var done = 0; var skipped = 0; var failed = 0;
        var saveLock = new SemaphoreSlim(1, 1);
        var parallelism = ResolveParallelism();

        await Parallel.ForEachAsync(
            repositories.Select((repo, i) => (repo, i)),
            new ParallelOptions { MaxDegreeOfParallelism = parallelism, CancellationToken = cancellationToken },
            async (item, ct) =>
            {
                var (repo, i) = item;
                if (string.IsNullOrEmpty(repo.Url) || string.IsNullOrEmpty(repo.Name))
                {
                    Console.Error.WriteLine($"[{i + 1}/{total}] Ignorado: sem Url/Name.");
                    Interlocked.Increment(ref failed);
                    return;
                }
                if (resume && repo.CkClassRows > 0) { Interlocked.Increment(ref skipped); return; }

                Console.WriteLine($"[{i + 1}/{total}] {repo.Name}");
                try
                {
                    await Sprint1CkRunnerCore.RunSingleAsync(repo, repoRoot, ckJar, evidencePerRepo, ct);
                    Interlocked.Increment(ref done);

                    await saveLock.WaitAsync(ct);
                    try { CsvExporter.SaveToCsv(repositories, "repositorios_processo.csv"); }
                    finally { saveLock.Release(); }
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref failed);
                    Console.Error.WriteLine($"  falhou [{repo.Name}]: {ex.Message}");
                }
            });

        Console.WriteLine($"CK batch: OK={done}, ignorados (resume)={skipped}, falhas={failed}.");
        return (done, skipped, failed);
    }
}
