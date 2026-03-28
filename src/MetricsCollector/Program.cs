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
        if (!ckOnly && (string.IsNullOrWhiteSpace(githubToken) || githubToken == "SEU_TOKEN_AQUI"))
            Console.WriteLine("Aviso: defina GITHUB_TOKEN (ou GH_TOKEN) no ambiente; com .env, carregue-o antes (o Load acima injeta no processo).");

        List<RepositoryData> repositories;
        var repoRoot = RepoLayout.FindRepoRoot();

        if (ckOnly)
        {
            Console.WriteLine("Modo --ck-only: lendo data/repositorios_processo.csv e executando só clone + CK.");
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
                    $"Lab02S01 batch: CSV em data/repositorios_processo.csv (coluna CkClassRows para --ck-resume). Evidências: só com --ck-evidence.");
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
