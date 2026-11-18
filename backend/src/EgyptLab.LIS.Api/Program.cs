using System.IdentityModel.Tokens.Jwt;
using System.Text;
using EgyptLab.LIS.Api.Identity;
using EgyptLab.LIS.Infrastructure.Domain;
using EgyptLab.LIS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var isDevelopment = builder.Environment.IsDevelopment();

builder.Services.AddDbContext<LisDbContext>(options =>
    options
        .UseSqlite(builder.Configuration.GetConnectionString("Lis") ?? "Data Source=egyptlab.db")
        .UseBranchScope());
builder.Services.AddScoped<IBranchScope, BranchScope>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<JwtTokenService>();

var jwtIssuer = ValueOr(
    builder.Configuration[$"{JwtOptions.SectionName}:Issuer"],
    "egyptlab-lis");
var jwtAudience = ValueOr(
    builder.Configuration[$"{JwtOptions.SectionName}:Audience"],
    "egyptlab-lis-clients");
var jwtSigningKey = builder.Configuration[$"{JwtOptions.SectionName}:SigningKey"];

if (string.IsNullOrWhiteSpace(jwtSigningKey) && isDevelopment)
{
    jwtSigningKey = JwtOptions.DevelopmentSigningKey;
}

if (string.IsNullOrWhiteSpace(jwtSigningKey) || Encoding.UTF8.GetByteCount(jwtSigningKey) < 32)
{
    throw new InvalidOperationException(
        "A JWT signing key of at least 32 bytes must be supplied in Jwt:SigningKey.");
}

var jwtExpiryMinutes = int.TryParse(
    builder.Configuration[$"{JwtOptions.SectionName}:ExpiryMinutes"],
    out var configuredExpiry) && configuredExpiry > 0
        ? configuredExpiry
        : 15;

builder.Services.Configure<JwtOptions>(options =>
{
    options.Issuer = jwtIssuer;
    options.Audience = jwtAudience;
    options.SigningKey = jwtSigningKey;
    options.ExpiryMinutes = jwtExpiryMinutes;
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Keep claim names exactly as issued; the LIS claims are explicit and
        // must not be rewritten into legacy SOAP claim types.
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey)),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var principal = context.Principal;
                if (principal is null
                    || !AuthEndpoints.TryReadIdentity(
                        principal,
                        out var userId,
                        out var branchId,
                        out var securityStamp))
                {
                    context.Fail("The token does not contain a valid LIS identity.");
                    return;
                }

                // Validation happens before branch scope is applied, so this
                // lookup must bypass the global filters and check scope itself.
                var scopeFactory = context.HttpContext.RequestServices
                    .GetRequiredService<IServiceScopeFactory>();
                using var serviceScope = scopeFactory.CreateScope();
                var db = serviceScope.ServiceProvider.GetRequiredService<LisDbContext>();

                var user = await db.Users.IgnoreQueryFilters()
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        item => item.Id == userId
                            && item.BranchId == branchId
                            && item.IsActive
                            && item.DeletedAt == null,
                        context.HttpContext.RequestAborted);

                if (user is null || !AuthEndpoints.StampMatches(user.SecurityStamp, securityStamp))
                {
                    context.Fail("The account is no longer active or its credentials changed.");
                }
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
        .RequireAuthenticatedUser()
        .Build();

    foreach (var permission in LisPermissions.All)
    {
        options.AddPolicy(
            permission,
            policy => policy.Requirements.Add(new PermissionRequirement(permission)));
    }
});
builder.Services.AddSingleton<IAuthorizationHandler, PermissionHandler>();

// OpenAPI is development-only; it is not part of the production surface.
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseMiddleware<AuditActorMiddleware>();
app.UseMiddleware<BranchScopeMiddleware>();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
   .WithName("Health")
   .WithTags("Operations")
   .AllowAnonymous();

app.MapAuthEndpoints();
app.MapAdminEndpoints();

app.Run();

static string ValueOr(string? value, string fallback) =>
    string.IsNullOrWhiteSpace(value) ? fallback : value;

// Exposed for integration tests using WebApplicationFactory.
public partial class Program;
