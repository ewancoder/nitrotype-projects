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

        return _cache.results.season;
    }
}

public sealed record ApiData(ApiResults results);

public sealed record ApiResults(CatInfo[] season);

public sealed record CatInfo(string username, string displayName, long typed, long errs, long played, long secs)
{
    public string Name => string.IsNullOrWhiteSpace(displayName) ? username : displayName;
    public decimal Accuracy => 100m * (typed - errs) / typed;
    public decimal AverageTextLength => (decimal)typed / played;
    public decimal AverageSpeed => (60m / 5) * typed / secs;
}
