using C2C.DevicePlatform.Application.Repositories;
using C2C.DevicePlatform.Api.Security;
using C2C.DevicePlatform.Api.Services;
using C2C.DevicePlatform.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddScoped<DeviceCatalogService>();
builder.Services.AddScoped<EnvironmentConfigTemplateService>();
builder.Services.AddScoped<DeviceExportService>();
builder.Services.AddScoped<SensitiveDataMaskingService>();
builder.Services.AddScoped<AuditTrailService>();
builder.Services.AddScoped<CriticalChangeGuardService>();
builder.Services.AddScoped<UserEnvironmentScopeService>();
builder.Services.AddSingleton<IAuthorizationHandler, EnvironmentAccessHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, MfaRequirementHandler>();

var supabaseConnectionString = builder.Configuration.GetConnectionString("Supabase");
if (string.IsNullOrWhiteSpace(supabaseConnectionString))
{
    throw new InvalidOperationException("ConnectionStrings:Supabase must be configured.");
}

builder.Services.AddScoped<IDeviceCatalogRepository>(_ => new SupabaseDeviceCatalogRepository(supabaseConnectionString));
builder.Services.AddScoped<IAssignmentTargetRepository>(_ => new SupabaseAssignmentTargetRepository(supabaseConnectionString));
builder.Services.AddScoped<IEnvironmentConfigRepository>(_ => new SupabaseEnvironmentConfigRepository(supabaseConnectionString));
builder.Services.AddScoped<IAuditRepository>(_ => new SupabaseAuditRepository(supabaseConnectionString));
builder.Services.AddScoped<IChangeApprovalRepository>(_ => new SupabaseChangeApprovalRepository(supabaseConnectionString));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtSection = builder.Configuration.GetSection("Authentication:Jwt");
        options.Authority = jwtSection["Authority"];
        options.Audience = jwtSection["Audience"];
        options.RequireHttpsMetadata = true;
        options.TokenValidationParameters.RoleClaimType = "role";
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(PolicyNames.PlatformAccess, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.Requirements.Add(new EnvironmentAccessRequirement("demo", "qa", "prod"));
    });

    options.AddPolicy(PolicyNames.DeviceManage, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("PlatformAdmin", "OpsAdmin");
        policy.Requirements.Add(new EnvironmentAccessRequirement("demo", "qa", "prod"));
    });

    options.AddPolicy(PolicyNames.SupportReadOnly, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("PlatformAdmin", "OpsAdmin", "Support", "MerchantViewer");
        policy.Requirements.Add(new EnvironmentAccessRequirement("demo", "qa", "prod"));
    });

    options.AddPolicy(PolicyNames.AdminMfa, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("PlatformAdmin", "OpsAdmin");
        policy.Requirements.Add(new MfaRequirement());
        policy.Requirements.Add(new EnvironmentAccessRequirement("demo", "qa", "prod"));
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
