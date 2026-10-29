using System.Globalization;

namespace ResourceHunterBonusScout.Services;

/// <summary>One log file per day, kept for <see cref="DaysKept"/> days.</summary>
public class DailyFileLog : IAppLog
{
    public const int DaysKept = 14;
    const string DateFormat = "yyyy-MM-dd";

    readonly string _folder;
    readonly TimeProvider _time;
    readonly Lock _writing = new();
    DateOnly? _prunedOn;

    public DailyFileLog(string folder, TimeProvider time)
    {
        this._folder = folder;
        this._time = time;
    }

    public string TodaysFile => FileFor(Today);

    DateOnly Today => DateOnly.FromDateTime(this._time.GetLocalNow().DateTime);

    string FileFor(DateOnly date) => Path.Combine(this._folder, $"{date.ToString(DateFormat, CultureInfo.InvariantCulture)}.log");

    public void LogInformation(string message)
    {
        var now = this._time.GetLocalNow();
        lock (this._writing)
        {
            try
            {
                Directory.CreateDirectory(this._folder);
                if (this._prunedOn != Today)
                {
                    Prune();
                }

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
        this._prunedOn = Today;
        var oldest = Today.AddDays(1 - DaysKept);
        foreach (var file in Directory.GetFiles(this._folder, "*.log"))
        {
            if (DateOnly.TryParseExact(Path.GetFileNameWithoutExtension(file), DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                && date < oldest)
            {
                File.Delete(file);
            }
        }
    }
}
