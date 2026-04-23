using AudioGuideOwnerPortal.Data;
using AudioGuideOwnerPortal.Helpers;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);
var sharedAudioRoot = SharedStoragePathHelper.ResolveAudioRoot(builder.Configuration, builder.Environment.ContentRootPath);
var sharedImageRoot = SharedStoragePathHelper.ResolveImageRoot(builder.Configuration, builder.Environment.ContentRootPath);
var sharedDatabaseFile = SharedStoragePathHelper.ResolveDataFile(builder.Configuration, builder.Environment.ContentRootPath);
var sharedKeyRoot = SharedStoragePathHelper.ResolveKeyRoot(builder.Configuration, builder.Environment.ContentRootPath);
var databaseProvider = builder.Configuration["Database:Provider"]?.Trim() ?? "Sqlite";
var useHttpsRedirection = builder.Configuration.GetValue("Networking:UseHttpsRedirection", false);

Directory.CreateDirectory(sharedAudioRoot);
Directory.CreateDirectory(sharedImageRoot);
Directory.CreateDirectory(Path.GetDirectoryName(sharedDatabaseFile)!);
Directory.CreateDirectory(sharedKeyRoot);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
#if DEBUG
builder.Logging.AddDebug();
#endif

builder.Services.AddControllersWithViews();
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(sharedKeyRoot))
    .SetApplicationName("AudioGuideSystem");
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = "AudioGuideOwnerPortal.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromHours(8);
});
builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (string.Equals(databaseProvider, "Sqlite", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlite($"Data Source={sharedDatabaseFile}");
        return;
    }

    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure());
});
builder.Services.AddSingleton(new AudioStorageOptions(sharedAudioRoot));
builder.Services.AddSingleton(new ImageStorageOptions(sharedImageRoot));

var app = builder.Build();

await AppDataInitializer.InitializeAsync(app.Services);

app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(sharedAudioRoot),
    RequestPath = "/audio"
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(sharedImageRoot),
    RequestPath = "/images"
});

if (useHttpsRedirection)
{
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseSession();

app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";

    if (path.StartsWith("/ownerauth") || path == "/" || path.StartsWith("/css") || path.StartsWith("/js") || path.StartsWith("/lib"))
    {
        await next();
        return;
    }

    if (path.StartsWith("/ownerpoi") || path.StartsWith("/ownerdashboard"))
    {
        if (string.IsNullOrWhiteSpace(context.Session.GetString(OwnerSessionHelper.OwnerIdKey)))
        {
            var returnUrl = context.Request.Path + context.Request.QueryString;
            context.Response.Redirect($"/OwnerAuth/Login?returnUrl={Uri.EscapeDataString(returnUrl)}");
            return;
        }

        await next();
        return;
    }

    context.Response.Redirect("/OwnerAuth/Login");
});

app.MapControllerRoute(
    name: "owner-auth",
    pattern: "OwnerAuth/{action=Login}/{id?}",
    defaults: new { controller = "OwnerAuth" });

app.MapControllerRoute(
    name: "owner-poi",
    pattern: "OwnerPoi/{action=Index}/{id?}",
    defaults: new { controller = "OwnerPoi" });

app.MapControllerRoute(
    name: "owner-dashboard",
    pattern: "OwnerDashboard/{action=Index}/{id?}",
    defaults: new { controller = "OwnerDashboard" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=OwnerDashboard}/{action=Index}/{id?}");

app.Run();
