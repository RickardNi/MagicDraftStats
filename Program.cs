using MagicDraftStats;
using MagicDraftStats.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSingleton(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddSingleton<IBGStatsImportService, BGStatsImportService>();
builder.Services.AddSingleton<IGlobalFilterService, GlobalFilterService>();
builder.Services.AddSingleton<IDeckImportService, DeckImportService>();
builder.Services.AddSingleton<IScryfallCardService, ScryfallCardService>();

await builder.Build().RunAsync();
