using AssetManagement.Web.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using AssetManagement.Core.Entities;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;

using CurrieTechnologies.Razor.SweetAlert2;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddHubOptions(options =>
    {
        options.MaximumReceiveMessageSize = 32 * 1024 * 1024; // 32 MB
    });

builder.Services.Configure<Microsoft.AspNetCore.SignalR.HubOptions>(options =>
{
    options.MaximumReceiveMessageSize = 32 * 1024 * 1024; // 32 MB
});
    
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<AssetManagement.Infrastructure.Data.ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString), ServiceLifetime.Transient);

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<AssetManagement.Infrastructure.Data.ApplicationDbContext>()
.AddDefaultTokenProviders();

// Removed SecurityStampValidatorOptions to fix Remember Me in Blazor Server

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/login";
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
    options.Cookie.MaxAge = TimeSpan.FromDays(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
    options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.None;
});

builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.FromDays(30);
});

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider, Microsoft.AspNetCore.Components.Server.ServerAuthenticationStateProvider>();

builder.Services.AddTransient<AssetManagement.Infrastructure.Services.DeviceService>();
builder.Services.AddTransient<AssetManagement.Infrastructure.Services.EmployeeService>();
builder.Services.AddTransient<AssetManagement.Infrastructure.Services.HandoverService>();
builder.Services.AddTransient<AssetManagement.Infrastructure.Services.RepairService>();
builder.Services.AddTransient<AssetManagement.Infrastructure.Services.RequisitionService>();
builder.Services.AddTransient<AssetManagement.Infrastructure.Services.PurchaseService>();
builder.Services.AddTransient<AssetManagement.Core.Interfaces.IInvoiceAutomationService, AssetManagement.Infrastructure.Services.LocalAiInvoiceService>();
builder.Services.AddTransient<AssetManagement.Core.Interfaces.IAutomationService, AssetManagement.Infrastructure.Services.AutomationService>();
builder.Services.AddScoped<AssetManagement.Core.Interfaces.ISettingsService, AssetManagement.Infrastructure.Services.SettingsService>();
builder.Services.AddScoped<AssetManagement.Core.Interfaces.IDashboardService, AssetManagement.Infrastructure.Services.DashboardService>();
builder.Services.AddTransient<AssetManagement.Core.Interfaces.IQrBarcodeDetectionService, AssetManagement.Infrastructure.Services.QrBarcodeDetectionService>();
builder.Services.AddTransient<AssetManagement.Core.Interfaces.IOcrService, AssetManagement.Infrastructure.Services.TesseractOcrService>();
builder.Services.AddHttpClient<AssetManagement.Core.Interfaces.IAiDocumentExtractionService, AssetManagement.Infrastructure.Services.OllamaAiDocumentExtractionService>();
builder.Services.AddTransient<AssetManagement.Core.Interfaces.IPdfProcessingService, AssetManagement.Infrastructure.Services.PdfProcessingService>();
builder.Services.AddTransient<AssetManagement.Core.Interfaces.IAiFieldMappingService, AssetManagement.Infrastructure.Services.AiFieldMappingService>();
builder.Services.AddTransient<AssetManagement.Core.Interfaces.IAiValidationService, AssetManagement.Infrastructure.Services.AiValidationService>();
builder.Services.AddTransient<AssetManagement.Core.Interfaces.IDocumentBundleService, AssetManagement.Infrastructure.Services.DocumentBundleService>();
builder.Services.AddSweetAlert2();
builder.Services.AddScoped<AssetManagement.Web.Services.NotificationService>();

builder.Services.AddAuthorizationCore(options =>
{
    foreach (var perm in AssetManagement.Core.Security.Permissions.GetAllPermissions())
    {
        options.AddPolicy(perm, policy => policy.RequireClaim("Permission", perm));
    }
});

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "Keys")))
    .SetApplicationName("AssetManagementSystem");

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AssetManagement.Infrastructure.Data.ApplicationDbContext>();
    context.Database.Migrate();

    if (!context.Devices.Any())
    {
        context.Devices.AddRange(AssetManagement.Core.Data.MockData.Devices);
        context.SaveChanges();
    }

    if (!context.Brands.Any())
    {
        var existingBrands = context.Devices.Select(d => d.Brand).Where(b => !string.IsNullOrEmpty(b)).Distinct().ToList();
        var additionalBrands = new[] { "Lenovo", "Dell", "HP", "Asus", "Acer", "Apple", "Samsung", "LG", "A4Tech", "Logitech", "Rapoo" };
        var allBrands = existingBrands.Concat(additionalBrands).Distinct();
        context.Brands.AddRange(allBrands.Select(b => new AssetManagement.Core.Entities.Brand { Name = b }));
        context.SaveChanges();
    }

    if (!context.DeviceCategories.Any())
    {
        var existingCategories = context.Devices.Select(d => d.Category).Where(c => !string.IsNullOrEmpty(c)).Distinct().ToList();
        var additionalCategories = new[] { "Laptop", "Desktop", "Monitor", "Mobile", "Peripheral", "Cable" };
        var allCategories = existingCategories.Concat(additionalCategories).Distinct();
        context.DeviceCategories.AddRange(allCategories.Select(c => new AssetManagement.Core.Entities.DeviceCategoryModel { Name = c }));
        context.SaveChanges();
    }

    var dbCategories = context.DeviceCategories.ToList();
    AssetManagement.Core.Entities.CategoryTypeRegistry.Sync(dbCategories);

    if (!context.Designations.Any())
    {
        var existingDesignations = context.Employees.Select(e => e.Designation).Where(d => !string.IsNullOrEmpty(d)).Distinct().ToList();
        var deviceDesignations = context.Devices.Select(d => d.Designation).Where(d => !string.IsNullOrEmpty(d)).Distinct().ToList();
        var allDesignations = existingDesignations.Concat(deviceDesignations).Distinct();
        context.Designations.AddRange(allDesignations.Select(d => new AssetManagement.Core.Entities.Designation { Name = d }));
        context.SaveChanges();
    }

    // Seed Office Locations (TK Bhaban & BSEC Bhaban)
    var requiredLocations = new[] { "TK Bhaban", "BSEC Bhaban" };
    foreach (var locName in requiredLocations)
    {
        if (!context.Locations.Any(l => l.Name.ToLower() == locName.ToLower()))
        {
            context.Locations.Add(new AssetManagement.Core.Entities.Location { Name = locName });
        }
    }
    context.SaveChanges();

    // Seed Project Managers (Imtiaz Morshed - TK Bhaban, Ifteraz Hafez - BSEC Bhaban)
    var pm1 = context.ProjectManagers.FirstOrDefault(p => p.Name.ToLower() == "imtiaz morshed");
    if (pm1 == null)
    {
        pm1 = new AssetManagement.Core.Entities.ProjectManager { Name = "Imtiaz Morshed" };
        context.ProjectManagers.Add(pm1);
    }
    var pm2 = context.ProjectManagers.FirstOrDefault(p => p.Name.ToLower() == "ifteraz hafez");
    if (pm2 == null)
    {
        pm2 = new AssetManagement.Core.Entities.ProjectManager { Name = "Ifteraz Hafez" };
        context.ProjectManagers.Add(pm2);
    }
    context.SaveChanges();

    var deviceService = scope.ServiceProvider.GetRequiredService<AssetManagement.Infrastructure.Services.DeviceService>();
    await deviceService.AssignMissingAssetTagsAsync();

    // Seed roles and default admin user
    await AssetManagement.Infrastructure.Data.IdentitySeeder.SeedRolesAndAdminAsync(scope.ServiceProvider);
    
    // Ensure Admin has all permissions
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var adminRole = await roleManager.FindByNameAsync("Admin");
    if (adminRole != null)
    {
        var existingClaims = await roleManager.GetClaimsAsync(adminRole);
        var existingPerms = existingClaims.Where(c => c.Type == "Permission").Select(c => c.Value).ToHashSet();
        foreach (var perm in AssetManagement.Core.Security.Permissions.GetAllPermissions())
        {
            if (!existingPerms.Contains(perm))
            {
                await roleManager.AddClaimAsync(adminRole, new System.Security.Claims.Claim("Permission", perm));
            }
        }
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
provider.Mappings[".webm"] = "video/webm";
app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = provider
});

app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value?.ToLower() ?? "";
    var userAgent = context.Request.Headers["User-Agent"].ToString().ToLower();

    bool isMobile = userAgent.Contains("mobi") || userAgent.Contains("android") || userAgent.Contains("iphone");

    if (isMobile)
    {
        // Always allow static assets, uploaded files, and Blazor framework files
        if (path.StartsWith("/uploads/") ||
            path.StartsWith("/assets/") ||
            path.StartsWith("/css/") ||
            path.StartsWith("/js/") ||
            path.StartsWith("/images/") ||
            path.StartsWith("/img/") ||
            path.StartsWith("/icons/") ||
            path.StartsWith("/fonts/") ||
            path.StartsWith("/favicon") ||
            path.StartsWith("/_framework/") ||
            path.StartsWith("/_blazor/") ||
            path == "/_blazor" ||
            path.EndsWith(".css") ||
            path.EndsWith(".js") ||
            path.EndsWith(".png") ||
            path.EndsWith(".jpg") ||
            path.EndsWith(".jpeg") ||
            path.EndsWith(".webp") ||
            path.EndsWith(".svg") ||
            path.EndsWith(".gif") ||
            path.EndsWith(".ico") ||
            path.EndsWith(".webm") ||
            path.EndsWith(".mp4") ||
            path.EndsWith(".woff") ||
            path.EndsWith(".woff2") ||
            path.EndsWith(".ttf") ||
            path.EndsWith(".pdf") ||
            path.EndsWith(".json") ||
            path.EndsWith(".map"))
        {
            await next(context);
            return;
        }

        // Allow: root, login, auth endpoints, scan, QR deep links (/a/, /e/)
        if (path == "/" || path == "" ||
            path == "/login" || path == "/account/login" || path == "/account/loginpost" || path == "/account/logoutpost" || path == "/account/accessdenied" ||
            path == "/scan" || path.StartsWith("/scan/") ||
            path.StartsWith("/a/") ||    // Asset QR deep link
            path.StartsWith("/e/") ||    // Employee QR deep link
            path.StartsWith("/employee-scan/") ||
            path.StartsWith("/device-register") || path.StartsWith("/device-registration"))
        {
            await next(context);
            return;
        }

        // Block all other management routes on mobile (tables, reports, settings, etc.)
        context.Response.StatusCode = 403;
        context.Response.ContentType = "text/html";
        await context.Response.WriteAsync(@"
            <html>
                <head><meta name='viewport' content='width=device-width, initial-scale=1.0'><title>Desktop Only</title></head>
                <body style='font-family:sans-serif;display:flex;justify-content:center;align-items:center;height:100vh;margin:0;background:#0f172a;color:#f8fafc;text-align:center;'>
                    <div>
                        <svg style='width:56px;height:56px;margin:0 auto 16px auto;stroke:#60a5fa;fill:none;' viewBox='0 0 24 24' stroke-width='1.5'><rect x='2' y='3' width='20' height='14' rx='2'/><path d='M8 21h8M12 17v4'/></svg>
                        <h1 style='font-size:20px;margin:0;color:#f8fafc;'>Desktop Required</h1>
                        <p style='color:#94a3b8;font-size:13px;margin-top:8px;padding:0 24px;line-height:1.5;'>Management pages require a desktop browser.<br>Use your phone camera to scan an Asset QR label.</p>
                    </div>
                </body>
            </html>");
        return;
    }

    await next(context);
});


app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapGet("/Account/Login", (HttpContext context) => 
{
    var returnUrl = context.Request.Query["ReturnUrl"];
    return Results.Redirect($"{context.Request.PathBase}/login?returnUrl={returnUrl}");
});

app.MapGet("/Account/AccessDenied", (HttpContext context) => 
{
    return Results.Redirect($"{context.Request.PathBase}/login");
});

app.MapPost("/Account/LoginPost", async (HttpContext context, Microsoft.AspNetCore.Identity.SignInManager<ApplicationUser> signInManager) =>
{
    var form = await context.Request.ReadFormAsync();
    var email = form["email"].ToString();
    var password = form["password"].ToString();
    var rememberMeStr = form["rememberMe"].ToString().ToLower();
    var rememberMe = rememberMeStr == "on" || rememberMeStr == "true";
    var returnUrl = form["returnUrl"].ToString();

    var result = await signInManager.PasswordSignInAsync(email, password, rememberMe, lockoutOnFailure: false);
    
    if (result.Succeeded)
    {
        var basePath = context.Request.PathBase.Value ?? "";
        var redirectUrl = string.IsNullOrEmpty(returnUrl) 
            ? (string.IsNullOrEmpty(basePath) ? "/" : basePath + "/")
            : (returnUrl.StartsWith("/") 
                ? (!string.IsNullOrEmpty(basePath) && returnUrl.StartsWith(basePath) ? returnUrl : basePath + returnUrl) 
                : (string.IsNullOrEmpty(basePath) ? "/" + returnUrl : basePath + "/" + returnUrl));
        return Results.Redirect(redirectUrl);
    }
    
    return Results.Redirect($"{context.Request.PathBase}/login?error=Invalid login attempt&returnUrl={returnUrl}");
});

app.MapPost("/Account/LogoutPost", async (HttpContext context, Microsoft.AspNetCore.Identity.SignInManager<ApplicationUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Redirect($"{context.Request.PathBase}/login");
});

app.Run();



