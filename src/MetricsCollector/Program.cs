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
        else
        {
            Console.WriteLine(".env não encontrado; tentando carregar padrão.");
            DotNetEnv.Env.Load();
        }

        var githubToken = DotNetEnv.Env.GetString("GITHUB_TOKEN");

        using var http = new HttpClient();
        GitHubGraphQlRepositoryCollector.ConfigureHttp(http, githubToken);

        Console.WriteLine($"Iniciando busca pelos {GitHubGraphQlRepositoryCollector.MaxRepositories} repositórios Java (GraphQL + cursor).");

        var repositories = await GitHubGraphQlRepositoryCollector.CollectAsync(http);

        Console.WriteLine($"Coleta concluída. {repositories.Count} repositórios encontrados.");

        CsvExporter.SaveToCsv(repositories, "repositorios_processo.csv");
    }
}
