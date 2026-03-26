using AudioGuideAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace AudioGuideAPI.Data;

public static class AppDataInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await EnsureLanguageTableAsync(context);
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

        if (!await context.LanguageOptions.AnyAsync())
        {
            context.LanguageOptions.AddRange(
                new LanguageOption { Code = "vi-VN", Name = "Tieng Viet", NativeName = "Tiếng Việt", Locale = "vi-VN", SortOrder = 1 },
                new LanguageOption { Code = "en-US", Name = "English", NativeName = "English", Locale = "en-US", SortOrder = 2 },
                new LanguageOption { Code = "zh-CN", Name = "Chinese", NativeName = "中文", Locale = "zh-CN", SortOrder = 3 },
                new LanguageOption { Code = "ja-JP", Name = "Japanese", NativeName = "日本語", Locale = "ja-JP", SortOrder = 4 });

            await context.SaveChangesAsync();
        }

        if (!await context.Categories.AnyAsync())
        {
            context.Categories.AddRange(
                new Category { Slug = "food-street", Name = "Pho am thuc", Description = "Danh muc mon an va cum quan an.", ThemeColor = "#c97732", SortOrder = 1 },
                new Category { Slug = "history", Name = "Lich su dia phuong", Description = "Cac diem ke chuyen lich su khu vuc.", ThemeColor = "#17324d", SortOrder = 2 },
                new Category { Slug = "culture", Name = "Van hoa - doi song", Description = "Net sinh hoat va van hoa pho Vinh Khanh.", ThemeColor = "#2a9d8f", SortOrder = 3 },
                new Category { Slug = "check-in", Name = "Check-in - trai nghiem", Description = "Cac diem dung chan va chup anh.", ThemeColor = "#6d597a", SortOrder = 4 });

            await context.SaveChangesAsync();
        }
    }

    private static async Task EnsureLanguageTableAsync(AppDbContext context)
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
}
