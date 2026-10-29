namespace ResourceHunterBonusScout.Services;

/// <summary>The running <c>eqgame.exe</c> processes.</summary>
public interface IEqProcesses
{
    IReadOnlyList<EqProcess> Find();
}
