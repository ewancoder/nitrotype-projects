using System.Text.Json;

namespace NitroType.Team;

public sealed class NitroTypeApiClient
{
    private List<ApiData> _data = [];
    private static readonly TimeSpan _deltaInterval = TimeSpan.FromDays(1);

    public NitroTypeApiClient()
    {
        _ = Task.Run(async () =>
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromMinutes(1));
                try
                {
                    _ = await GetCatsInfoAsync();
                }
                catch (Exception exception)
                {
                    Console.WriteLine("Error: " + exception);
                }
            }
        });
    }

    public async ValueTask<IEnumerable<CatInfo>> GetCatsInfoAsync()
    {
        if (_data.Count == 0 || DateTime.UtcNow - _data.Last().Timestamp > TimeSpan.FromMinutes(1))
        {
            var client = new HttpClient();
            var data = await client.GetAsync("https://www.nitrotype.com/api/v2/teams/KECATS");
            var json = await data.Content.ReadAsStringAsync();
            var apiData = JsonSerializer.Deserialize<ApiData>(json)
                ?? throw new InvalidOperationException("Could not deserialize response from NitroType.");

            apiData.Timestamp = DateTime.UtcNow;
            _data.Add(apiData);
        }

        var previous = _data.Where(x => x.Timestamp < DateTime.UtcNow - _deltaInterval).FirstOrDefault();
        if (previous == null) previous = _data.First();
        var current = _data.Last();

        var result = _data.Last().results.season.Select(cat =>
        {
            var previousCat = previous.results.season.FirstOrDefault(x => x.username == cat.username);
            if (previous == null)
                return cat with { CurrentDelta = new() };

            return cat with
            {
                CurrentDelta = new Delta
                {
                    Accuracy = cat.Accuracy - previousCat.Accuracy,
                    Speed = cat.AverageSpeed - previousCat.AverageSpeed,
                    CharactersTyped = cat.typed - previousCat.typed,
                    Races = cat.played - previousCat.played
                }
            };
        }).ToList();

        if (result.Any(x => x.CurrentDelta.CharactersTyped < 0))
        {
            Console.WriteLine("Erasing season, it has ended.");
            _data = [];
            return await GetCatsInfoAsync();
        }

        return result;
    }
}

public sealed record ApiData(ApiResults results)
{
    public DateTime Timestamp { get; set; }
}

public sealed record ApiResults(CatInfo[] season);

public sealed record CatInfo(string username, string displayName, long typed, long errs, long played, long secs)
{
    public string Name => string.IsNullOrWhiteSpace(displayName) ? username : displayName;
    public decimal Accuracy => 100m * (typed - errs) / typed;
    public decimal AverageTextLength => (decimal)typed / played;
    public decimal AverageSpeed => (60m / 5) * typed / secs;
    public decimal AccuracyImprovement { get; set; } = 0m;
    public decimal AverageSpeedImprovement { get; set; } = 0m;

    public Delta CurrentDelta { get; set; }

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

    public string GetDeltaClass(decimal value)
    {
        if (value > 0) return "good";
        if (value < 0) return "bad";
        return string.Empty;
    }


}

public sealed record Delta
{
    public decimal Accuracy { get; set; }
    public decimal Speed { get; set; }
    public long CharactersTyped { get; set; }
    public long Races { get; set; }

    public string AccuracyString => ValueToString(Accuracy);
    public string SpeedString => ValueToString(Speed);
    public string CharactersTypedString => LongToString(CharactersTyped);
    public string RacesString => LongToString(Races);

    private string LongToString(long value)
    {
        if (value > 0) return $"\u25b2 {value.ToString()}";
        if (value < 0) return $"\u25bc {(-value).ToString()}";

        return string.Empty;
    }

    private string ValueToString(decimal value)
    {
        if (value > 0) return $"\u25b2 {value.ToString("#.#####")}";
        if (value < 0) return $"\u25bc {(-value).ToString("#.#####")}";

        return string.Empty;
    }
}
