using ClaimsApi.Auth;
using ClaimsApi.Data;
using ClaimsApi.Models;
using ClaimsApi.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services.AddProblemDetails();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ClaimsDb"))
);

builder.Services.AddScoped<IClaimService, ClaimService>();

builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>();

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
