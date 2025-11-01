using MembersHub.Web.Components;
using MembersHub.Infrastructure;
using MembersHub.Application;
using MudBlazor.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components.Authorization;
using MembersHub.Web.Services;
using MembersHub.Core.Interfaces;

var builder = WebApplication.CreateBuilder(args);

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

// Apply database migrations on startup
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<MembersHub.Infrastructure.Data.MembersHubContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        // Apply migrations first
        await context.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied successfully");

        // Check if this is a fresh database or has old incorrect hashes
        var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
        var correctHash = "$2a$11$Oc/hGN4tb2JwuShFyQIDDuCg6b8loRTxHA1Qi.jL3gyZTWCPZ2fcK"; // Aris100*
        var oldIncorrectHash = "$2a$11$yH8BxJ0Tff7K9B7N.zPgWOZ"; // Prefix of old incorrect hash

        if (adminUser != null)
        {
            // Fix if password is empty or has the old incorrect hash
            if (string.IsNullOrEmpty(adminUser.PasswordHash) ||
                adminUser.PasswordHash.StartsWith(oldIncorrectHash))
            {
                await context.Database.ExecuteSqlRawAsync(@"
                    UPDATE ""Users"" SET ""PasswordHash"" = {0} WHERE ""Username"" = 'admin';
                    UPDATE ""Users"" SET ""PasswordHash"" = {0} WHERE ""Username"" = 'owner';
                    UPDATE ""Users"" SET ""PasswordHash"" = {0} WHERE ""Username"" = 'treasurer';
                ", correctHash);

                logger.LogInformation("User passwords updated with correct BCrypt hashes");
            }

            // Clear any account lockouts during startup (for development)
            if (context.Database.GetConnectionString()?.Contains("localhost") == true)
            {
                var lockoutsCleared = await context.Database.ExecuteSqlRawAsync(@"
                    DELETE FROM ""AccountLockouts"" WHERE ""UserId"" IN (
                        SELECT ""Id"" FROM ""Users"" WHERE ""Username"" IN ('admin', 'owner', 'treasurer')
                    );
                ");

                if (lockoutsCleared > 0)
                {
                    logger.LogInformation("Cleared {Count} account lockouts for default users", lockoutsCleared);
                }
            }
        }
        else
        {
            logger.LogInformation("No users found in database - this may be a completely empty database");
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

app.Run();
