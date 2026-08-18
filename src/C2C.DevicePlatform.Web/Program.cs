using C2C.DevicePlatform.Web.Api;
using C2C.DevicePlatform.Web.Components;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;
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
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var allowMissingEnvironmentClaim = builder.Configuration.GetValue<bool>("Authorization:AllowMissingEnvironmentClaim", false);
var defaultEnvironment = builder.Configuration.GetValue<string>("Authorization:DefaultEnvironment") ?? "demo";
var apiScope = builder.Configuration.GetValue<string>("Authentication:Oidc:ApiScope");

bool HasAllowedEnvironment(System.Security.Claims.ClaimsPrincipal user)
{
    var allowedEnvironments = user.FindAll("env").Select(claim => claim.Value)
        .Concat(user.FindAll("environment").Select(claim => claim.Value))
        .SelectMany(value => value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Select(value => value.Trim().ToLowerInvariant())
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    if (allowedEnvironments.Count == 0 && allowMissingEnvironmentClaim)
    {
        allowedEnvironments.Add(defaultEnvironment.Trim().ToLowerInvariant());
    }

    return allowedEnvironments.Contains("demo")
        || allowedEnvironments.Contains("qa")
        || allowedEnvironments.Contains("prod");
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
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
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
        options.CallbackPath = "/signin-oidc";
        options.SignedOutCallbackPath = "/signout-callback-oidc";
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        if (!string.IsNullOrWhiteSpace(apiScope))
        {
            options.Scope.Add(apiScope);
        }
        options.CorrelationCookie.SameSite = SameSiteMode.None;
        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
        options.NonceCookie.SameSite = SameSiteMode.None;
        options.NonceCookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Events = new OpenIdConnectEvents
        {
            OnRemoteFailure = context =>
            {
                context.Response.Redirect("/not-found");
                context.HandleResponse();
                return Task.CompletedTask;
            }
        };
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
app.UseForwardedHeaders();
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapGet("/logout", async context =>
{
    var authProperties = new AuthenticationProperties
    {
        RedirectUri = "/"
    };

    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    await context.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme, authProperties);
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
