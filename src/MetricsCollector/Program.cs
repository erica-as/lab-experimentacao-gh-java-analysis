using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using DotNetEnv;
using MetricsCollector.Lab;
using MetricsCollector.Models;

namespace MetricsCollector;

class Program
{
    static async Task Main(string[] args)
    {
        var collectOnly = args.Contains("--collect-only", StringComparer.OrdinalIgnoreCase);
        var ckOnly = args.Contains("--ck-only", StringComparer.OrdinalIgnoreCase);
        var ckAll = args.Contains("--ck-all", StringComparer.OrdinalIgnoreCase);
        var ckResume = args.Contains("--ck-resume", StringComparer.OrdinalIgnoreCase);
        var ckEvidence = args.Contains("--ck-evidence", StringComparer.OrdinalIgnoreCase);
        var locOnly = args.Contains("--loc-only", StringComparer.OrdinalIgnoreCase);
        var locAll = args.Contains("--loc-all", StringComparer.OrdinalIgnoreCase);
        var locResume = args.Contains("--loc-resume", StringComparer.OrdinalIgnoreCase);

        string? FindEnv()
        {
            var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, ".env");
                if (File.Exists(candidate)) return candidate;
                dir = dir.Parent;
            }
            return null;
        }

        var envPath = FindEnv();
        if (!string.IsNullOrEmpty(envPath))
        {
            DotNetEnv.Env.Load(envPath);
            Console.WriteLine($".env carregado: {envPath}");
        }

        var githubToken =
            Environment.GetEnvironmentVariable("GITHUB_TOKEN")?.Trim()
            ?? Environment.GetEnvironmentVariable("GH_TOKEN")?.Trim();
        if (!ckOnly && !locOnly && (string.IsNullOrWhiteSpace(githubToken) || githubToken == "SEU_TOKEN_AQUI"))
            Console.WriteLine("Aviso: defina GITHUB_TOKEN (ou GH_TOKEN) no ambiente; com .env, carregue-o antes (o Load acima injeta no processo).");

        List<RepositoryData> repositories;
        var repoRoot = RepoLayout.FindRepoRoot();

        if (ckOnly || locOnly)
        {
            var modoLabel = locOnly ? "--loc-only" : "--ck-only";
            Console.WriteLine($"Modo {modoLabel}: lendo data/repositorios_processo.csv sem chamar a Search API.");
            try
            {
                repositories = CsvExporter.LoadFromCsv("repositorios_processo.csv");
            }
            catch (FileNotFoundException ex)
            {
                Console.Error.WriteLine(ex.Message);
                Environment.ExitCode = 2;
                return;
            }

            if (repositories.Count == 0)
            {
                Console.Error.WriteLine("CSV vazio.");
                Environment.ExitCode = 2;
                return;
            }

            if (locOnly)
                Console.WriteLine($"Linhas carregadas: {repositories.Count}. Git no PATH necessário para clone.");
            else
                Console.WriteLine($"Linhas carregadas: {repositories.Count}. CK_JAR + Java necessários.");
        }
        else
        {
            using var http = new HttpClient();
            GitHubRestSearchCollector.ConfigureHttp(http, githubToken);

            Console.WriteLine(
                $"Iniciando busca pelos {GitHubRestSearchCollector.MaxRepositories} repositórios Java (REST Search, até 10 páginas × 100).");

            repositories = await GitHubRestSearchCollector.CollectAsync(http);

            Console.WriteLine($"Coleta concluída. {repositories.Count} repositórios encontrados.");

            CsvExporter.SaveToCsv(repositories, "repositorios_processo.csv");

            if (collectOnly)
            {
                Console.WriteLine("Modo --collect-only: clone/CK não executados.");
                return;
            }
        }

        if (locOnly)
        {
            if (!locAll)
            {
                Console.Error.WriteLine("--loc-only requer --loc-all. Exemplo: dotnet run … -- --loc-only --loc-all [--loc-resume]");
                Environment.ExitCode = 1;
                return;
            }

            Console.WriteLine(
                "Modo --loc-only --loc-all: LOC em todos os repositórios (clone shallow por repo). " +
                (locResume ? "--loc-resume: ignora linhas com TotalLoc > 0. " : ""));
            var (done, skipped, failed) = await Sprint1LabWorkflow.RunLocAllAsync(
                repositories, repoRoot, locResume);
            if (failed > 0)
                Environment.ExitCode = 4;
            Console.WriteLine("LOC batch: CSV atualizado com TotalLoc e CommentLines.");
            return;
        }

        try
        {
            if (ckAll)
            {
                Console.WriteLine(
                    "Modo --ck-all: CK em todos os repositórios (demora). Grava CSV após cada sucesso. " +
                    (ckResume ? "--ck-resume: ignora linhas com CkClassRows > 0. " : "") +
                    (ckEvidence ? "--ck-evidence: copia CSV CK por repo para data/lab02s01_ck_evidence (muito disco). " : ""));
                var (done, skipped, failed) = await Sprint1LabWorkflow.RunCkAllAsync(
                    repositories, repoRoot, ckResume, ckEvidence);
                if (failed > 0)
                    Environment.ExitCode = 4;
                Console.WriteLine(
                    "Lab02S01 batch: CSV em data/repositorios_processo.csv (coluna CkClassRows para --ck-resume). " +
                    "ExitCode 4 indica falhas parciais — use --ck-resume para retomar. Evidências: só com --ck-evidence.");
                return;
            }

            await Sprint1LabWorkflow.RunCkSampleAsync(repositories, repoRoot);
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine("Lab02S01 (CK/CkSample): " + ex.Message);
            Console.Error.WriteLine("Dica: use --collect-only só para CSV; ou defina CK_JAR + Java no PATH / JAVA_HOME.");
            Environment.ExitCode = 3;
            return;
        }

        CsvExporter.SaveToCsv(repositories, "repositorios_processo.csv");
        Console.WriteLine("Lab02S01: CSV principal atualizado — amostra com evidência em data/lab02s01_ck_evidence/ (use --ck-all para todos).");
    }
}
