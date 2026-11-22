using System.Text;
using ClaimsApi.Auth;
using ClaimsApi.Data;
using ClaimsApi.Models;
using ClaimsApi.Services;
using EntityFramework.Exceptions.PostgreSQL;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services.AddProblemDetails();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ClaimsDb")).UseExceptionProcessor()
);

builder.Services.AddScoped<IClaimService, ClaimService>();

builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>();

builder
    .Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

var jwt =
    builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException(
        $"Missing '{JwtOptions.SectionName}' configuration section"
    );

builder
    .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),

            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,

            ValidateAudience = true,
            ValidAudience = jwt.Audience,

            ValidateLifetime = true,
            // Tokens are issued and validated by this same app, so no clock drift to absorb.
            ClockSkew = TimeSpan.Zero,
        };
    });

builder
    .Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

builder.Services.AddScoped<ITokenService, TokenService>();

var app = builder.Build();

await AdminBootstrapper.EnsureAdminAsync(app.Services);

app.UseExceptionHandler();
app.UseHttpsRedirection();

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "Claims API v1");
});

app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi().AllowAnonymous();

app.MapControllers();

app.MapHealthChecks("/health").AllowAnonymous();

app.Run();
