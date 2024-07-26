using Holefeeder.Ui.Common.Authentication;
using Holefeeder.Ui.Common.Services;

using Microsoft.Extensions.DependencyInjection;

using Refit;

namespace Holefeeder.Ui.Common.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTokenService(this IServiceCollection services)
    {
        services.AddScoped<TokenService>();

        return services;
    }

    public static IServiceCollection AddRefitClients(this IServiceCollection services,
        Func<IServiceProvider, DelegatingHandler> configureHandler)
    {
        services.AddRefitClient<IHolefeederApiService>(new RefitSettings
        {
            UrlParameterFormatter = new CustomDateUrlParameterFormatter()
        })
            .AddHttpMessageHandler(configureHandler)
            .ConfigureHttpClient((provider, client) =>
            {
                var apiConfig = provider.GetRequiredService<DownStreamApiConfig>();
                client.BaseAddress = apiConfig.BaseUrl;
            });

        return services;
    }
}
