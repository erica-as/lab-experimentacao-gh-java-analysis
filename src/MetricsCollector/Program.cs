using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using DotNetEnv;

namespace MetricsCollector;

class Program
{
    static async Task Main(string[] args)
    {
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
        GitHubGraphQlRepositoryCollector.ConfigureHttp(http, githubToken);

        Console.WriteLine($"Iniciando busca pelos {GitHubGraphQlRepositoryCollector.MaxRepositories} repositórios Java (GraphQL + cursor).");

        var repositories = await GitHubGraphQlRepositoryCollector.CollectAsync(http);

        Console.WriteLine($"Coleta concluída. {repositories.Count} repositórios encontrados.");

        CsvExporter.SaveToCsv(repositories, "repositorios_processo.csv");
    }
}
