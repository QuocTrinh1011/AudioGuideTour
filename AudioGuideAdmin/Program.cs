using AudioGuideAdmin.Helpers;
using AudioGuideAdmin.Data;
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
    options.Cookie.Name = "AudioGuideAdmin.Session";
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

    if (path.StartsWith("/auth") ||
        path.StartsWith("/qrcode/open/") ||
        path.StartsWith("/qrcode/renderpng/") ||
        path.StartsWith("/qrcode/renderpngbycode") ||
        path.StartsWith("/qrcode/rendersvg/") ||
        path.StartsWith("/qrcode/rendersvgbycode"))
    {
        await next();
        return;
    }

    if (string.IsNullOrWhiteSpace(context.Session.GetString("user")))
    {
        context.Response.Redirect("/Auth/Login");
        return;
    }

    await next();
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();
