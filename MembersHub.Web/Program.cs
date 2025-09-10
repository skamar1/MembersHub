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

// Add regular SQL Server DbContext
builder.Services.AddDbContext<MembersHub.Infrastructure.Data.MembersHubContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("membershubdb")));

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add MudBlazor services
builder.Services.AddMudServices();

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

// Add Email services
builder.Services.AddScoped<MembersHub.Core.Interfaces.IEmailEncryptionService, MembersHub.Infrastructure.Services.EmailEncryptionService>();
builder.Services.AddScoped<MembersHub.Core.Interfaces.IEmailConfigurationService, MembersHub.Infrastructure.Services.EmailConfigurationService>();

var app = builder.Build();

// Apply database migrations on startup
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<MembersHub.Infrastructure.Data.MembersHubContext>();
        await context.Database.MigrateAsync();
        
        // Log successful migration
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Database migrations applied successfully");
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

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
