using System.Text.Json;

namespace NitroType.Team;

public sealed class NitroTypeApiClient
{
    private ApiData _cache;
    private DateTime _lastQueriedAt;

    public async ValueTask<IEnumerable<CatInfo>> GetCatsInfoAsync()
    {
        if (DateTime.UtcNow - _lastQueriedAt > TimeSpan.FromSeconds(20))
        {
            var client = new HttpClient();
            var data = await client.GetAsync("https://www.nitrotype.com/api/v2/teams/KECATS");
            var json = await data.Content.ReadAsStringAsync();
            var apiData = JsonSerializer.Deserialize<ApiData>(json);

            _cache = apiData;
            _lastQueriedAt = DateTime.UtcNow;
        }

        return _cache.results.season.Select(cat => new CatInfo(cat.Name, cat.Precision));
    }
}

public sealed record CatInfo(string Name, decimal Accuracy);

public sealed record ApiData(ApiResults results);

public sealed record ApiResults(ApiSeason[] season);

public sealed record ApiSeason(string username, string displayName, long typed, long errs)
{
    public string Name => string.IsNullOrWhiteSpace(displayName) ? username : displayName;
    public decimal Precision => 100m * (typed - errs) / typed;
}
