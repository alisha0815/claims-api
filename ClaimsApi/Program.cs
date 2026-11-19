using ClaimsApi.Auth;
using ClaimsApi.Data;
using ClaimsApi.Models;
using ClaimsApi.Services;
using EntityFramework.Exceptions.PostgreSQL;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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

var app = builder.Build();

await AdminBootstrapper.EnsureAdminAsync(app.Services);

app.UseExceptionHandler();

app.MapOpenApi();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "Claims API v1");
});

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health");

app.Run();
