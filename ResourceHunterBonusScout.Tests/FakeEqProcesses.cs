using ResourceHunterBonusScout.Services;

namespace ResourceHunterBonusScout.Tests;

class FakeEqProcesses : IEqProcesses
{
    public List<EqProcess> Running { get; } = [];
    public IReadOnlyList<EqProcess> Find() => [.. Running];
}
