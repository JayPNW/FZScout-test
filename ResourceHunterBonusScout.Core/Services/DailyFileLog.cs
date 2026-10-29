using System.Globalization;

namespace ResourceHunterBonusScout.Services;

/// <summary>One log file per day, kept for <see cref="DaysKept"/> days.</summary>
public class DailyFileLog : IAppLog
{
    public const int DaysKept = 14;
    const string DateFormat = "yyyy-MM-dd";

    readonly string folder;
    readonly TimeProvider time;
    readonly Lock writing = new();
    DateOnly? prunedOn;

    public DailyFileLog(string folder, TimeProvider time)
    {
        this.folder = folder;
        this.time = time;
    }

    public string TodaysFile => FileFor(Today);

    DateOnly Today => DateOnly.FromDateTime(time.GetLocalNow().DateTime);

    string FileFor(DateOnly date) => Path.Combine(folder, $"{date.ToString(DateFormat, CultureInfo.InvariantCulture)}.log");

    public void Write(string message)
    {
        var now = time.GetLocalNow();
        lock (writing)
        {
            try
            {
                Directory.CreateDirectory(folder);
                if (prunedOn != Today)
                    Prune();
                File.AppendAllText(TodaysFile, $"{now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)} {message}{Environment.NewLine}");
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                // Logging must never stop the app.
            }
        }
    }

    void Prune()
    {
        prunedOn = Today;
        var oldest = Today.AddDays(1 - DaysKept);
        foreach (var file in Directory.GetFiles(folder, "*.log"))
        {
            if (DateOnly.TryParseExact(Path.GetFileNameWithoutExtension(file), DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                && date < oldest)
                File.Delete(file);
        }
    }
}
