using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MetricsCollector.Models;
using Octokit;
using CsvHelper;
using DotNetEnv;
using System.Globalization;

namespace MetricsCollector
{
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

            var client = new GitHubClient(new ProductHeaderValue("PUC-Minas-Lab02"));
            
            if (!string.IsNullOrEmpty(githubToken) && githubToken != "SEU_TOKEN_AQUI")
            {
                client.Credentials = new Credentials(githubToken);
            }

            Console.WriteLine("Iniciando busca pelos 1.000 repositórios Java mais populares.");

            var repositories = await RepositoryCollector.CollectRepositories(client);

            Console.WriteLine($"Coleta concluída. {repositories.Count} repositórios encontrados.");
            
            CsvExporter.SaveToCsv(repositories, "repositorios_processo.csv");
        }
    }
}