using HealthChecks.UI.Client;

using Holefeeder.Web.Config;
using Holefeeder.Web.Controllers;
using Holefeeder.Web.Extensions;

using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, _, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration));

builder.Services.AddControllersWithViews();
builder.Services.AddYarpReverseProxy(builder.Configuration);
builder.Services.Configure<AngularSettings>(builder.Configuration.GetSection(nameof(AngularSettings)))
    .AddSingleton(sp => sp.GetRequiredService<IOptions<AngularSettings>>().Value);

// builder.Services
//   .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//   .AddMicrosoftIdentityWebApi(
//     options => options.TokenValidationParameters =
//       new TokenValidationParameters {ValidateIssuer = false},
//     options => builder.Configuration.Bind("AzureAdB2C", options));

builder.Services
    .AddHealthChecksUI(setup =>
    {
        setup.AddHealthCheckEndpoint("web", "/healthz");
        setup.AddHealthCheckEndpoint("budgeting", "/gateway/budgeting/healthz");
        setup.AddHealthCheckEndpoint("object-store", "/gateway/object-store/healthz");
    })
    .AddInMemoryStorage();

builder.Services.AddHealthChecks();

// builder.Services.AddAuthorization(options =>
// {
//   options.AddPolicy("customPolicy", policy =>
//     policy.RequireAuthenticatedUser());
// });

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseSerilogRequestLogging();
// app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// app.UseAuthentication();
// app.UseAuthorization();

app.MapControllerRoute(
    "default",
    "{controller}/{action=Index}/{id?}");

app.MapFallbackToFile("index.html");

app.AddConfigRoutes();

app.MapHealthChecks("/healthz",
    new HealthCheckOptions {Predicate = _ => true, ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse}
);
app.UseHttpLogging();
app.UseEndpoints(config =>
{
    config.MapReverseProxy();
    config.MapHealthChecksUI(config =>
    {
        config.UIPath = "/hc-ui";
        config.ResourcesPath = "/hc-ui/resources";
        config.ApiPath = "/hc-ui/api";
        config.WebhookPath = "/hc-ui/webhooks";
        config.UseRelativeApiPath = true;
        config.UseRelativeResourcesPath = true;
        config.UseRelativeWebhookPath = true;
    });
});

app.Run();