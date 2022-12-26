using ZENBitPackToolbox.Managers;

namespace ZENBitPackToolbox.Models;

public class Spvar
{
    private readonly StateManager _stateManager;
    public Spvar(StateManager stateManager, string name)
    {
        _stateManager = stateManager;
        Name = name;
    }

    public string Name { get; }
    public int CurrentValue { get; private set; }
    public int ExpectedValue { get; private set; }

    public void SetCurrentValue(int value)
    {
        if (value != CurrentValue)
        {
            CurrentValue = value;
        }
    }

    public void SetExpectedValue(int value)
    {
        if (value != ExpectedValue)
        {
            ExpectedValue = value;
        }
    }
}
