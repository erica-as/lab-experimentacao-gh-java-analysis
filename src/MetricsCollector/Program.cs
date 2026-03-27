using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using DotNetEnv;
using MetricsCollector.Lab;

namespace MetricsCollector;

class Program
{
    static async Task Main(string[] args)
    {
        var collectOnly = args.Contains("--collect-only", StringComparer.OrdinalIgnoreCase);

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
        if (string.IsNullOrWhiteSpace(githubToken) || githubToken == "SEU_TOKEN_AQUI")
            Console.WriteLine("Aviso: defina GITHUB_TOKEN (ou GH_TOKEN) no ambiente; com .env, carregue-o antes (o Load acima injeta no processo).");

        using var http = new HttpClient();
        GitHubRestSearchCollector.ConfigureHttp(http, githubToken);

        Console.WriteLine($"Iniciando busca pelos {GitHubRestSearchCollector.MaxRepositories} repositórios Java (REST Search, até 10 páginas × 100).");

        var repositories = await GitHubRestSearchCollector.CollectAsync(http);

        Console.WriteLine($"Coleta concluída. {repositories.Count} repositórios encontrados.");

        CsvExporter.SaveToCsv(repositories, "repositorios_processo.csv");

        if (collectOnly)
        {
            Console.WriteLine("Modo --collect-only: clone/CK não executados.");
            return;
        }

        var repoRoot = RepoLayout.FindRepoRoot();
        try
        {
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
        Console.WriteLine("Lab02S01: CSV principal atualizado com médias CK na linha da amostra + evidência em data/lab02s01_ck_evidence/");
    }
}
