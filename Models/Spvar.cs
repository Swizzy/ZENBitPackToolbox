namespace ZENBitPackToolbox.Models;

public class Spvar
{
    public Spvar()
    {

    }

    public Spvar(string name)
    {
        Name = name;
    }

    public string Name { get; set; }
    public int CurrentValue { get; set; }
    public int ExpectedValue { get; set; }

    public void SetCurrentValue(int value)
    {
        Console.WriteLine($"Updating current value for SPVAR, new value: {value}, previous value: {CurrentValue}");
        CurrentValue = value;
    }

    public void SetExpectedValue(int value) => ExpectedValue = value;
}
