using AudioGuideAPI.Helpers;
using AudioGuideAPI.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);
var sharedAudioRoot = SharedStoragePathHelper.ResolveAudioRoot(builder.Configuration, builder.Environment.ContentRootPath);
var sharedImageRoot = SharedStoragePathHelper.ResolveImageRoot(builder.Configuration, builder.Environment.ContentRootPath);
var sharedDatabaseFile = SharedStoragePathHelper.ResolveDataFile(builder.Configuration, builder.Environment.ContentRootPath);
var databaseProvider = builder.Configuration["Database:Provider"]?.Trim() ?? "Sqlite";
var useHttpsRedirection = builder.Configuration.GetValue("Networking:UseHttpsRedirection", false);
Directory.CreateDirectory(sharedAudioRoot);
Directory.CreateDirectory(sharedImageRoot);
Directory.CreateDirectory(Path.GetDirectoryName(sharedDatabaseFile)!);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
#if DEBUG
builder.Logging.AddDebug();
#endif

builder.Services.AddControllers();
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
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("mobile", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (useHttpsRedirection)
{
    app.UseHttpsRedirection();
}
app.UseCors("mobile");
app.UseAuthorization();
app.MapControllers();

app.Run();
