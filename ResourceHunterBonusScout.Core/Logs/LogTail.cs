using System.Text;

namespace ResourceHunterBonusScout.Logs;

/// <summary>Reads lines as they're added to a log file.</summary>
public class LogTail(string path, long position)
{
    readonly StringBuilder partialLine = new();

    public string Path => path;

    /// <returns>The complete lines added since the last read. A line without its newline yet waits for the next read.</returns>
    public IReadOnlyList<string> ReadNewLines()
    {
        string text;
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            if (stream.Length < position)
            {
                // The file was replaced or truncated.
                position = 0;
                partialLine.Clear();
            }
            if (stream.Length == position)
                return [];

            stream.Position = position;
            var bytes = new byte[stream.Length - position];
            stream.ReadExactly(bytes);
            position += bytes.Length;
            text = Encoding.Latin1.GetString(bytes);
        }
        catch (IOException)
        {
            return [];
        }
        catch (UnauthorizedAccessException)
        {
            return [];
        }

        partialLine.Append(text);
        var all = partialLine.ToString();
        var end = all.LastIndexOf('\n');
        if (end < 0)
            return [];

        partialLine.Clear().Append(all[(end + 1)..]);
        return all[..end].Split('\n').Select(line => line.TrimEnd('\r')).ToList();
    }
}
