using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MetricsCollector.Models;
using Octokit;

namespace MetricsCollector
{
    public static class RepositoryCollector
    {
        public static async Task<List<RepositoryData>> CollectRepositories(GitHubClient client)
        {
            var list = new List<RepositoryData>();
            
            var starRanges = new List<string>
            {
                ">=100000",
                "70000..99999",
                "50000..69999",
                "40000..49999",
                "30000..39999",
                "25000..29999",
                "20000..24999",
                "15000..19999",
                "12000..14999",
                "10000..11999",
            };

            Console.WriteLine($"Iniciando coleta em {starRanges.Count} intervalos de estrelas...");

            foreach (var range in starRanges)
            {
                Console.WriteLine($"Buscando repositórios com estrelas: {range}");
                var request = new SearchRepositoriesRequest($"language:java stars:{range}")
                {
                    SortField = RepoSearchSort.Stars,
                    Order = SortDirection.Descending,
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
                        AgeInYears = (int)((DateTime.Now - repo.CreatedAt.DateTime).TotalDays / 365.25),
                        Url = repo.CloneUrl,
                        ReleasesCount = releases.Count 
                    });
                }
                
                Console.WriteLine($"Intervalo '{range}' processado. Aguardando 30 segundos para evitar rate limit...");
                await Task.Delay(30000); 
            }

            return list;
        }
    }
}
