using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using System.Reflection;
using ZENBitPackToolbox;

public static class Program
{
    public static string Version { get; }
    public static string RepoUrl { get; }

    static Program()
    {
        var assembly = Assembly.GetAssembly(typeof(Program))!;
        Type? gitver = assembly.GetTypes().FirstOrDefault(t => t.Name.EndsWith("GitVersionInformation", StringComparison.CurrentCultureIgnoreCase));

        if (gitver != null)
        {
            Version = "v" + gitver.GetField("SemVer")!.GetValue(null) + " (" + gitver.GetField("ShortSha")!.GetValue(null) + ")";
            RepoUrl = "https://github.com/Swizzy/ZENBitPackToolbox/commit/" + gitver.GetField("Sha")!.GetValue(null);
        }
        else
        {
            Version = "ERROR";
            RepoUrl = "https://github.com/Swizzy/ZENBitPackToolbox";
        }
    }

    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
        builder.Services.AddMudServices();
        builder.Services.AddBlazoredLocalStorage();

        await builder.Build().RunAsync();
    }
}
