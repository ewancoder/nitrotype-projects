using System.Text.Json;

namespace NitroType.Team;

public sealed class NitroTypeApiClient
{
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
        var client = new HttpClient();
        var data = await client.GetAsync("https://api.tnt.typingrealm.com/api/statistics/kecats");
        var json = await data.Content.ReadAsStringAsync();
        var apiData = JsonSerializer.Deserialize<TntApiRecord[]>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("Could not deserialize response from NitroType.");

        var dx = apiData.Select(x => new CatInfo(x.Username, x.Name, x.Typed, x.Errors, x.RacesPlayed, x.Secs)).ToList();

        return dx;
    }
}

public sealed record TntApiRecord(
    string Username,
    string Team,
    long Typed,
    long Errors,
    string Name,
    long RacesPlayed,
    DateTime Timestamp,
    long Secs,
    decimal Accuracy,
    decimal AverageTextLength,
    decimal AverageSpeed,
    string TimeSpent);

public sealed record ApiData(ApiResults results)
{
    public DateTime Timestamp { get; set; }
}

public sealed record ApiResults(CatInfo[] season);

public sealed record CatInfo(string username, string displayName, long typed, long errs, long played, long secs)
{
    public string Name => string.IsNullOrWhiteSpace(displayName) ? username : displayName;
    public decimal Accuracy => typed == 0 ? 0 : 100m * (typed - errs) / typed;
    public decimal AverageTextLength => played == 0 ? 0 : (decimal)typed / played;
    // ReSharper disable once ArrangeRedundantParentheses
    public decimal AverageSpeed => secs == 0 ? 0 : (60m / 5) * typed / secs;


    public Delta CurrentDelta { get; set; }

    public string TimeSpent
    {
        get
        {
            var time = secs == null ? TimeSpan.Zero : TimeSpan.FromSeconds(secs);
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
