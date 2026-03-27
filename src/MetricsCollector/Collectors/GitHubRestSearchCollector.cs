using System.Net.Http.Headers;
using System.Text.Json;
using MetricsCollector.Models;

namespace MetricsCollector;

/// <summary>
/// Lista até 1.000 repositórios Java via <c>GET /search/repositories</c> (REST).
/// A API permite no máximo 10 páginas × 100 = 1.000 resultados por query.
/// O GraphQL com <c>releases { totalCount }</c> por nó costumava encerrar a paginação cedo (~250 itens).
/// </summary>
public static class GitHubRestSearchCollector
{
    public const int MaxRepositories = 1000;
    private const int PerPage = 100;
    private const int MaxPages = 10;
    private const string SearchQuery = "language:java"; // sort/order vêm nos query params

    public static async Task<List<RepositoryData>> CollectAsync(HttpClient http, CancellationToken cancellationToken = default)
    {
        var list = new List<RepositoryData>(MaxRepositories);

        for (var page = 1; page <= MaxPages && list.Count < MaxRepositories; page++)
        {
            var qs = $"q={Uri.EscapeDataString(SearchQuery)}" +
                     $"&sort=stars" +
                     $"&order=desc" +
                     $"&per_page={PerPage}" +
                     $"&page={page}";

            var url = $"https://api.github.com/search/repositories?{qs}";
            using var response = await http.GetAsync(url, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"GitHub Search HTTP {(int)response.StatusCode}: {body}");

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            if (!root.TryGetProperty("items", out var items))
                break;

            var got = 0;
            foreach (var item in items.EnumerateArray())
            {
                if (!item.TryGetProperty("full_name", out var fnEl)) continue;
                var name = fnEl.GetString();
                if (string.IsNullOrEmpty(name)) continue;

                var stars = item.TryGetProperty("stargazers_count", out var st) ? st.GetInt32() : 0;
                DateTime created = default;
                if (item.TryGetProperty("created_at", out var cr) && cr.ValueKind == JsonValueKind.String)
                    _ = DateTime.TryParse(cr.GetString(), null, System.Globalization.DateTimeStyles.RoundtripKind, out created);

                var cloneUrl = item.TryGetProperty("clone_url", out var cu) ? cu.GetString() : $"https://github.com/{name}.git";

                list.Add(new RepositoryData
                {
                    Name = name,
                    Stars = stars,
                    CreatedAt = created,
                    AgeInYears = (int)((DateTime.UtcNow - created.ToUniversalTime()).TotalDays / 365.25),
                    Url = cloneUrl,
                    ReleasesCount = 0 // Search REST não traz releases; enriquecimento opcional depois
                });

                got++;
                if (list.Count >= MaxRepositories)
                    break;
            }

            var totalCount = root.TryGetProperty("total_count", out var tc) ? tc.GetInt32() : 0;
            Console.WriteLine($"Página {page}/{MaxPages}: +{got} repos (acumulado {list.Count}/{MaxRepositories}; total_count API: {totalCount}).");

            if (got < PerPage)
                break;

            // Search REST: ~30 req/min autenticado — folga entre páginas
            if (page < MaxPages && list.Count < MaxRepositories)
                await Task.Delay(2100, cancellationToken);
        }

        return list;
    }

    public static void ConfigureHttp(HttpClient http, string? token)
    {
        http.DefaultRequestHeaders.UserAgent.ParseAdd("PUC-Minas-Lab02-MetricsCollector");
        http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        if (!string.IsNullOrWhiteSpace(token) && token != "SEU_TOKEN_AQUI")
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}
