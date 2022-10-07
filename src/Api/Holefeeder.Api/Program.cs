using System.Diagnostics.CodeAnalysis;

using Carter;

using Holefeeder.Api.ErrorHandling;
using Holefeeder.Api.Extensions;
using Holefeeder.Application.Extensions;
using Holefeeder.Application.SeedWork.BackgroundRequest;
using Holefeeder.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddCarter(configurator: configurator => configurator.WithEmptyValidators())
    .AddSwagger(builder.Environment)
    .AddHealthChecks(builder.Configuration)
    .AddSecurity(builder.Configuration)
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddHostedService<LongRunningService>();

builder.Host.AddSerilog();

var app = builder.Build();

app.UseCustomErrors(app.Environment);

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseSerilog()
    .MapSwagger(builder.Environment)
    .MapHealthChecks()
    .MapCarter();

app.UseAuthentication()
    .UseAuthorization()
    .UseHttpsRedirection();

app.MigrateDb();

await app.RunAsync();

#pragma warning disable CA1050
namespace Holefeeder.Api
{
    [ExcludeFromCodeCoverage]
    public class Program
    {
    }
}
#pragma warning restore CA1050
