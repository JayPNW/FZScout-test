using ResourceHunterBonusScout.Reporting;
using ResourceHunterBonusScout.Site;

namespace ResourceHunterBonusScout;

/// <summary>
/// Asks the user for an API key whenever the reporter needs one: at launch if there's none, or whenever the site rejects it.
/// Waits for the answer in the background so the app keeps watching logs meanwhile.
/// </summary>
sealed class ApiKeyPrompt
{
    readonly BonusReporter _reporter;
    readonly Log _log;
    bool _needed;
    Task<string>? _answer;

    public ApiKeyPrompt(BonusReporter reporter, Log log)
    {
        this._reporter = reporter;
        this._log = log;
        reporter.ApiKeyNeeded += () => this._needed = true;
    }

    /// <summary>Call every tick. Asks if needed, and uses the answer once it comes.</summary>
    public async Task TickAsync(CancellationToken cancellationToken)
    {
        if (this._needed && this._answer is null)
        {
            this._needed = false;
            Console.Write("Paste your API key (it starts with fz_live_; see docs/create-api-key.md), or press Enter to skip: ");
            this._answer = Task.Run(ReadHidden, cancellationToken);
        }

        if (this._answer is not { IsCompleted: true })
        {
            return;
        }

        var text = (await this._answer).Trim();
        this._answer = null;
        if (text.Length == 0)
        {
            this._log.Warning("No API key entered, so nothing will be reported. Restart the app to enter one.");
            return;
        }

        var apiKey = new ApiKey(text);
        ApiKeyStore.Save(apiKey);
        this._log.Info("API key saved to Windows Credential Manager.");
        await this._reporter.ChangeApiKeyAsync(apiKey, cancellationToken);
    }

    /// <summary>Reads a line without echoing it, so the key doesn't end up on screen.</summary>
    static string ReadHidden()
    {
        if (Console.IsInputRedirected)
        {
            return Console.ReadLine() ?? string.Empty;
        }

        var text = new System.Text.StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                return text.ToString();
            }

            if (key.Key == ConsoleKey.Backspace && text.Length > 0)
            {
                text.Length--;
            }
            else if (!char.IsControl(key.KeyChar))
            {
                text.Append(key.KeyChar);
            }
        }
    }
}
