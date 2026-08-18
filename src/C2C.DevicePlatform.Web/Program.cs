using C2C.DevicePlatform.Web.Api;
using C2C.DevicePlatform.Web.Components;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();
builder.Services.Configure<DevicePlatformApiOptions>(builder.Configuration.GetSection("Api"));
builder.Services.AddHttpClient("DevicePlatformApi", (serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<DevicePlatformApiOptions>>().Value;
    if (string.IsNullOrWhiteSpace(options.BaseUrl))
    {
        throw new InvalidOperationException("Api:BaseUrl must be configured.");
    }

    client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
});
builder.Services.AddScoped<DeviceAdminApiClient>();

var allowMissingEnvironmentClaim = builder.Configuration.GetValue<bool>("Authorization:AllowMissingEnvironmentClaim", false);
var defaultEnvironment = builder.Configuration.GetValue<string>("Authorization:DefaultEnvironment") ?? "demo";

bool HasAllowedEnvironment(System.Security.Claims.ClaimsPrincipal user)
{
    var envClaim = user.FindFirst("env")?.Value
        ?? user.FindFirst("environment")?.Value;

    if (string.IsNullOrWhiteSpace(envClaim) && allowMissingEnvironmentClaim)
    {
        envClaim = defaultEnvironment;
    }

    return envClaim is not null
        && (envClaim.Equals("demo", StringComparison.OrdinalIgnoreCase)
            || envClaim.Equals("qa", StringComparison.OrdinalIgnoreCase)
            || envClaim.Equals("prod", StringComparison.OrdinalIgnoreCase));
}

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.AccessDeniedPath = "/not-found";
    })
    .AddOpenIdConnect(options =>
    {
        var oidc = builder.Configuration.GetSection("Authentication:Oidc");
        options.Authority = oidc["Authority"];
        options.ClientId = oidc["ClientId"] ?? "c2c-device-platform-web";
        options.ClientSecret = oidc["ClientSecret"];
        options.ResponseType = "code";
        options.SaveTokens = true;
        options.GetClaimsFromUserInfoEndpoint = true;
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.TokenValidationParameters.RoleClaimType = "role";
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("PlatformViewer", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context => HasAllowedEnvironment(context.User));
    });

    options.AddPolicy("PlatformOperator", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("PlatformAdmin", "OpsAdmin");
        policy.RequireAssertion(context => HasAllowedEnvironment(context.User));
    });

    options.AddPolicy("AdminMfa", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("PlatformAdmin", "OpsAdmin");
        policy.RequireAssertion(context =>
        {
            var amrClaims = context.User.FindAll("amr").Select(claim => claim.Value);
            var hasMfaByAmr = amrClaims.Any(value => value.Equals("mfa", StringComparison.OrdinalIgnoreCase));

            var acrClaim = context.User.FindFirst("acr")?.Value;
            var hasMfaByAcr = !string.IsNullOrWhiteSpace(acrClaim)
                && acrClaim.Contains("mfa", StringComparison.OrdinalIgnoreCase);

            return hasMfaByAmr || hasMfaByAcr;
        });
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .RequireAuthorization("PlatformViewer");

app.Run();
