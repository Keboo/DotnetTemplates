using BlazorApp.Components;
using BlazorApp.Components.Account;
using BlazorApp.Core;
using BlazorApp.Data;
using BlazorApp.Services;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;

using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddHttpContextAccessor();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddAuthenticationStateSerialization();

builder.Services.AddMudServices();

// Token-based authentication services
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<TokenAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<TokenAuthenticationStateProvider>());
builder.Services.AddScoped<TokenRefreshService>();
builder.Services.AddTransient<BearerTokenHandler>();

// HTTP client for backend API calls with Bearer token
// Use service discovery to resolve the app's own address when running in Aspire
builder.Services.AddHttpClient("Backend", client =>
{
    // Use the service name "blazorapp" which is registered in AppHost
    // The https+http:// scheme allows the client to use either HTTPS or HTTP
    client.BaseAddress = new Uri("https+http://blazorapp");
})
    .AddHttpMessageHandler<BearerTokenHandler>();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();

builder.Services.AddAuthentication()
    .AddBearerToken(IdentityConstants.BearerScheme);


builder.AddDatabase();
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(BlazorApp.Client._Imports).Assembly);

// Map Identity API endpoints for token authentication
app.MapGroup("/").MapIdentityApi<ApplicationUser>();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();
