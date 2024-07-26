using Holefeeder.Ui.Common.Authentication;
using Holefeeder.Ui.Common.Extensions;
using Holefeeder.Ui.Web;
using Holefeeder.Ui.Web.Authentication;

using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var auth0Config = builder.Configuration.GetSection("Auth0").Get<Auth0Config>() ??
                  throw new InvalidOperationException("Auth0 configuration is missing");
var downStreamApiConfig = builder.Configuration.GetSection("DownstreamApi").Get<DownStreamApiConfig>() ??
                          throw new InvalidOperationException("DownstreamApi configuration is missing");

builder.Services.AddSingleton(auth0Config);
builder.Services.AddSingleton(downStreamApiConfig);

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddOidcAuthentication(options =>
{
    builder.Configuration.Bind("Auth0", options.ProviderOptions);
    options.ProviderOptions.ResponseType = "code";
    options.ProviderOptions.AdditionalProviderParameters.Add("audience", auth0Config.Audience);
    foreach (var scope in downStreamApiConfig.ScopesArray)
    {
        options.ProviderOptions.DefaultScopes.Add(scope);
    }
});
builder.Services.AddMudServices();
builder.Services.AddScoped<AuthenticationMessageHandler>();
builder.Services.AddScoped<IAuthNavigationManager, AuthNavigationManager>();
builder.Services.AddRefitClients(provider => provider.GetRequiredService<AuthenticationMessageHandler>());

await builder.Build().RunAsync();
