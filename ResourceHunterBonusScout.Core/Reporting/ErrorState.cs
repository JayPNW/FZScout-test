namespace ResourceHunterBonusScout.Reporting;

/// <summary>Whether the last report or data load failed. Shown as the Error icon.</summary>
public class ErrorState
{
    public bool LastOperationFailed { get; private set; }

    public void Succeeded() => LastOperationFailed = false;

    public void Failed() => LastOperationFailed = true;
}
