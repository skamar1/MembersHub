using MembersHub.Web.Components;
using MembersHub.Infrastructure;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults
builder.AddServiceDefaults();

// Add Aspire PostgreSQL
builder.AddNpgsqlDbContext<MembersHub.Infrastructure.Data.MembersHubContext>("membershubdb");
// Redis will be added later - commented out temporarily
// builder.AddRedisDistributedCache("redis");

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add MudBlazor services
builder.Services.AddMudServices();

// Add Infrastructure services
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

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


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
