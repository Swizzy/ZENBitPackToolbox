using Microsoft.AspNetCore.Components;
using Blazored.LocalStorage;
using MudBlazor;
using Microsoft.JSInterop;
using ZENBitPackToolbox.Managers;

namespace ZENBitPackToolbox
{
    public partial class MainLayout
    {
        private const string THEME_KEY = "theme";
        private const string DARK_THEME = "dark";
        private const string LIGHT_THEME = "light";

        [Inject]
        public ILocalStorageService LocalStorage { get; set; }

        [Inject]
        IJSRuntime JsRuntime { get; set; }

        [Inject]
        private StateManager StateManager { get; set; }

        private MudThemeProvider? _mudThemeProvider;
        private string ModeIcon => _isDarkMode ? Icons.Outlined.LightMode : Icons.Outlined.DarkMode;
        private string ModeTitle => _isDarkMode ? "Switch to light theme" : "Switch to dark theme";
        public string AppVersion => Program.Version;
        public string RepoUrl => Program.RepoUrl;

        private bool _isDarkMode;

        protected override async Task OnParametersSetAsync()
        {
            if (await LocalStorage.ContainKeyAsync(THEME_KEY))
            {
                _isDarkMode = await LocalStorage.GetItemAsStringAsync(THEME_KEY) == DARK_THEME;
            }
            else
            {
                _isDarkMode = await _mudThemeProvider!.GetSystemPreference();
                SaveTheme();
            }
            await StateManager.LoadAsync();
            await base.OnParametersSetAsync();
        }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
        }

        private void ToggleDarkMode()
        {
            _isDarkMode = !_isDarkMode;
            SaveTheme();
        }

        private void SaveTheme()
        {
            var theme = _isDarkMode ? DARK_THEME : LIGHT_THEME;
            LocalStorage.SetItemAsStringAsync(THEME_KEY, theme);
            JsRuntime.InvokeVoidAsync("SetTheme", theme);
        }
    }
}