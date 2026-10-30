namespace ResourceHunterBonusScout;

/// <summary>The app's log: timestamped lines on the console. Never pass it the API key.</summary>
sealed class Log(TextWriter writer, TimeProvider time)
{
    public void Info(string message) => this.Write(message);

    public void Warning(string message) => this.Write($"WARNING: {message}");

    public void Error(string message) => this.Write($"ERROR: {message}");

    void Write(string message)
    {
        writer.WriteLine($"{time.GetLocalNow():HH:mm:ss} {message}");
    }
}
