using System.Reflection;

using Auth0.OidcClient;

using CommunityToolkit.Maui;

using DotNet.Meteor.HotReload.Plugin;

using Holefeeder.Ui.Common.Authentication;
using Holefeeder.Ui.Common.Extensions;
using Holefeeder.Ui.Maui.Authentication;
using Holefeeder.Ui.Maui.Authentication.Persistence;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Logging;

using MudBlazor.Services;

namespace Holefeeder.Ui.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureEssentials()
#if DEBUG
            .EnableHotReload()
#endif
            .BuildConfiguration()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
        IdentityModelEventSource.ShowPII = true;
#endif

        builder.Services.AddMudServices();

        var auth0Config = builder.Configuration.GetSection("Auth0").Get<Auth0Config>() ??
                               throw new InvalidOperationException("Auth0 configuration is missing");
        var downStreamApiConfig = builder.Configuration.GetSection("DownstreamApi").Get<DownStreamApiConfig>() ??
                                  throw new InvalidOperationException("DownstreamApi configuration is missing");
        builder.Services.AddSingleton(auth0Config);
        builder.Services.AddSingleton(downStreamApiConfig);

        builder.Services.AddSingleton(new Auth0Client(new Auth0ClientOptions()
        {
            Domain = auth0Config.Domain,
            ClientId = auth0Config.ClientId,
            Scope = downStreamApiConfig.Scopes,
            RedirectUri = "myapp://callback",
            PostLogoutRedirectUri = "myapp://callback",
        }));
        builder.Services.AddAuthorizationCore();
        builder.Services.AddScoped<AuthenticationMessageHandler>();
        builder.Services.AddScoped<Auth0AuthenticationStateProvider>();
        builder.Services.AddScoped<AuthenticationStateProvider>(serviceProvider => serviceProvider.GetRequiredService<Auth0AuthenticationStateProvider>());
        builder.Services.AddScoped<IAuthNavigationManager>(serviceProvider => serviceProvider.GetRequiredService<Auth0AuthenticationStateProvider>());
        builder.Services.AddScoped<ITokenProvider, SecureStorageTokenProvider>();
        builder.Services.AddTokenService();
        builder.Services.AddRefitClients(provider => provider.GetRequiredService<AuthenticationMessageHandler>());

        return builder.Build();
    }

    private static MauiAppBuilder BuildConfiguration(this MauiAppBuilder builder)
    {
        // Load environment-specific configuration
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

        // Get the executing assembly to access the embedded resources
        var assembly = Assembly.GetExecutingAssembly();

        using var streamBaseFile =
            assembly.GetManifestResourceStream("Holefeeder.Ui.Maui.appsettings.json") ??
            throw new InvalidOperationException("Could not find appsettings.json");
        using var streamEnvFile =
            assembly.GetManifestResourceStream($"Holefeeder.Ui.Maui.appsettings.{environment}.json") ??
            throw new InvalidOperationException($"Could not find appsettings.{environment}.json");

        var configuration = new ConfigurationBuilder()
            .AddJsonStream(streamBaseFile)
            .AddJsonStream(streamEnvFile)
            .Build();

        builder.Configuration.AddConfiguration(configuration);

        return builder;
    }
}
