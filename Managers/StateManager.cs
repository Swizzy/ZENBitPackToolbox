using Blazored.LocalStorage;
using ZENBitPackToolbox.Models;

namespace ZENBitPackToolbox.Managers;

public class StateManager
{
    private string KEY = "state";

    public StateManager(ILocalStorageService localStorage)
    {
        LocalStorage = localStorage;
    }

    private class State
    {
        public Dictionary<int, Spvar> Spvars { get; } = new();
        public List<Variable> Variables { get; } = new();
        public int StartSpvar { get; set; }
        public int StartBit { get; set; }
    }

    public ILocalStorageService LocalStorage { get; }

    private State CurrentState { get; set; } = new();

    public event EventHandler? StateChanged;

    public int TotalBitsUsed => CurrentState.Variables.Sum(v => v.TotalBits);
    public int TotalSpvarsUsed => (CurrentState.StartBit + TotalBitsUsed) / 32 + (CurrentState.StartBit + TotalBitsUsed % 32 > 0 ? 1 : 0);
    public int LastSpvarUsed => TotalSpvarsUsed + CurrentState.StartSpvar - (CurrentState.StartBit + TotalBitsUsed % 32 > 0 ? 1 : 0);

    public async Task LoadAsync()
    {
        try
        {
            CurrentState = await LocalStorage.GetItemAsync<State>(KEY) ?? new State();
            await DefaultStateIfNeededAsync();
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
        catch
        {
            await ResetAsync();
        }
    }

    private async Task SaveAsync() => await LocalStorage.SetItemAsync(KEY, CurrentState);

    public async Task ResetAsync()
    {
        CurrentState = new State(); // Clear the state
        await DefaultStateIfNeededAsync();
        StateChanged?.Invoke(this, EventArgs.Empty);
        await SaveAsync();
    }

    public string Download() => System.Text.Json.JsonSerializer.Serialize(CurrentState);

    public async Task<bool> UploadAsync(string json)
    {
        try
        {
            CurrentState = System.Text.Json.JsonSerializer.Deserialize<State>(json) ?? new State();
            await DefaultStateIfNeededAsync();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            await ResetAsync();
            return false;
        }
    }

    public async Task SetStartSpvarAsync(int startSpvar)
    {
        CurrentState.StartSpvar = startSpvar;
        await RefreshVariablesAsync();
        await SaveAsync();
    }

    public async Task SetStartBitAsync(int startBit)
    {
        CurrentState.StartBit = startBit;
        await RefreshVariablesAsync();
        await SaveAsync();
    }

    public async Task SetSpvarCurrentValueAsync(int index, int value)
    {
        if (CurrentState.Spvars.ContainsKey(index) == false)
        {
            CurrentState.Spvars[index] = new Spvar(this, $"SPVAR_{index}");
        }
        CurrentState.Spvars[index].SetCurrentValue(value);
        await SaveAsync();
    }

    public async Task SetSpvarExpectedValueAsync(int index, int value)
    {
        if (CurrentState.Spvars.ContainsKey(index) == false)
        {
            CurrentState.Spvars[index] = new Spvar(this, $"SPVAR_{index}");
        }
        CurrentState.Spvars[index].SetCurrentValue(value);
        await SaveAsync();
    }

    public int GetExpectedSpvarValue(int spvar) => CurrentState.Spvars.ContainsKey(spvar) ? CurrentState.Spvars[spvar].ExpectedValue : 0;

    public Dictionary<int, Spvar> GetSpvars => CurrentState.Spvars;

    public List<Variable> GetVariables => CurrentState.Variables;

    #region Private Methods
    private async Task RefreshVariablesAsync()
    {
        var currentBit = CurrentState.StartBit;
        int currentSpvar = CurrentState.StartSpvar;

        foreach (Variable variable in CurrentState.Variables)
        {
            variable.SetSpvarInfo(currentSpvar, currentBit);
            await variable.SaveAsync(this);
            currentBit += variable.TotalBits;
            if (currentBit >= 32)
            {
                currentSpvar++;
                currentBit -= 32;
            }
        }
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private async Task DefaultStateIfNeededAsync()
    {
        if (CurrentState.Spvars.Any() == false)
        {
            for (int i = 0; i < 64; i++)
            {
                await SetSpvarExpectedValueAsync(i + 1, 0);
            }
        }
        if (CurrentState.StartSpvar <= 0)
        {
            CurrentState.StartSpvar = 1;
        }
    }
    #endregion
}
