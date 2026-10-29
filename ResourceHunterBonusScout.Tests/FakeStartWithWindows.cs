using ResourceHunterBonusScout.Services;

namespace ResourceHunterBonusScout.Tests;

class FakeStartWithWindows : IStartWithWindows
{
    public bool IsEnabled { get; set; }
}
