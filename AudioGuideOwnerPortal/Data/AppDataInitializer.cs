using Microsoft.EntityFrameworkCore;

namespace AudioGuideOwnerPortal.Data;

public static class AppDataInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (context.Database.IsSqlite())
        {
            await EnsureSqliteOwnerWorkflowTablesAsync(context);
            return;
        }

        await EnsureOwnerWorkflowTablesAsync(context);
    }

    private static async Task EnsureSqliteOwnerWorkflowTablesAsync(AppDbContext context)
    {
        await context.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS "ShopOwners" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_ShopOwners" PRIMARY KEY,
                "FullName" TEXT NOT NULL DEFAULT '',
                "Phone" TEXT NOT NULL DEFAULT '',
                "Email" TEXT NOT NULL DEFAULT '',
                "BusinessName" TEXT NOT NULL DEFAULT '',
                "PasswordHash" TEXT NOT NULL DEFAULT '',
                "PasswordSalt" TEXT NOT NULL DEFAULT '',
                "Status" TEXT NOT NULL DEFAULT 'pending',
                "AdminNote" TEXT NOT NULL DEFAULT '',
                "CreatedAt" TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                "UpdatedAt" TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                "ApprovedAt" TEXT NULL,
                "LastLoginAt" TEXT NULL,
                "ApprovedByAdminId" INTEGER NULL
            );
            """);

        await context.Database.ExecuteSqlRawAsync(
            """
            CREATE INDEX IF NOT EXISTS "IX_ShopOwners_Status"
            ON "ShopOwners" ("Status");
            """);

        await context.Database.ExecuteSqlRawAsync(
            """
            CREATE INDEX IF NOT EXISTS "IX_ShopOwners_Phone"
            ON "ShopOwners" ("Phone");
            """);

        await context.Database.ExecuteSqlRawAsync(
            """
            CREATE INDEX IF NOT EXISTS "IX_ShopOwners_Email"
            ON "ShopOwners" ("Email");
            """);

        if (await SqliteTableExistsAsync(context, "Pois") && !await SqliteColumnExistsAsync(context, "Pois", "OwnerId"))
        {
            await context.Database.ExecuteSqlRawAsync("""ALTER TABLE "Pois" ADD COLUMN "OwnerId" TEXT NULL;""");
        }

        if (await SqliteTableExistsAsync(context, "Pois"))
        {
            await context.Database.ExecuteSqlRawAsync(
                """
                CREATE TABLE IF NOT EXISTS "PoiTranslations" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_PoiTranslations" PRIMARY KEY AUTOINCREMENT,
                    "PoiId" INTEGER NOT NULL,
                    "Language" TEXT NOT NULL DEFAULT 'vi-VN',
                    "Title" TEXT NOT NULL DEFAULT '',
                    "Summary" TEXT NOT NULL DEFAULT '',
                    "Description" TEXT NOT NULL DEFAULT '',
                    "AudioUrl" TEXT NOT NULL DEFAULT '',
                    "TtsScript" TEXT NOT NULL DEFAULT '',
                    "VoiceName" TEXT NOT NULL DEFAULT '',
                    "IsPublished" INTEGER NOT NULL DEFAULT 1,
                    "UpdatedAt" TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    CONSTRAINT "FK_PoiTranslations_Pois_PoiId"
                        FOREIGN KEY ("PoiId") REFERENCES "Pois" ("Id") ON DELETE CASCADE
                );
                """);

            await context.Database.ExecuteSqlRawAsync(
                """
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_PoiTranslations_PoiId_Language"
                ON "PoiTranslations" ("PoiId", "Language");
                """);

            await context.Database.ExecuteSqlRawAsync(
                """
                CREATE INDEX IF NOT EXISTS "IX_Pois_OwnerId"
                ON "Pois" ("OwnerId");
                """);

            await context.Database.ExecuteSqlRawAsync(
                """
                CREATE TABLE IF NOT EXISTS "PoiSubmissions" (
                    "Id" TEXT NOT NULL CONSTRAINT "PK_PoiSubmissions" PRIMARY KEY,
                    "PoiId" INTEGER NULL,
                    "OwnerId" TEXT NOT NULL,
                    "SubmissionType" TEXT NOT NULL DEFAULT 'create',
                    "Status" TEXT NOT NULL DEFAULT 'draft',
                    "ReviewNote" TEXT NOT NULL DEFAULT '',
                    "Name" TEXT NOT NULL DEFAULT '',
                    "Category" TEXT NOT NULL DEFAULT 'food-street',
                    "Summary" TEXT NOT NULL DEFAULT '',
                    "Description" TEXT NOT NULL DEFAULT '',
                    "Address" TEXT NOT NULL DEFAULT '',
                    "Latitude" REAL NOT NULL DEFAULT 0,
                    "Longitude" REAL NOT NULL DEFAULT 0,
                    "Radius" INTEGER NOT NULL DEFAULT 0,
                    "ApproachRadiusMeters" INTEGER NOT NULL DEFAULT 90,
                    "Priority" INTEGER NOT NULL DEFAULT 1,
                    "DebounceSeconds" INTEGER NOT NULL DEFAULT 15,
                    "CooldownSeconds" INTEGER NOT NULL DEFAULT 120,
                    "TriggerMode" TEXT NOT NULL DEFAULT 'both',
                    "ImageUrl" TEXT NOT NULL DEFAULT '',
                    "MapUrl" TEXT NOT NULL DEFAULT '',
                    "IsActive" INTEGER NOT NULL DEFAULT 1,
                    "AudioMode" TEXT NOT NULL DEFAULT 'tts',
                    "AudioUrl" TEXT NOT NULL DEFAULT '',
                    "TtsScript" TEXT NOT NULL DEFAULT '',
                    "DefaultLanguage" TEXT NOT NULL DEFAULT 'vi-VN',
                    "EstimatedDurationSeconds" INTEGER NOT NULL DEFAULT 60,
                    "CreatedAt" TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    "UpdatedAt" TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    "SubmittedAt" TEXT NULL,
                    "ReviewedAt" TEXT NULL,
                    "ReviewedByAdminId" INTEGER NULL,
                    CONSTRAINT "FK_PoiSubmissions_Pois_PoiId"
                        FOREIGN KEY ("PoiId") REFERENCES "Pois" ("Id") ON DELETE SET NULL,
                    CONSTRAINT "FK_PoiSubmissions_ShopOwners_OwnerId"
                        FOREIGN KEY ("OwnerId") REFERENCES "ShopOwners" ("Id") ON DELETE CASCADE
                );
                """);

            await context.Database.ExecuteSqlRawAsync(
                """
                CREATE INDEX IF NOT EXISTS "IX_PoiSubmissions_OwnerId_Status"
                ON "PoiSubmissions" ("OwnerId", "Status");
                """);

            await context.Database.ExecuteSqlRawAsync(
                """
                CREATE TABLE IF NOT EXISTS "PoiTranslationSubmissions" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_PoiTranslationSubmissions" PRIMARY KEY AUTOINCREMENT,
                    "SubmissionId" TEXT NOT NULL,
                    "Language" TEXT NOT NULL DEFAULT 'vi-VN',
                    "Title" TEXT NOT NULL DEFAULT '',
                    "Summary" TEXT NOT NULL DEFAULT '',
                    "Description" TEXT NOT NULL DEFAULT '',
                    "AudioUrl" TEXT NOT NULL DEFAULT '',
                    "TtsScript" TEXT NOT NULL DEFAULT '',
                    "VoiceName" TEXT NOT NULL DEFAULT '',
                    "SortOrder" INTEGER NOT NULL DEFAULT 0,
                    "UpdatedAt" TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    CONSTRAINT "FK_PoiTranslationSubmissions_PoiSubmissions_SubmissionId"
                        FOREIGN KEY ("SubmissionId") REFERENCES "PoiSubmissions" ("Id") ON DELETE CASCADE
                );
                """);

            await context.Database.ExecuteSqlRawAsync(
                """
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_PoiTranslationSubmissions_SubmissionId_Language"
                ON "PoiTranslationSubmissions" ("SubmissionId", "Language");
                """);
        }
    }

    private static async Task EnsureOwnerWorkflowTablesAsync(AppDbContext context)
    {
        await context.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'[ShopOwners]', N'U') IS NULL
            BEGIN
                CREATE TABLE [ShopOwners](
                    [Id] nvarchar(64) NOT NULL,
                    [FullName] nvarchar(200) NOT NULL CONSTRAINT [DF_ShopOwners_FullName] DEFAULT N'',
                    [Phone] nvarchar(50) NOT NULL CONSTRAINT [DF_ShopOwners_Phone] DEFAULT N'',
                    [Email] nvarchar(150) NOT NULL CONSTRAINT [DF_ShopOwners_Email] DEFAULT N'',
                    [BusinessName] nvarchar(200) NOT NULL CONSTRAINT [DF_ShopOwners_BusinessName] DEFAULT N'',
                    [PasswordHash] nvarchar(200) NOT NULL CONSTRAINT [DF_ShopOwners_PasswordHash] DEFAULT N'',
                    [PasswordSalt] nvarchar(200) NOT NULL CONSTRAINT [DF_ShopOwners_PasswordSalt] DEFAULT N'',
                    [Status] nvarchar(40) NOT NULL CONSTRAINT [DF_ShopOwners_Status] DEFAULT N'pending',
                    [AdminNote] nvarchar(1000) NOT NULL CONSTRAINT [DF_ShopOwners_AdminNote] DEFAULT N'',
                    [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_ShopOwners_CreatedAt] DEFAULT SYSUTCDATETIME(),
                    [UpdatedAt] datetime2 NOT NULL CONSTRAINT [DF_ShopOwners_UpdatedAt] DEFAULT SYSUTCDATETIME(),
                    [ApprovedAt] datetime2 NULL,
                    [LastLoginAt] datetime2 NULL,
                    [ApprovedByAdminId] int NULL,
                    CONSTRAINT [PK_ShopOwners] PRIMARY KEY ([Id])
                );
            END
            """);

        await context.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'[ShopOwners]', N'U') IS NOT NULL
                AND NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = 'IX_ShopOwners_Status' AND object_id = OBJECT_ID(N'[ShopOwners]')
                )
            BEGIN
                CREATE INDEX [IX_ShopOwners_Status] ON [ShopOwners]([Status]);
            END
            """);

        await context.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'[ShopOwners]', N'U') IS NOT NULL
                AND NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = 'IX_ShopOwners_Phone' AND object_id = OBJECT_ID(N'[ShopOwners]')
                )
            BEGIN
                CREATE INDEX [IX_ShopOwners_Phone] ON [ShopOwners]([Phone]);
            END
            """);

        await context.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'[ShopOwners]', N'U') IS NOT NULL
                AND NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = 'IX_ShopOwners_Email' AND object_id = OBJECT_ID(N'[ShopOwners]')
                )
            BEGIN
                CREATE INDEX [IX_ShopOwners_Email] ON [ShopOwners]([Email]);
            END
            """);

        await context.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'[Pois]', N'U') IS NOT NULL AND OBJECT_ID(N'[PoiTranslations]', N'U') IS NULL
            BEGIN
                CREATE TABLE [PoiTranslations](
                    [Id] int IDENTITY(1,1) NOT NULL,
                    [PoiId] int NOT NULL,
                    [Language] nvarchar(20) NOT NULL CONSTRAINT [DF_PoiTranslations_Language] DEFAULT N'vi-VN',
                    [Title] nvarchar(max) NOT NULL CONSTRAINT [DF_PoiTranslations_Title] DEFAULT N'',
                    [Summary] nvarchar(max) NOT NULL CONSTRAINT [DF_PoiTranslations_Summary] DEFAULT N'',
                    [Description] nvarchar(max) NOT NULL CONSTRAINT [DF_PoiTranslations_Description] DEFAULT N'',
                    [AudioUrl] nvarchar(max) NOT NULL CONSTRAINT [DF_PoiTranslations_AudioUrl] DEFAULT N'',
                    [TtsScript] nvarchar(max) NOT NULL CONSTRAINT [DF_PoiTranslations_TtsScript] DEFAULT N'',
                    [VoiceName] nvarchar(max) NOT NULL CONSTRAINT [DF_PoiTranslations_VoiceName] DEFAULT N'',
                    [IsPublished] bit NOT NULL CONSTRAINT [DF_PoiTranslations_IsPublished] DEFAULT 1,
                    [UpdatedAt] datetime2 NOT NULL CONSTRAINT [DF_PoiTranslations_UpdatedAt] DEFAULT SYSUTCDATETIME(),
                    CONSTRAINT [PK_PoiTranslations] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_PoiTranslations_Pois_PoiId] FOREIGN KEY ([PoiId]) REFERENCES [Pois]([Id]) ON DELETE CASCADE
                );
            END
            """);

        await context.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'[PoiTranslations]', N'U') IS NOT NULL
                AND NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = 'IX_PoiTranslations_PoiId_Language' AND object_id = OBJECT_ID(N'[PoiTranslations]')
                )
            BEGIN
                CREATE UNIQUE INDEX [IX_PoiTranslations_PoiId_Language] ON [PoiTranslations]([PoiId], [Language]);
            END
            """);

        await context.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'[Pois]', N'U') IS NOT NULL
               AND COL_LENGTH('Pois', 'OwnerId') IS NULL
            BEGIN
                ALTER TABLE [Pois] ADD [OwnerId] nvarchar(64) NULL;
            END
            """);

        await context.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'[Pois]', N'U') IS NOT NULL
                AND NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = 'IX_Pois_OwnerId' AND object_id = OBJECT_ID(N'[Pois]')
                )
            BEGIN
                CREATE INDEX [IX_Pois_OwnerId] ON [Pois]([OwnerId]);
            END
            """);

        await context.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'[Pois]', N'U') IS NOT NULL AND OBJECT_ID(N'[PoiSubmissions]', N'U') IS NULL
            BEGIN
                CREATE TABLE [PoiSubmissions](
                    [Id] nvarchar(64) NOT NULL,
                    [PoiId] int NULL,
                    [OwnerId] nvarchar(64) NOT NULL,
                    [SubmissionType] nvarchar(20) NOT NULL CONSTRAINT [DF_PoiSubmissions_SubmissionType] DEFAULT N'create',
                    [Status] nvarchar(40) NOT NULL CONSTRAINT [DF_PoiSubmissions_Status] DEFAULT N'draft',
                    [ReviewNote] nvarchar(1000) NOT NULL CONSTRAINT [DF_PoiSubmissions_ReviewNote] DEFAULT N'',
                    [Name] nvarchar(200) NOT NULL CONSTRAINT [DF_PoiSubmissions_Name] DEFAULT N'',
                    [Category] nvarchar(100) NOT NULL CONSTRAINT [DF_PoiSubmissions_Category] DEFAULT N'food-street',
                    [Summary] nvarchar(max) NOT NULL CONSTRAINT [DF_PoiSubmissions_Summary] DEFAULT N'',
                    [Description] nvarchar(max) NOT NULL CONSTRAINT [DF_PoiSubmissions_Description] DEFAULT N'',
                    [Address] nvarchar(max) NOT NULL CONSTRAINT [DF_PoiSubmissions_Address] DEFAULT N'',
                    [Latitude] float NOT NULL CONSTRAINT [DF_PoiSubmissions_Latitude] DEFAULT 0,
                    [Longitude] float NOT NULL CONSTRAINT [DF_PoiSubmissions_Longitude] DEFAULT 0,
                    [Radius] int NOT NULL CONSTRAINT [DF_PoiSubmissions_Radius] DEFAULT 0,
                    [ApproachRadiusMeters] int NOT NULL CONSTRAINT [DF_PoiSubmissions_ApproachRadiusMeters] DEFAULT 90,
                    [Priority] int NOT NULL CONSTRAINT [DF_PoiSubmissions_Priority] DEFAULT 1,
                    [DebounceSeconds] int NOT NULL CONSTRAINT [DF_PoiSubmissions_DebounceSeconds] DEFAULT 15,
                    [CooldownSeconds] int NOT NULL CONSTRAINT [DF_PoiSubmissions_CooldownSeconds] DEFAULT 120,
                    [TriggerMode] nvarchar(40) NOT NULL CONSTRAINT [DF_PoiSubmissions_TriggerMode] DEFAULT N'both',
                    [ImageUrl] nvarchar(max) NOT NULL CONSTRAINT [DF_PoiSubmissions_ImageUrl] DEFAULT N'',
                    [MapUrl] nvarchar(max) NOT NULL CONSTRAINT [DF_PoiSubmissions_MapUrl] DEFAULT N'',
                    [IsActive] bit NOT NULL CONSTRAINT [DF_PoiSubmissions_IsActive] DEFAULT 1,
                    [AudioMode] nvarchar(40) NOT NULL CONSTRAINT [DF_PoiSubmissions_AudioMode] DEFAULT N'tts',
                    [AudioUrl] nvarchar(max) NOT NULL CONSTRAINT [DF_PoiSubmissions_AudioUrl] DEFAULT N'',
                    [TtsScript] nvarchar(max) NOT NULL CONSTRAINT [DF_PoiSubmissions_TtsScript] DEFAULT N'',
                    [DefaultLanguage] nvarchar(20) NOT NULL CONSTRAINT [DF_PoiSubmissions_DefaultLanguage] DEFAULT N'vi-VN',
                    [EstimatedDurationSeconds] int NOT NULL CONSTRAINT [DF_PoiSubmissions_EstimatedDurationSeconds] DEFAULT 60,
                    [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_PoiSubmissions_CreatedAt] DEFAULT SYSUTCDATETIME(),
                    [UpdatedAt] datetime2 NOT NULL CONSTRAINT [DF_PoiSubmissions_UpdatedAt] DEFAULT SYSUTCDATETIME(),
                    [SubmittedAt] datetime2 NULL,
                    [ReviewedAt] datetime2 NULL,
                    [ReviewedByAdminId] int NULL,
                    CONSTRAINT [PK_PoiSubmissions] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_PoiSubmissions_Pois_PoiId] FOREIGN KEY ([PoiId]) REFERENCES [Pois]([Id]) ON DELETE SET NULL,
                    CONSTRAINT [FK_PoiSubmissions_ShopOwners_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [ShopOwners]([Id]) ON DELETE CASCADE
                );
            END
            """);

        await context.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'[PoiSubmissions]', N'U') IS NOT NULL
                AND NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = 'IX_PoiSubmissions_OwnerId_Status' AND object_id = OBJECT_ID(N'[PoiSubmissions]')
                )
            BEGIN
                CREATE INDEX [IX_PoiSubmissions_OwnerId_Status] ON [PoiSubmissions]([OwnerId], [Status]);
            END
            """);

        await context.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'[PoiTranslationSubmissions]', N'U') IS NULL
            BEGIN
                CREATE TABLE [PoiTranslationSubmissions](
                    [Id] int IDENTITY(1,1) NOT NULL,
                    [SubmissionId] nvarchar(64) NOT NULL,
                    [Language] nvarchar(20) NOT NULL CONSTRAINT [DF_PoiTranslationSubmissions_Language] DEFAULT N'vi-VN',
                    [Title] nvarchar(max) NOT NULL CONSTRAINT [DF_PoiTranslationSubmissions_Title] DEFAULT N'',
                    [Summary] nvarchar(max) NOT NULL CONSTRAINT [DF_PoiTranslationSubmissions_Summary] DEFAULT N'',
                    [Description] nvarchar(max) NOT NULL CONSTRAINT [DF_PoiTranslationSubmissions_Description] DEFAULT N'',
                    [AudioUrl] nvarchar(max) NOT NULL CONSTRAINT [DF_PoiTranslationSubmissions_AudioUrl] DEFAULT N'',
                    [TtsScript] nvarchar(max) NOT NULL CONSTRAINT [DF_PoiTranslationSubmissions_TtsScript] DEFAULT N'',
                    [VoiceName] nvarchar(max) NOT NULL CONSTRAINT [DF_PoiTranslationSubmissions_VoiceName] DEFAULT N'',
                    [SortOrder] int NOT NULL CONSTRAINT [DF_PoiTranslationSubmissions_SortOrder] DEFAULT 0,
                    [UpdatedAt] datetime2 NOT NULL CONSTRAINT [DF_PoiTranslationSubmissions_UpdatedAt] DEFAULT SYSUTCDATETIME(),
                    CONSTRAINT [PK_PoiTranslationSubmissions] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_PoiTranslationSubmissions_PoiSubmissions_SubmissionId] FOREIGN KEY ([SubmissionId]) REFERENCES [PoiSubmissions]([Id]) ON DELETE CASCADE
                );
            END
            """);

        await context.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'[PoiTranslationSubmissions]', N'U') IS NOT NULL
                AND NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = 'IX_PoiTranslationSubmissions_SubmissionId_Language' AND object_id = OBJECT_ID(N'[PoiTranslationSubmissions]')
                )
            BEGIN
                CREATE UNIQUE INDEX [IX_PoiTranslationSubmissions_SubmissionId_Language] ON [PoiTranslationSubmissions]([SubmissionId], [Language]);
            END
            """);
    }

    private static async Task<bool> SqliteTableExistsAsync(AppDbContext context, string tableName)
    {
        var connection = context.Database.GetDbConnection();
        var shouldClose = connection.State != System.Data.ConnectionState.Open;
        if (shouldClose)
        {
            await connection.OpenAsync();
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT COUNT(1)
                FROM sqlite_master
                WHERE type = 'table' AND name = $name;
                """;
            var parameter = command.CreateParameter();
            parameter.ParameterName = "$name";
            parameter.Value = tableName;
            command.Parameters.Add(parameter);
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result) > 0;
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static async Task<bool> SqliteColumnExistsAsync(AppDbContext context, string tableName, string columnName)
    {
        var connection = context.Database.GetDbConnection();
        var shouldClose = connection.State != System.Data.ConnectionState.Open;
        if (shouldClose)
        {
            await connection.OpenAsync();
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = $"PRAGMA table_info(\"{tableName}\");";

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                if (string.Equals(reader["name"]?.ToString(), columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }
}
