using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;

using Holefeeder.Api.Swagger;
using Holefeeder.Application.Authorization;
using Holefeeder.Infrastructure.SeedWork;

using MicroElements.Swashbuckle.FluentValidation.AspNetCore;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace Holefeeder.Api.Extensions;

[ExcludeFromCodeCoverage]
internal static class ServiceCollectionExtensions
{
    internal const string AllowBlazorClientPolicy = nameof(AllowBlazorClientPolicy);
    private static readonly string[] ServiceTags = ["holefeeder", "api", "service"];
    private static readonly string[] DatabaseTags = ["holefeeder", "api", "mariadb"];
    private static readonly string[] HangfireTags = ["holefeeder", "api", "hangfire"];

    public static IServiceCollection AddSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var domain = GetDomain();
                var audience = GetAudience();
                options.Authority = $"https://{domain}/";
                options.Audience = audience;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = ClaimTypes.NameIdentifier
                };
            });

        services
            .AddAuthorization(options =>
            {
                var domain = $"https://{GetDomain()}/";
                options.AddPolicy(Policies.ReadUser,
                    policy => policy.Requirements.Add(new HasScopeRequirement(Policies.ReadUser, domain)));
                options.AddPolicy(Policies.WriteUser,
                    policy => policy.Requirements.Add(new HasScopeRequirement(Policies.WriteUser, domain)));
            });

        return services;

        string GetDomain() => configuration["Auth0:Domain"] ??
                              throw new InvalidOperationException("Auth0 domain is missing");

        string GetAudience() => configuration["Auth0:Audience"] ??
                                throw new InvalidOperationException("Auth0 audience is missing");
    }

    public static IServiceCollection AddSwagger(this IServiceCollection services, IHostEnvironment environment,
        IConfiguration configuration)
    {
        services
            .AddEndpointsApiExplorer()
            .AddSwaggerGen(options =>
            {
                options.EnableAnnotations();
                options.SwaggerDoc("v2",
                    new OpenApiInfo { Title = environment.ApplicationName, Version = "v2" });
                options.CustomSchemaIds(type => type.ToString());
                options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.OAuth2,
                    Flows = new OpenApiOAuthFlows
                    {
                        AuthorizationCode = new OpenApiOAuthFlow
                        {
                            TokenUrl = new Uri($"https://{configuration["Auth0:Domain"]}/oauth/token"),
                            AuthorizationUrl =
                                new Uri(
                                    $"https://{configuration["Auth0:Domain"]}/authorize?audience={configuration["Auth0:Audience"]}"),
                            Scopes = new Dictionary<string, string>
                            {
                                ["openid"] = "Sign In Permissions",
                                ["profile"] = "Read profile Permission",
                                [Policies.ReadUser] = "read:user",
                                [Policies.WriteUser] = "write:user",
                            }
                        }
                    }
                });
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "oauth2"
                            },
                            Scheme = "oauth2",
                            Name = "oauth2",
                            In = ParameterLocation.Header,
                        },
                        new List<string>()
                    }
                });
                options.OperationFilter<QueryRequestOperationFilter>();
                options.DocumentFilter<HideInDocsFilter>();
            })
            .AddFluentValidationRulesToSwagger();

        return services;
    }

    public static IServiceCollection AddHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddHealthChecks()
            .AddCheck("api", () => HealthCheckResult.Healthy(), ServiceTags)
            .AddMySql(configuration.GetConnectionString(BudgetingConnectionStringBuilder.BudgetingConnectionString)!,
                name: "holefeeder-db", tags: DatabaseTags)
            .AddHangfire(options => options.MinimumAvailableServers = 1, name: "hangfire", tags: HangfireTags);
        services
#pragma warning disable S1075
            .AddHealthChecksUI(setup => { setup.AddHealthCheckEndpoint("hc-api", "http://127.0.0.1/healthz"); })
#pragma warning restore S1075
            .AddInMemoryStorage();

        return services;
    }

    public static IServiceCollection AddCors(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        services.AddCors(options =>
        {
            options.AddPolicy(AllowBlazorClientPolicy, builder =>
            {
                builder
                    .WithOrigins(allowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });

        return services;
    }
}
