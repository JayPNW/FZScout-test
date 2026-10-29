namespace ResourceHunterBonusScout.Services;

public record EqProcess(int Id, string ExePath);

/// <summary>The running <c>eqgame.exe</c> processes.</summary>
public interface IEqProcesses
{
    IReadOnlyList<EqProcess> Find();
}
