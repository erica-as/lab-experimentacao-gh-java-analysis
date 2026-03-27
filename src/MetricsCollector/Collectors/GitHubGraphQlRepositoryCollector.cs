using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using MetricsCollector.Models;

namespace MetricsCollector;

/// <summary>
/// Busca repositórios Java via GraphQL (paginação por cursor).
/// A Search REST do GitHub limita a ~1000 resultados por query, mas é preciso paginar;
/// a implementação anterior usava várias faixas de estrelas e só a primeira página de cada (~247 itens).
/// </summary>
public static class GitHubGraphQlRepositoryCollector
{
    private const string GraphQlEndpoint = "https://api.github.com/graphql";
    public const int MaxRepositories = 1000;

    /// <summary> Busca no GitHub: Java, ordenadas por estrelas (decrescente). Se a API rejeitar, há fallback. </summary>
    private const string JavaStarsQueryPrimary = "language:java sort:stars-desc";

    private const string JavaStarsQueryFallback = "language:java";

    private static readonly string SearchQuery = """
        query($q: String!, $cursor: String) {
          search(query: $q, type: REPOSITORY, first: 100, after: $cursor) {
            pageInfo {
              hasNextPage
              endCursor
            }
            nodes {
              ... on Repository {
                nameWithOwner
                stargazerCount
                createdAt
                releases(first: 1) {
                  totalCount
                }
              }
            }
          }
        }
        """;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<List<RepositoryData>> CollectAsync(HttpClient http, CancellationToken cancellationToken = default)
    {
        var list = new List<RepositoryData>(MaxRepositories);
        string? cursor = null;
        var page = 0;
        var searchQuery = JavaStarsQueryPrimary;
        var usedFallback = false;

        while (list.Count < MaxRepositories)
        {
            page++;
            var payload = new
            {
                query = SearchQuery,
                variables = new { q = searchQuery, cursor }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, GraphQlEndpoint)
            {
                Content = JsonContent.Create(payload, options: JsonOptions)
            };

            using var response = await http.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"GraphQL HTTP {(int)response.StatusCode}: {body}");

            var root = JsonSerializer.Deserialize<GraphQlEnvelope>(body, JsonOptions);
            if (root?.Errors is { Count: > 0 })
            {
                var msg = string.Join("; ", root.Errors.Select(e => e.Message));
                if (!usedFallback && searchQuery == JavaStarsQueryPrimary)
                {
                    Console.WriteLine($"Aviso: query com sort falhou ({msg}). Tentando '{JavaStarsQueryFallback}'.");
                    usedFallback = true;
                    searchQuery = JavaStarsQueryFallback;
                    cursor = null;
                    page = 0;
                    continue;
                }

                throw new InvalidOperationException($"GraphQL: {msg}");
            }

            var search = root?.Data?.Search;
            if (search?.Nodes is null)
                break;

            foreach (var node in search.Nodes)
            {
                if (node is null || string.IsNullOrEmpty(node.NameWithOwner))
                    continue;

                var created = node.CreatedAt?.UtcDateTime ?? default;
                list.Add(new RepositoryData
                {
                    Name = node.NameWithOwner,
                    Stars = node.StargazerCount,
                    CreatedAt = created,
                    AgeInYears = (int)((DateTime.UtcNow - created).TotalDays / 365.25),
                    Url = $"https://github.com/{node.NameWithOwner}.git",
                    ReleasesCount = node.Releases?.TotalCount ?? 0
                });

                if (list.Count >= MaxRepositories)
                    break;
            }

            Console.WriteLine($"Página {page}: +{search.Nodes.Count(n => n?.NameWithOwner is not null)} repos (total {list.Count}/{MaxRepositories}).");

            var pi = search.PageInfo;
            if (pi is not { HasNextPage: true } || string.IsNullOrEmpty(pi.EndCursor))
                break;

            cursor = pi.EndCursor;

            // GraphQL cost: pequena pausa para secondary rate limit
            await Task.Delay(150, cancellationToken);
        }

        return list;
    }

    public static void ConfigureHttp(HttpClient http, string? token)
    {
        http.DefaultRequestHeaders.UserAgent.ParseAdd("PUC-Minas-Lab02-MetricsCollector");
        http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        if (!string.IsNullOrWhiteSpace(token) && token != "SEU_TOKEN_AQUI")
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private sealed class GraphQlEnvelope
    {
        public GraphQlData? Data { get; set; }
        public List<GraphQlErrorItem>? Errors { get; set; }
    }

    private sealed class GraphQlData
    {
        public SearchPayload? Search { get; set; }
    }

    private sealed class SearchPayload
    {
        public PageInfoPayload? PageInfo { get; set; }
        public List<RepositoryNode?>? Nodes { get; set; }
    }

    private sealed class PageInfoPayload
    {
        public bool HasNextPage { get; set; }
        public string? EndCursor { get; set; }
    }

    private sealed class RepositoryNode
    {
        public string? NameWithOwner { get; set; }
        public int StargazerCount { get; set; }

        public DateTimeOffset? CreatedAt { get; set; }

        public ReleasesPayload? Releases { get; set; }
    }

    private sealed class ReleasesPayload
    {
        public int TotalCount { get; set; }
    }

    private sealed class GraphQlErrorItem
    {
        public string? Message { get; set; }
    }
}
