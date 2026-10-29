using Microsoft.Extensions.Time.Testing;
using ResourceHunterBonusScout.Services;

namespace ResourceHunterBonusScout.Tests;

public sealed class DailyFileLogTests : IDisposable
{
    readonly string _folder = Path.Combine(Path.GetTempPath(), $"RHBS-{Guid.NewGuid()}");
    readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 10, 28, 19, 20, 1, TimeSpan.Zero));
    readonly DailyFileLog _log;

    public DailyFileLogTests()
    {
        this._time.SetLocalTimeZone(TimeZoneInfo.Utc);
        this._log = new(this._folder, this._time);
    }

    public void Dispose()
    {
        if (Directory.Exists(this._folder))
        {
            Directory.Delete(this._folder, recursive: true);
        }
    }

    [Fact]
    public void OneFilePerDay()
    {
        this._log.LogInformation("first");
        this._time.Advance(TimeSpan.FromDays(1));
        this._log.LogInformation("second");

        Assert.Equal(["2026-10-28 19:20:01 first"], File.ReadAllLines(Path.Combine(this._folder, "2026-10-28.log")));
        Assert.Equal(["2026-10-29 19:20:01 second"], File.ReadAllLines(this._log.TodaysFile));
    }

    [Fact]
    public void KeepsFourteenDays()
    {
        Directory.CreateDirectory(this._folder);
        foreach (var name in new[] { "2026-10-14.log", "2026-10-15.log", "notes.log" })
        {
            File.WriteAllText(Path.Combine(this._folder, name), "");
        }

        this._log.LogInformation("hello");

        Assert.Equal(["2026-10-15.log", "2026-10-28.log", "notes.log"], Directory.GetFiles(this._folder).Select(Path.GetFileName).Order());
    }
}
