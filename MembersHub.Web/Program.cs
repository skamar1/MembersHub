using MembersHub.Web.Components;
using MembersHub.Infrastructure;
using MembersHub.Application;
using MudBlazor.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components.Authorization;
using MembersHub.Web.Services;
using MembersHub.Core.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Configure for Fly.io deployment (reads PORT environment variable)
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Add Aspire service defaults
builder.AddServiceDefaults();

// Add Redis distributed caching
builder.AddRedisDistributedCache("cache");

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add MudBlazor services
builder.Services.AddMudServices();

// Add Infrastructure services (without DbContext - handled by Aspire above)
builder.Services.AddInfrastructure(builder.Configuration);

// Add Application services
builder.Services.AddApplication();

// Add Authentication and Authorization
builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

// Add Session Management
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
builder.Services.AddScoped<CustomAuthenticationStateProvider>(provider => 
    (CustomAuthenticationStateProvider)provider.GetRequiredService<AuthenticationStateProvider>());

// Add HTTP Context services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IHttpContextInfoService, HttpContextInfoService>();


var app = builder.Build();

// Apply database migrations and seed data on startup
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<MembersHub.Infrastructure.Data.MembersHubContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        // Apply migrations first
        await context.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied successfully");

        // Seed admin user if database is empty
        var seeder = new MembersHub.Infrastructure.Services.DatabaseSeeder(context,
            scope.ServiceProvider.GetRequiredService<ILogger<MembersHub.Infrastructure.Services.DatabaseSeeder>>());

        var generatedPassword = await seeder.SeedAdminUserIfNeededAsync();

        // Clear any account lockouts during startup (for development only)
        if (context.Database.GetConnectionString()?.Contains("localhost") == true)
        {
            var lockoutsCleared = await context.Database.ExecuteSqlRawAsync(@"
                DELETE FROM ""AccountLockouts"" WHERE ""UserId"" IN (
                    SELECT ""Id"" FROM ""Users""
                );
            ");

            if (lockoutsCleared > 0)
            {
                logger.LogInformation("Cleared {Count} account lockouts for development", lockoutsCleared);
            }
        }
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database");
    }
}

// Map Aspire default endpoints
app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

// Add status code pages for 404 and other errors
app.UseStatusCodePagesWithReExecute("/Error/{0}");

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Health check endpoint for Fly.io
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Run();
