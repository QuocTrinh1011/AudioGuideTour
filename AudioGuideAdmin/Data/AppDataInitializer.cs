using AudioGuideAdmin.Models;
using Microsoft.EntityFrameworkCore;

namespace AudioGuideAdmin.Data;

public static class AppDataInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await EnsureVisitorTableAsync(context);
        await EnsureCategoryTableAsync(context);
        await EnsureLanguageTableAsync(context);
        await EnsureQrCodeTableAsync(context);
        await SeedLanguagesAsync(context);
    }

    public static async Task EnsureVisitorTableAsync(AppDbContext context)
    {
        await context.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'[Users]', N'U') IS NULL
            BEGIN
                CREATE TABLE [Users](
                    [Id] nvarchar(450) NOT NULL,
                    [DeviceId] nvarchar(450) NOT NULL,
                    [DisplayName] nvarchar(200) NOT NULL CONSTRAINT [DF_Users_DisplayName] DEFAULT N'Khach an danh',
                    [Language] nvarchar(20) NOT NULL CONSTRAINT [DF_Users_Language] DEFAULT N'vi-VN',
                    [AllowBackgroundTracking] bit NOT NULL CONSTRAINT [DF_Users_AllowBackgroundTracking] DEFAULT 1,
                    [AllowAutoPlay] bit NOT NULL CONSTRAINT [DF_Users_AllowAutoPlay] DEFAULT 1,
                    [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_Users_CreatedAt] DEFAULT SYSUTCDATETIME(),
                    [LastSeenAt] datetime2 NOT NULL CONSTRAINT [DF_Users_LastSeenAt] DEFAULT SYSUTCDATETIME(),
                    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
                );
            END
            """);
    }

    public static async Task EnsureCategoryTableAsync(AppDbContext context)
    {
        await context.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'[Categories]', N'U') IS NULL
            BEGIN
                CREATE TABLE [Categories](
                    [Id] int NOT NULL IDENTITY,
                    [Slug] nvarchar(100) NOT NULL,
                    [Name] nvarchar(150) NOT NULL,
                    [Description] nvarchar(max) NOT NULL CONSTRAINT [DF_Categories_Description] DEFAULT N'',
                    [ThemeColor] nvarchar(32) NOT NULL CONSTRAINT [DF_Categories_ThemeColor] DEFAULT N'#17324d',
                    [IsActive] bit NOT NULL CONSTRAINT [DF_Categories_IsActive] DEFAULT 1,
                    [SortOrder] int NOT NULL CONSTRAINT [DF_Categories_SortOrder] DEFAULT 1,
                    CONSTRAINT [PK_Categories] PRIMARY KEY ([Id])
                );
            END
            """);

        await context.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'[Categories]', N'U') IS NOT NULL
                AND NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'IX_Categories_Slug'
                      AND object_id = OBJECT_ID(N'[Categories]')
                )
            BEGIN
                CREATE UNIQUE INDEX [IX_Categories_Slug] ON [Categories]([Slug]);
            END
            """);
    }

    public static async Task EnsureLanguageTableAsync(AppDbContext context)
    {
        await context.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'[LanguageOptions]', N'U') IS NULL
            BEGIN
                CREATE TABLE [LanguageOptions](
                    [Id] int NOT NULL IDENTITY,
                    [Code] nvarchar(20) NOT NULL,
                    [Name] nvarchar(100) NOT NULL,
                    [NativeName] nvarchar(100) NOT NULL CONSTRAINT [DF_LanguageOptions_NativeName] DEFAULT N'',
                    [Locale] nvarchar(20) NOT NULL CONSTRAINT [DF_LanguageOptions_Locale] DEFAULT N'',
                    [IsActive] bit NOT NULL CONSTRAINT [DF_LanguageOptions_IsActive] DEFAULT 1,
                    [SortOrder] int NOT NULL CONSTRAINT [DF_LanguageOptions_SortOrder] DEFAULT 1,
                    CONSTRAINT [PK_LanguageOptions] PRIMARY KEY ([Id])
                );
            END
            """);

        await context.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'[LanguageOptions]', N'U') IS NOT NULL
                AND NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'IX_LanguageOptions_Code'
                      AND object_id = OBJECT_ID(N'[LanguageOptions]')
                )
            BEGIN
                CREATE UNIQUE INDEX [IX_LanguageOptions_Code] ON [LanguageOptions]([Code]);
            END
            """);
    }

    public static async Task EnsureQrCodeTableAsync(AppDbContext context)
    {
        await context.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'[QRCodes]', N'U') IS NULL
            BEGIN
                CREATE TABLE [QRCodes](
                    [Id] int NOT NULL IDENTITY,
                    [PoiId] int NOT NULL,
                    [Code] nvarchar(100) NOT NULL,
                    [Note] nvarchar(max) NOT NULL CONSTRAINT [DF_QRCodes_Note] DEFAULT N'',
                    CONSTRAINT [PK_QRCodes] PRIMARY KEY ([Id])
                );
            END
            """);

        await context.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'[QRCodes]', N'U') IS NOT NULL
                AND NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'IX_QRCodes_Code'
                      AND object_id = OBJECT_ID(N'[QRCodes]')
                )
            BEGIN
                CREATE UNIQUE INDEX [IX_QRCodes_Code] ON [QRCodes]([Code]);
            END
            """);
    }

    public static async Task SeedLanguagesAsync(AppDbContext context)
    {
        if (await context.LanguageOptions.AnyAsync())
        {
            return;
        }

        context.LanguageOptions.AddRange(
            new LanguageOption
            {
                Code = "vi-VN",
                Name = "Tieng Viet",
                NativeName = "Tiếng Việt",
                Locale = "vi-VN",
                SortOrder = 1
            },
            new LanguageOption
            {
                Code = "en-US",
                Name = "English",
                NativeName = "English",
                Locale = "en-US",
                SortOrder = 2
            },
            new LanguageOption
            {
                Code = "zh-CN",
                Name = "Chinese",
                NativeName = "中文",
                Locale = "zh-CN",
                SortOrder = 3
            },
            new LanguageOption
            {
                Code = "ja-JP",
                Name = "Japanese",
                NativeName = "日本語",
                Locale = "ja-JP",
                SortOrder = 4
            });

        await context.SaveChangesAsync();
    }
}
