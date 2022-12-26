﻿using Blazored.LocalStorage;
using Newtonsoft.Json;
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

    public async Task LoadAsync() => await UploadAsync(await LocalStorage.GetItemAsStringAsync(KEY));

    private async Task SaveAsync() => await LocalStorage.SetItemAsStringAsync(KEY, Download());

    public async Task ResetAsync()
    {
        CurrentState = new State(); // Clear the state
        await DefaultStateIfNeededAsync();
        StateChanged?.Invoke(this, EventArgs.Empty);
        await SaveAsync();
    }

    public string Download() => JsonConvert.SerializeObject(CurrentState);

    public async Task<bool> UploadAsync(string json)
    {
        try
        {
            CurrentState = JsonConvert.DeserializeObject<State>(json) ?? new State();
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
            CurrentState.Spvars[index] = new Spvar($"SPVAR_{index}");
        }
        CurrentState.Spvars[index].SetCurrentValue(value);
        await SaveAsync();
    }

    public async Task SetSpvarExpectedValueAsync(int index, int value)
    {
        if (CurrentState.Spvars.ContainsKey(index) == false)
        {
            CurrentState.Spvars[index] = new Spvar($"SPVAR_{index}");
        }
        CurrentState.Spvars[index].SetExpectedValue(value);
        await SaveAsync();
    }

    public int GetExpectedSpvarValue(int spvar) => CurrentState.Spvars.ContainsKey(spvar) ? CurrentState.Spvars[spvar].ExpectedValue : 0;

    public Dictionary<int, Spvar> GetSpvars => CurrentState.Spvars;

    public List<Variable> GetVariables => CurrentState.Variables;

    public async Task RemoveVariableAsync(int index)
    {
        var variable = CurrentState.Variables.FirstOrDefault(v => v.Index == index);
        if (variable != null)
        {
            CurrentState.Variables.Remove(variable);
            await RefreshVariablesAsync();
        }
    }

    public async Task AddNewVariableAsync(Variable variable)
    {
        CurrentState.Variables.Add(variable);
        await RefreshVariablesAsync();
    }

    public async Task MoveVariableAsync(int index, bool up)
    {
        var variable = CurrentState.Variables.FirstOrDefault(v => v.Index == index);
        if (variable != null)
        {
            if (up)
            {
                var above = CurrentState.Variables.FirstOrDefault(v => v.Index == index - 1);
                if (above != null)
                {
                    above.UpdateIndex(index);
                    variable.UpdateIndex(index - 1);
                    await RefreshVariablesAsync();
                }
            }
            else
            {
                var below = CurrentState.Variables.FirstOrDefault(v => v.Index == index + 1);
                if (below != null)
                {
                    below.UpdateIndex(index);
                    variable.UpdateIndex(index + 1);
                    await RefreshVariablesAsync();
                }
            }
        }
    }

    #region Private Methods
    private async Task RefreshVariablesAsync()
    {
        int index = 0;
        var currentBit = CurrentState.StartBit;
        int currentSpvar = CurrentState.StartSpvar;

        foreach (Variable variable in CurrentState.Variables.OrderBy(v => v.Index))
        {
            variable.UpdateIndex(index++);
            variable.SetSpvarInfo(currentSpvar, currentBit);
            await variable.SaveAsync(this);
            currentBit += variable.TotalBits;
            if (currentBit >= 32)
            {
                currentSpvar++;
                currentBit -= 32;
            }
        }
        await SaveAsync();
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