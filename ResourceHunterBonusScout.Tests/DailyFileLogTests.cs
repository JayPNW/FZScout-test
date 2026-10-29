using Microsoft.Extensions.Time.Testing;
using ResourceHunterBonusScout.Services;

namespace ResourceHunterBonusScout.Tests;

public sealed class DailyFileLogTests : IDisposable
{
    readonly string folder = Path.Combine(Path.GetTempPath(), $"RHBS-{Guid.NewGuid()}");
    readonly FakeTimeProvider time = new(new DateTimeOffset(2026, 10, 28, 19, 20, 1, TimeSpan.Zero));
    readonly DailyFileLog log;

    public DailyFileLogTests()
    {
        time.SetLocalTimeZone(TimeZoneInfo.Utc);
        log = new(folder, time);
    }

    public void Dispose()
    {
        if (Directory.Exists(folder))
            Directory.Delete(folder, recursive: true);
    }

    [Fact]
    public void OneFilePerDay()
    {
        log.Write("first");
        time.Advance(TimeSpan.FromDays(1));
        log.Write("second");

        Assert.Equal(["2026-10-28 19:20:01 first"], File.ReadAllLines(Path.Combine(folder, "2026-10-28.log")));
        Assert.Equal(["2026-10-29 19:20:01 second"], File.ReadAllLines(log.TodaysFile));
    }

    [Fact]
    public void KeepsFourteenDays()
    {
        Directory.CreateDirectory(folder);
        foreach (var name in new[] { "2026-10-14.log", "2026-10-15.log", "notes.log" })
            File.WriteAllText(Path.Combine(folder, name), "");

        log.Write("hello");

        Assert.Equal(["2026-10-15.log", "2026-10-28.log", "notes.log"], Directory.GetFiles(folder).Select(Path.GetFileName).Order());
    }
}
