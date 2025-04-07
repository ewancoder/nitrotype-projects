using System.Text.Json;

namespace NitroType.Team;

public sealed class NitroTypeApiClient
{
    private ApiData _previousData;
    private DateTime _previousDataQueriedAt;
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

        if (DateTime.UtcNow - _previousDataQueriedAt > TimeSpan.FromDays(1))
        {
            _previousData = _cache;
            _previousDataQueriedAt = DateTime.UtcNow;
        }

        foreach (var catInfo in _cache.results.season)
        {
            var previous = _previousData.results.season.FirstOrDefault(x => x.username == catInfo.username);
            if (previous != null)
            {
                catInfo.AccuracyImprovement = catInfo.Accuracy - previous.Accuracy;
                catInfo.AverageSpeedImprovement = catInfo.AverageSpeed - previous.AverageSpeed;
            }
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
    public decimal AccuracyImprovement { get; set; } = 0m;
    public decimal AverageSpeedImprovement { get; set; } = 0m;

    public string TimeSpent
    {
        get
        {
            var time = TimeSpan.FromSeconds(secs);
            var parts = new List<string>();
            if (time.Days > 0)
                parts.Add($"{time.Days} day{(time.Days > 1 ? "s" : "")}");
            if (time.Hours > 0)
                parts.Add($"{time.Hours} hour{(time.Hours > 1 ? "s" : "")}");
            if (time.Minutes > 0)
                parts.Add($"{time.Minutes} minute{(time.Minutes > 1 ? "s" : "")}");
            if (time.Seconds > 0)
                parts.Add($"{time.Seconds} second{(time.Minutes > 1 ? "s" : "")}");

            return string.Join(" ", parts);
        }
    }

    public string AccuracyImprovementString
    {
        get
        {
            if (AccuracyImprovement > 0) return $"\u25b2 {AccuracyImprovement.ToString("#.#####")}";
            if (AccuracyImprovement < 0) return $"\u25bc {(-AccuracyImprovement).ToString("#.#####")}";

            return string.Empty;
        }
    }

    public string AverageSpeedImprovementString
    {
        get
        {
            if (AverageSpeedImprovement > 0) return $"\u25b2 {AverageSpeedImprovement.ToString("#.#####")}";
            if (AverageSpeedImprovement < 0) return $"\u25bc {(-AverageSpeedImprovement).ToString("#.#####")}";

            return string.Empty;
        }
    }

    public string GetDeltaClass(decimal value)
    {
        if (value > 0) return "good";
        if (value < 0) return "bad";
        return string.Empty;
    }
}
