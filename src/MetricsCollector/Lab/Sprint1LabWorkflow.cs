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

    /// <summary>CK em todos os repositórios; grava CSV após cada sucesso. <paramref name="resume"/> ignora linhas com <see cref="RepositoryData.CkClassRows"/> &gt; 0.</summary>
    public static async Task<(int done, int skipped, int failed)> RunCkAllAsync(
        List<RepositoryData> repositories,
        string repoRoot,
        bool resume,
        bool evidencePerRepo,
        CancellationToken cancellationToken = default)
    {
        var ckJar = RequireCkJar();
        var done = 0;
        var skipped = 0;
        var failed = 0;
        var total = repositories.Count;

        for (var i = 0; i < total; i++)
        {
            var repo = repositories[i];
            if (string.IsNullOrEmpty(repo.Url) || string.IsNullOrEmpty(repo.Name))
            {
                Console.Error.WriteLine($"[{i + 1}/{total}] Ignorado: sem Url/Name.");
                failed++;
                continue;
            }

            if (resume && repo.CkClassRows > 0)
            {
                skipped++;
                continue;
            }

            Console.WriteLine($"[{i + 1}/{total}] {repo.Name}");
            try
            {
                await Sprint1CkRunnerCore.RunSingleAsync(repo, repoRoot, ckJar, evidencePerRepo, cancellationToken);
                done++;
                CsvExporter.SaveToCsv(repositories, "repositorios_processo.csv");
            }
            catch (Exception ex)
            {
                failed++;
                Console.Error.WriteLine($"  falhou: {ex.Message}");
            }
        }

        Console.WriteLine($"CK batch: OK={done}, ignorados (resume)={skipped}, falhas={failed}.");
        return (done, skipped, failed);
    }
}
