using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor;
using MudBlazor.Services;
using ZENBitPackToolbox.Managers;

namespace ZENBitPackToolbox;

public static class Program
{
    public static string Version { get; }
    public static string RepoUrl { get; }

    static Program()
    {
        Version = $"v{GitVersionInformation.SemVer} ({GitVersionInformation.ShortSha})";
        RepoUrl = "https://github.com/Swizzy/ZENBitPackToolbox/commit/" + GitVersionInformation.Sha;
    }

    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
        builder.Services.AddMudServices(options =>
                                        {
                                            options.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomStart;
                                        });
        builder.Services.AddBlazoredLocalStorage();
        builder.Services.AddScoped<StateManager>();

        await builder.Build().RunAsync();
    }
}