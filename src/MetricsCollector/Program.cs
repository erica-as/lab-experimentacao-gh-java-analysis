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
            DotNetEnv.Env.Load();
            var githubToken = DotNetEnv.Env.GetString("GITHUB_TOKEN");

            var client = new GitHubClient(new ProductHeaderValue("PUC-Minas-Lab02"));
            
            if (!string.IsNullOrEmpty(githubToken) && githubToken != "SEU_TOKEN_AQUI")
            {
                client.Credentials = new Credentials(githubToken);
            }

            Console.WriteLine("Iniciando busca pelos 1.000 repositórios Java mais populares.");

            var repositories = await CollectRepositories(client);

            Console.WriteLine($"Coleta concluída. {repositories.Count} repositórios encontrados.");
            
            SaveToCsv(repositories, "data/repositorios_processo.csv");
        }

        static async Task<List<RepositoryData>> CollectRepositories(GitHubClient client)
        {
            var list = new List<RepositoryData>();
            
            for (int i = 1; i <= 10; i++)
            {
                var request = new SearchRepositoriesRequest("language:java")
                {
                    SortField = RepoSearchSort.Stars,
                    Order = SortDirection.Descending,
                    Page = i,
                    PerPage = 100
                };

                var result = await client.Search.SearchRepo(request);

                foreach (var repo in result.Items)
                {
                    var releases = await client.Repository.Release.GetAll(repo.Owner.Login, repo.Name);
                    
                    list.Add(new RepositoryData
                    {
                        Name = repo.FullName,
                        Stars = repo.StargazersCount,
                        CreatedAt = repo.CreatedAt.DateTime,
                        AgeInYears = (int)((DateTime.Now - repo.CreatedAt.DateTime).TotalDays / 365.25), // RQ 02: Maturidade 
                        Url = repo.CloneUrl,
                        ReleasesCount = releases.Count 
                    });
                }
                
                Console.WriteLine($"Pagina {i} processada.");
                await Task.Delay(1000); 
            }

            return list;
        }

        static void SaveToCsv(List<RepositoryData> data, string fileName)
        {
            using (var writer = new StreamWriter(fileName))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(data);
            }
            Console.WriteLine($"Arquivo {fileName} gerado com sucesso.");
        }
    }
}