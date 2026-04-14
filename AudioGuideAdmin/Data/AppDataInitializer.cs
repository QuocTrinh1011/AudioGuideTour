using AudioGuideAdmin.Models;
using Microsoft.EntityFrameworkCore;

namespace AudioGuideAdmin.Data;

public static class AppDataInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (context.Database.IsSqlite())
        {
            await context.Database.EnsureCreatedAsync();
            await EnsureSqliteAdminUserTableAsync(context);
            await EnsureSqliteRegistrationTablesAsync(context);
            await SeedLanguagesAsync(context);
            await NormalizeUnsupportedLanguageReferencesAsync(context);
            await SeedRegistrationPlansAsync(context);
            await SeedCategoriesAsync(context);
            await SeedDemoContentAsync(context);
            await EnsureDemoTranslationCoverageAsync(context);
            await EnsureDemoMediaLinksAsync(context);
            await EnsureDemoEnglishAudioLinksAsync(context);
            return;
        }

        await context.Database.MigrateAsync();
        await EnsureVisitorTableAsync(context);
        await EnsureAdminUserTableAsync(context);
        await EnsureCategoryTableAsync(context);
        await EnsureLanguageTableAsync(context);
        await EnsureQrCodeTableAsync(context);
        await EnsureRegistrationTablesAsync(context);
        await SeedLanguagesAsync(context);
        await NormalizeUnsupportedLanguageReferencesAsync(context);
        await SeedRegistrationPlansAsync(context);
        await SeedCategoriesAsync(context);
        await SeedDemoContentAsync(context);
        await EnsureDemoTranslationCoverageAsync(context);
        await EnsureDemoMediaLinksAsync(context);
        await EnsureDemoEnglishAudioLinksAsync(context);
    }

    private static async Task EnsureSqliteAdminUserTableAsync(AppDbContext context)
    {
        if (!context.Database.IsSqlite())
        {
            return;
        }

        await context.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS "AdminUsers" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_AdminUsers" PRIMARY KEY AUTOINCREMENT,
                "Username" TEXT NOT NULL,
                "Password" TEXT NOT NULL,
                "DisplayName" TEXT NOT NULL DEFAULT '',
                "CreatedAt" TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
            );
            """);

        await context.Database.ExecuteSqlRawAsync(
            """
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_AdminUsers_Username"
            ON "AdminUsers" ("Username");
            """);
    }

    private static async Task EnsureSqliteRegistrationTablesAsync(AppDbContext context)
    {
        if (!context.Database.IsSqlite())
        {
            return;
        }

        await context.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS "RegistrationPlans" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_RegistrationPlans" PRIMARY KEY AUTOINCREMENT,
                "Code" TEXT NOT NULL,
                "Name" TEXT NOT NULL DEFAULT '',
                "Description" TEXT NOT NULL DEFAULT '',
                "HighlightText" TEXT NOT NULL DEFAULT '',
                "Price" INTEGER NOT NULL DEFAULT 0,
                "DurationDays" INTEGER NOT NULL DEFAULT 0,
                "Currency" TEXT NOT NULL DEFAULT 'VND',
                "IsActive" INTEGER NOT NULL DEFAULT 1,
                "SortOrder" INTEGER NOT NULL DEFAULT 1
            );
            """);

        await context.Database.ExecuteSqlRawAsync(
            """
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_RegistrationPlans_Code"
            ON "RegistrationPlans" ("Code");
            """);

        await context.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS "MembershipRegistrations" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_MembershipRegistrations" PRIMARY KEY,
                "VisitorId" TEXT NOT NULL DEFAULT '',
                "DeviceId" TEXT NOT NULL DEFAULT '',
                "FullName" TEXT NOT NULL DEFAULT '',
                "Phone" TEXT NOT NULL DEFAULT '',
                "Email" TEXT NOT NULL DEFAULT '',
                "PreferredLanguage" TEXT NOT NULL DEFAULT 'vi-VN',
                "Source" TEXT NOT NULL DEFAULT 'mobile',
                "Status" TEXT NOT NULL DEFAULT 'pending-form',
                "PaymentStatus" TEXT NOT NULL DEFAULT 'FORM_ONLY',
                "RegistrationPlanId" INTEGER NULL,
                "Amount" INTEGER NOT NULL DEFAULT 0,
                "Currency" TEXT NOT NULL DEFAULT 'VND',
                "OrderCode" INTEGER NULL,
                "PaymentLinkId" TEXT NOT NULL DEFAULT '',
                "CheckoutUrl" TEXT NOT NULL DEFAULT '',
                "QrCode" TEXT NOT NULL DEFAULT '',
                "ReturnToken" TEXT NOT NULL DEFAULT '',
                "Note" TEXT NOT NULL DEFAULT '',
                "AdminNote" TEXT NOT NULL DEFAULT '',
                "CreatedAt" TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                "UpdatedAt" TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                "FormSubmittedAt" TEXT NULL,
                "PaymentStartedAt" TEXT NULL,
                "PaidAt" TEXT NULL,
                "CancelledAt" TEXT NULL,
                "LastSyncedAt" TEXT NULL,
                CONSTRAINT "FK_MembershipRegistrations_RegistrationPlans_RegistrationPlanId"
                    FOREIGN KEY ("RegistrationPlanId") REFERENCES "RegistrationPlans" ("Id") ON DELETE SET NULL
            );
            """);

        await context.Database.ExecuteSqlRawAsync(
            """
            CREATE INDEX IF NOT EXISTS "IX_MembershipRegistrations_VisitorId"
            ON "MembershipRegistrations" ("VisitorId");
            """);

        await context.Database.ExecuteSqlRawAsync(
            """
            CREATE INDEX IF NOT EXISTS "IX_MembershipRegistrations_DeviceId"
            ON "MembershipRegistrations" ("DeviceId");
            """);

        await context.Database.ExecuteSqlRawAsync(
            """
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_MembershipRegistrations_OrderCode"
            ON "MembershipRegistrations" ("OrderCode");
            """);

        await context.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS "CustomerAccounts" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_CustomerAccounts" PRIMARY KEY,
                "RegistrationId" TEXT NOT NULL,
                "FullName" TEXT NOT NULL DEFAULT '',
                "Phone" TEXT NOT NULL DEFAULT '',
                "Email" TEXT NOT NULL DEFAULT '',
                "PreferredLanguage" TEXT NOT NULL DEFAULT 'vi-VN',
                "PasswordHash" TEXT NOT NULL DEFAULT '',
                "PasswordSalt" TEXT NOT NULL DEFAULT '',
                "IsActive" INTEGER NOT NULL DEFAULT 0,
                "IsPaid" INTEGER NOT NULL DEFAULT 0,
                "Status" TEXT NOT NULL DEFAULT 'pending-payment',
                "SessionToken" TEXT NOT NULL DEFAULT '',
                "SessionExpiresAt" TEXT NULL,
                "CreatedAt" TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                "UpdatedAt" TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                "PaidAt" TEXT NULL,
                "LastLoginAt" TEXT NULL,
                CONSTRAINT "FK_CustomerAccounts_MembershipRegistrations_RegistrationId"
                    FOREIGN KEY ("RegistrationId") REFERENCES "MembershipRegistrations" ("Id") ON DELETE CASCADE
            );
            """);

        await context.Database.ExecuteSqlRawAsync(
            """
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_CustomerAccounts_RegistrationId"
            ON "CustomerAccounts" ("RegistrationId");
            """);

        await context.Database.ExecuteSqlRawAsync(
            """
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_CustomerAccounts_Phone"
            ON "CustomerAccounts" ("Phone");
            """);

        await context.Database.ExecuteSqlRawAsync(
            """
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_CustomerAccounts_Email"
            ON "CustomerAccounts" ("Email");
            """);

        await context.Database.ExecuteSqlRawAsync(
            """
            CREATE INDEX IF NOT EXISTS "IX_CustomerAccounts_SessionToken"
            ON "CustomerAccounts" ("SessionToken");
            """);
    }

    public static async Task EnsureVisitorTableAsync(AppDbContext context)
    {
        await context.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'[Visitors]', N'U') IS NULL
            BEGIN
                CREATE TABLE [Visitors](
                    [Id] nvarchar(450) NOT NULL,
                    [DeviceId] nvarchar(450) NOT NULL,
                    [DisplayName] nvarchar(200) NOT NULL CONSTRAINT [DF_Visitors_DisplayName] DEFAULT N'Khach an danh',
                    [Language] nvarchar(20) NOT NULL CONSTRAINT [DF_Visitors_Language] DEFAULT N'vi-VN',
                    [AllowBackgroundTracking] bit NOT NULL CONSTRAINT [DF_Visitors_AllowBackgroundTracking] DEFAULT 1,
                    [AllowAutoPlay] bit NOT NULL CONSTRAINT [DF_Visitors_AllowAutoPlay] DEFAULT 1,
                    [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_Visitors_CreatedAt] DEFAULT SYSUTCDATETIME(),
                    [LastSeenAt] datetime2 NOT NULL CONSTRAINT [DF_Visitors_LastSeenAt] DEFAULT SYSUTCDATETIME(),
                    CONSTRAINT [PK_Visitors] PRIMARY KEY ([Id])
                );
            END
            """);

        await context.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'[Users]', N'U') IS NOT NULL
               AND COL_LENGTH('Users', 'DeviceId') IS NOT NULL
               AND COL_LENGTH('Users', 'Language') IS NOT NULL
               AND OBJECT_ID(N'[Visitors]', N'U') IS NOT NULL
            BEGIN
                INSERT INTO [Visitors]([Id], [DeviceId], [DisplayName], [Language], [AllowBackgroundTracking], [AllowAutoPlay], [CreatedAt], [LastSeenAt])
                SELECT [Id], [DeviceId], [DisplayName], [Language], [AllowBackgroundTracking], [AllowAutoPlay], [CreatedAt], [LastSeenAt]
                FROM [Users] source
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM [Visitors] target
                    WHERE target.[Id] = source.[Id]
                );
            END
            """);
    }

    public static async Task EnsureAdminUserTableAsync(AppDbContext context)
    {
        await context.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'[AdminUsers]', N'U') IS NULL
            BEGIN
                CREATE TABLE [AdminUsers](
                    [Id] int NOT NULL IDENTITY,
                    [Username] nvarchar(450) NOT NULL,
                    [Password] nvarchar(max) NOT NULL,
                    [DisplayName] nvarchar(max) NOT NULL CONSTRAINT [DF_AdminUsers_DisplayName_Runtime] DEFAULT N'',
                    [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_AdminUsers_CreatedAt_Runtime] DEFAULT SYSUTCDATETIME(),
                    CONSTRAINT [PK_AdminUsers] PRIMARY KEY ([Id])
                );
            END
            """);

        await context.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'[AdminUsers]', N'U') IS NOT NULL
                AND NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'IX_AdminUsers_Username'
                      AND object_id = OBJECT_ID(N'[AdminUsers]')
                )
            BEGIN
                CREATE UNIQUE INDEX [IX_AdminUsers_Username] ON [AdminUsers]([Username]);
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

    public static async Task EnsureRegistrationTablesAsync(AppDbContext context)
    {
        await context.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'[RegistrationPlans]', N'U') IS NULL
            BEGIN
                CREATE TABLE [RegistrationPlans](
                    [Id] int NOT NULL IDENTITY,
                    [Code] nvarchar(50) NOT NULL,
                    [Name] nvarchar(150) NOT NULL CONSTRAINT [DF_RegistrationPlans_Name] DEFAULT N'',
                    [Description] nvarchar(max) NOT NULL CONSTRAINT [DF_RegistrationPlans_Description] DEFAULT N'',
                    [HighlightText] nvarchar(200) NOT NULL CONSTRAINT [DF_RegistrationPlans_HighlightText] DEFAULT N'',
                    [Price] int NOT NULL CONSTRAINT [DF_RegistrationPlans_Price] DEFAULT 0,
                    [DurationDays] int NOT NULL CONSTRAINT [DF_RegistrationPlans_DurationDays] DEFAULT 0,
                    [Currency] nvarchar(10) NOT NULL CONSTRAINT [DF_RegistrationPlans_Currency] DEFAULT N'VND',
                    [IsActive] bit NOT NULL CONSTRAINT [DF_RegistrationPlans_IsActive] DEFAULT 1,
                    [SortOrder] int NOT NULL CONSTRAINT [DF_RegistrationPlans_SortOrder] DEFAULT 1,
                    CONSTRAINT [PK_RegistrationPlans] PRIMARY KEY ([Id])
                );
            END
            """);

        await context.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'[RegistrationPlans]', N'U') IS NOT NULL
                AND NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'IX_RegistrationPlans_Code'
                      AND object_id = OBJECT_ID(N'[RegistrationPlans]')
                )
            BEGIN
                CREATE UNIQUE INDEX [IX_RegistrationPlans_Code] ON [RegistrationPlans]([Code]);
            END
            """);

        await context.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'[MembershipRegistrations]', N'U') IS NULL
            BEGIN
                CREATE TABLE [MembershipRegistrations](
                    [Id] nvarchar(64) NOT NULL,
                    [VisitorId] nvarchar(100) NOT NULL CONSTRAINT [DF_MembershipRegistrations_VisitorId] DEFAULT N'',
                    [DeviceId] nvarchar(100) NOT NULL CONSTRAINT [DF_MembershipRegistrations_DeviceId] DEFAULT N'',
                    [FullName] nvarchar(200) NOT NULL CONSTRAINT [DF_MembershipRegistrations_FullName] DEFAULT N'',
                    [Phone] nvarchar(50) NOT NULL CONSTRAINT [DF_MembershipRegistrations_Phone] DEFAULT N'',
                    [Email] nvarchar(150) NOT NULL CONSTRAINT [DF_MembershipRegistrations_Email] DEFAULT N'',
                    [PreferredLanguage] nvarchar(20) NOT NULL CONSTRAINT [DF_MembershipRegistrations_PreferredLanguage] DEFAULT N'vi-VN',
                    [Source] nvarchar(50) NOT NULL CONSTRAINT [DF_MembershipRegistrations_Source] DEFAULT N'mobile',
                    [Status] nvarchar(50) NOT NULL CONSTRAINT [DF_MembershipRegistrations_Status] DEFAULT N'pending-form',
                    [PaymentStatus] nvarchar(50) NOT NULL CONSTRAINT [DF_MembershipRegistrations_PaymentStatus] DEFAULT N'FORM_ONLY',
                    [RegistrationPlanId] int NULL,
                    [Amount] int NOT NULL CONSTRAINT [DF_MembershipRegistrations_Amount] DEFAULT 0,
                    [Currency] nvarchar(10) NOT NULL CONSTRAINT [DF_MembershipRegistrations_Currency] DEFAULT N'VND',
                    [OrderCode] bigint NULL,
                    [PaymentLinkId] nvarchar(100) NOT NULL CONSTRAINT [DF_MembershipRegistrations_PaymentLinkId] DEFAULT N'',
                    [CheckoutUrl] nvarchar(max) NOT NULL CONSTRAINT [DF_MembershipRegistrations_CheckoutUrl] DEFAULT N'',
                    [QrCode] nvarchar(max) NOT NULL CONSTRAINT [DF_MembershipRegistrations_QrCode] DEFAULT N'',
                    [ReturnToken] nvarchar(100) NOT NULL CONSTRAINT [DF_MembershipRegistrations_ReturnToken] DEFAULT N'',
                    [Note] nvarchar(max) NOT NULL CONSTRAINT [DF_MembershipRegistrations_Note] DEFAULT N'',
                    [AdminNote] nvarchar(max) NOT NULL CONSTRAINT [DF_MembershipRegistrations_AdminNote] DEFAULT N'',
                    [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_MembershipRegistrations_CreatedAt] DEFAULT SYSUTCDATETIME(),
                    [UpdatedAt] datetime2 NOT NULL CONSTRAINT [DF_MembershipRegistrations_UpdatedAt] DEFAULT SYSUTCDATETIME(),
                    [FormSubmittedAt] datetime2 NULL,
                    [PaymentStartedAt] datetime2 NULL,
                    [PaidAt] datetime2 NULL,
                    [CancelledAt] datetime2 NULL,
                    [LastSyncedAt] datetime2 NULL,
                    CONSTRAINT [PK_MembershipRegistrations] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_MembershipRegistrations_RegistrationPlans_RegistrationPlanId] FOREIGN KEY ([RegistrationPlanId]) REFERENCES [RegistrationPlans]([Id]) ON DELETE SET NULL
                );
            END
            """);

        await context.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'[MembershipRegistrations]', N'U') IS NOT NULL
                AND NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'IX_MembershipRegistrations_VisitorId'
                      AND object_id = OBJECT_ID(N'[MembershipRegistrations]')
                )
            BEGIN
                CREATE INDEX [IX_MembershipRegistrations_VisitorId] ON [MembershipRegistrations]([VisitorId]);
            END
            """);

        await context.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'[MembershipRegistrations]', N'U') IS NOT NULL
                AND NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'IX_MembershipRegistrations_DeviceId'
                      AND object_id = OBJECT_ID(N'[MembershipRegistrations]')
                )
            BEGIN
                CREATE INDEX [IX_MembershipRegistrations_DeviceId] ON [MembershipRegistrations]([DeviceId]);
            END
            """);

        await context.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'[MembershipRegistrations]', N'U') IS NOT NULL
                AND NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'IX_MembershipRegistrations_OrderCode'
                      AND object_id = OBJECT_ID(N'[MembershipRegistrations]')
                )
            BEGIN
                CREATE UNIQUE INDEX [IX_MembershipRegistrations_OrderCode] ON [MembershipRegistrations]([OrderCode]) WHERE [OrderCode] IS NOT NULL;
            END
            """);

        await context.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'[CustomerAccounts]', N'U') IS NULL
            BEGIN
                CREATE TABLE [CustomerAccounts](
                    [Id] nvarchar(64) NOT NULL,
                    [RegistrationId] nvarchar(64) NOT NULL CONSTRAINT [DF_CustomerAccounts_RegistrationId] DEFAULT N'',
                    [FullName] nvarchar(200) NOT NULL CONSTRAINT [DF_CustomerAccounts_FullName] DEFAULT N'',
                    [Phone] nvarchar(50) NOT NULL CONSTRAINT [DF_CustomerAccounts_Phone] DEFAULT N'',
                    [Email] nvarchar(150) NOT NULL CONSTRAINT [DF_CustomerAccounts_Email] DEFAULT N'',
                    [PreferredLanguage] nvarchar(20) NOT NULL CONSTRAINT [DF_CustomerAccounts_PreferredLanguage] DEFAULT N'vi-VN',
                    [PasswordHash] nvarchar(max) NOT NULL CONSTRAINT [DF_CustomerAccounts_PasswordHash] DEFAULT N'',
                    [PasswordSalt] nvarchar(max) NOT NULL CONSTRAINT [DF_CustomerAccounts_PasswordSalt] DEFAULT N'',
                    [IsActive] bit NOT NULL CONSTRAINT [DF_CustomerAccounts_IsActive] DEFAULT 0,
                    [IsPaid] bit NOT NULL CONSTRAINT [DF_CustomerAccounts_IsPaid] DEFAULT 0,
                    [Status] nvarchar(50) NOT NULL CONSTRAINT [DF_CustomerAccounts_Status] DEFAULT N'pending-payment',
                    [SessionToken] nvarchar(100) NOT NULL CONSTRAINT [DF_CustomerAccounts_SessionToken] DEFAULT N'',
                    [SessionExpiresAt] datetime2 NULL,
                    [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_CustomerAccounts_CreatedAt] DEFAULT SYSUTCDATETIME(),
                    [UpdatedAt] datetime2 NOT NULL CONSTRAINT [DF_CustomerAccounts_UpdatedAt] DEFAULT SYSUTCDATETIME(),
                    [PaidAt] datetime2 NULL,
                    [LastLoginAt] datetime2 NULL,
                    CONSTRAINT [PK_CustomerAccounts] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_CustomerAccounts_MembershipRegistrations_RegistrationId] FOREIGN KEY ([RegistrationId]) REFERENCES [MembershipRegistrations]([Id]) ON DELETE CASCADE
                );
            END
            """);

        await context.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'[CustomerAccounts]', N'U') IS NOT NULL
                AND NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'IX_CustomerAccounts_RegistrationId'
                      AND object_id = OBJECT_ID(N'[CustomerAccounts]')
                )
            BEGIN
                CREATE UNIQUE INDEX [IX_CustomerAccounts_RegistrationId] ON [CustomerAccounts]([RegistrationId]);
            END
            """);

        await context.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'[CustomerAccounts]', N'U') IS NOT NULL
                AND NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'IX_CustomerAccounts_Phone'
                      AND object_id = OBJECT_ID(N'[CustomerAccounts]')
                )
            BEGIN
                CREATE UNIQUE INDEX [IX_CustomerAccounts_Phone] ON [CustomerAccounts]([Phone]);
            END
            """);

        await context.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'[CustomerAccounts]', N'U') IS NOT NULL
                AND NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'IX_CustomerAccounts_Email'
                      AND object_id = OBJECT_ID(N'[CustomerAccounts]')
                )
            BEGIN
                CREATE UNIQUE INDEX [IX_CustomerAccounts_Email] ON [CustomerAccounts]([Email]);
            END
            """);

        await context.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'[CustomerAccounts]', N'U') IS NOT NULL
                AND NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'IX_CustomerAccounts_SessionToken'
                      AND object_id = OBJECT_ID(N'[CustomerAccounts]')
                )
            BEGIN
                CREATE INDEX [IX_CustomerAccounts_SessionToken] ON [CustomerAccounts]([SessionToken]);
            END
            """);
    }

    public static async Task SeedLanguagesAsync(AppDbContext context)
    {
        var supportedLanguages = new[]
        {
            new LanguageOption { Code = "vi-VN", Name = "Tiếng Việt", NativeName = "Tiếng Việt", Locale = "vi-VN", SortOrder = 1, IsActive = true },
            new LanguageOption { Code = "en-US", Name = "English", NativeName = "English", Locale = "en-US", SortOrder = 2, IsActive = true },
            new LanguageOption { Code = "zh-CN", Name = "Tiếng Trung", NativeName = "简体中文", Locale = "zh-CN", SortOrder = 3, IsActive = true }
        };
        var supportedCodes = supportedLanguages
            .Select(x => x.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        await DeleteLanguageOptionAsync(context, "ja-JP");
        var existingLanguages = await context.LanguageOptions.ToListAsync();

        foreach (var supported in supportedLanguages)
        {
            var existing = existingLanguages.FirstOrDefault(x => string.Equals(x.Code, supported.Code, StringComparison.OrdinalIgnoreCase));
            if (existing == null)
            {
                context.LanguageOptions.Add(new LanguageOption
                {
                    Code = supported.Code,
                    Name = supported.Name,
                    NativeName = supported.NativeName,
                    Locale = supported.Locale,
                    SortOrder = supported.SortOrder,
                    IsActive = true
                });
                continue;
            }

            existing.Name = supported.Name;
            existing.NativeName = supported.NativeName;
            existing.Locale = supported.Locale;
            existing.SortOrder = supported.SortOrder;
            existing.IsActive = true;
        }

        foreach (var extra in existingLanguages.Where(x => !supportedCodes.Contains(x.Code) && !string.Equals(x.Code, "ja-JP", StringComparison.OrdinalIgnoreCase)))
        {
            extra.IsActive = false;
        }

        if (context.ChangeTracker.HasChanges())
        {
            await context.SaveChangesAsync();
        }
    }

    public static async Task NormalizeUnsupportedLanguageReferencesAsync(AppDbContext context)
    {
        const string fallbackLanguage = "vi-VN";

        var visitors = await context.Visitors
            .Where(x => x.Language == "ja-JP")
            .ToListAsync();
        foreach (var visitor in visitors)
        {
            visitor.Language = fallbackLanguage;
        }

        var tours = await context.Tours
            .Where(x => x.Language == "ja-JP")
            .ToListAsync();
        foreach (var tour in tours)
        {
            tour.Language = fallbackLanguage;
        }

        var pois = await context.Pois
            .Where(x => x.DefaultLanguage == "ja-JP")
            .ToListAsync();
        foreach (var poi in pois)
        {
            poi.DefaultLanguage = fallbackLanguage;
        }

        var visitHistories = await context.VisitHistories
            .Where(x => x.Language == "ja-JP")
            .ToListAsync();
        foreach (var visit in visitHistories)
        {
            visit.Language = fallbackLanguage;
        }

        var geofenceTriggers = await context.GeofenceTriggers
            .Where(x => x.Language == "ja-JP")
            .ToListAsync();
        foreach (var trigger in geofenceTriggers)
        {
            trigger.Language = fallbackLanguage;
        }

        await DeletePoiTranslationsByLanguageAsync(context, "ja-JP");

        if (context.ChangeTracker.HasChanges())
        {
            await context.SaveChangesAsync();
        }
    }

    private static Task DeleteLanguageOptionAsync(AppDbContext context, string languageCode)
        => context.Database.IsSqlite()
            ? context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM \"LanguageOptions\" WHERE \"Code\" = {languageCode}")
            : context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [LanguageOptions] WHERE [Code] = {languageCode}");

    private static Task DeletePoiTranslationsByLanguageAsync(AppDbContext context, string languageCode)
        => context.Database.IsSqlite()
            ? context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM \"PoiTranslations\" WHERE \"Language\" = {languageCode}")
            : context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [PoiTranslations] WHERE [Language] = {languageCode}");

    public static async Task SeedRegistrationPlansAsync(AppDbContext context)
    {
        var seedPlans = new[]
        {
            new RegistrationPlan { Code = "starter-20k", Name = "Gói khởi động", Description = "Phù hợp để đăng ký nhanh và trải nghiệm đầy đủ nội dung cơ bản trên tuyến phố.", HighlightText = "Tối thiểu 20.000đ", Price = 20_000, DurationDays = 30, Currency = "VND", IsActive = true, SortOrder = 1 },
            new RegistrationPlan { Code = "explorer-50k", Name = "Gói khám phá", Description = "Dành cho khách muốn nghe trọn bộ nội dung và sử dụng dài hơn trong nhiều lần quay lại.", HighlightText = "Phổ biến", Price = 50_000, DurationDays = 90, Currency = "VND", IsActive = true, SortOrder = 2 },
            new RegistrationPlan { Code = "premium-100k", Name = "Gói đồng hành", Description = "Phù hợp cho doanh nghiệp hoặc khách muốn trải nghiệm lâu dài và ổn định.", HighlightText = "Giá trị cao", Price = 100_000, DurationDays = 365, Currency = "VND", IsActive = true, SortOrder = 3 }
        };

        var existingPlans = await context.RegistrationPlans.ToListAsync();
        foreach (var seed in seedPlans)
        {
            var existing = existingPlans.FirstOrDefault(x => string.Equals(x.Code, seed.Code, StringComparison.OrdinalIgnoreCase));
            if (existing == null)
            {
                context.RegistrationPlans.Add(seed);
                continue;
            }

            existing.Name = seed.Name;
            existing.Description = seed.Description;
            existing.HighlightText = seed.HighlightText;
            existing.Price = seed.Price;
            existing.DurationDays = seed.DurationDays;
            existing.Currency = seed.Currency;
            existing.SortOrder = seed.SortOrder;
            existing.IsActive = true;
        }

        foreach (var extra in existingPlans.Where(x => seedPlans.All(seed => !string.Equals(seed.Code, x.Code, StringComparison.OrdinalIgnoreCase))))
        {
            extra.IsActive = false;
        }

        if (context.ChangeTracker.HasChanges())
        {
            await context.SaveChangesAsync();
        }
    }

    public static async Task SeedCategoriesAsync(AppDbContext context)
    {
        if (await context.Categories.AnyAsync())
        {
            return;
        }

        context.Categories.AddRange(
            new Category { Slug = "food-street", Name = "Phố ẩm thực", Description = "Danh mục món ăn và cụm quán ăn.", ThemeColor = "#c97732", SortOrder = 1 },
            new Category { Slug = "history", Name = "Lịch sử địa phương", Description = "Các điểm kể chuyện lịch sử khu vực.", ThemeColor = "#17324d", SortOrder = 2 },
            new Category { Slug = "culture", Name = "Văn hóa - đời sống", Description = "Nét sinh hoạt và văn hóa phố Vĩnh Khánh.", ThemeColor = "#2a9d8f", SortOrder = 3 },
            new Category { Slug = "check-in", Name = "Check-in - trải nghiệm", Description = "Các điểm dừng chân và chụp ảnh.", ThemeColor = "#6d597a", SortOrder = 4 });

        await context.SaveChangesAsync();
    }

    public static async Task SeedDemoContentAsync(AppDbContext context)
    {
        if (await context.Pois.AnyAsync())
        {
            return;
        }

        var now = DateTime.UtcNow;
        var pois = new[]
        {
            new Poi
            {
                Name = "Phố ẩm thực Vĩnh Khánh",
                Category = "food-street",
                Summary = "Điểm bắt đầu phố ẩm thực nổi tiếng của Quận 4.",
                Description = "Khu phố này sáng đèn từ chiều tới đêm khuya, nổi bật với các quán ốc, món nướng và không khí ăn đêm rất sôi động.",
                Address = "Đường Vĩnh Khánh, Phường 8, Quận 4, TP.HCM",
                Latitude = 10.760950,
                Longitude = 106.704120,
                Radius = 45,
                ApproachRadiusMeters = 95,
                Priority = 10,
                DebounceSeconds = 15,
                CooldownSeconds = 120,
                TriggerMode = "both",
                MapUrl = "https://www.google.com/maps/search/?api=1&query=10.76095,106.70412",
                TtsScript = "Bạn đang đứng tại cửa ngõ phố ẩm thực Vĩnh Khánh. Đây là điểm hợp lý để bắt đầu tour và làm quen với không khí ăn đêm của Quận 4.",
                DefaultLanguage = "vi-VN",
                EstimatedDurationSeconds = 90,
                CreatedAt = now,
                UpdatedAt = now
            },
            new Poi
            {
                Name = "Cụm quán ốc Vĩnh Khánh",
                Category = "food-street",
                Summary = "Cụm quán ốc và món ăn đêm đông khách nhất trên tuyến phố.",
                Description = "Du khách thường dừng lại tại đây để thử ốc, hải sản, món nướng và nhiều phiên bản nước chấm đặc trưng của khu vực.",
                Address = "Giữa phố Vĩnh Khánh, Quận 4, TP.HCM",
                Latitude = 10.760620,
                Longitude = 106.703760,
                Radius = 35,
                ApproachRadiusMeters = 80,
                Priority = 9,
                DebounceSeconds = 12,
                CooldownSeconds = 120,
                TriggerMode = "both",
                MapUrl = "https://www.google.com/maps/search/?api=1&query=10.76062,106.70376",
                TtsScript = "Đây là cụm quán ốc tiêu biểu của Vĩnh Khánh. Nhịp phục vụ nhanh, bàn sát vỉa hè và mùi nướng tạo nên bản sắc rất riêng của khu phố này.",
                DefaultLanguage = "vi-VN",
                EstimatedDurationSeconds = 75,
                CreatedAt = now,
                UpdatedAt = now
            },
            new Poi
            {
                Name = "Trạm xe buýt Khánh Hội",
                Category = "check-in",
                Summary = "Điểm vào tour bằng QR cho visitor đến bằng xe buýt.",
                Description = "Khách có thể quét QR tại điểm này để nghe giới thiệu ngay mà không cần đợi GPS kích hoạt.",
                Address = "Khu vực Khánh Hội, Quận 4, TP.HCM",
                Latitude = 10.761480,
                Longitude = 106.703020,
                Radius = 25,
                ApproachRadiusMeters = 45,
                Priority = 6,
                DebounceSeconds = 10,
                CooldownSeconds = 90,
                TriggerMode = "manual",
                MapUrl = "https://www.google.com/maps/search/?api=1&query=10.76148,106.70302",
                TtsScript = "Trạm xe buýt Khánh Hội là điểm vào nhanh cho tour. Nếu bạn vừa xuống xe, hãy quét QR để nghe tổng quan và bắt đầu hành trình ngay lập tức.",
                DefaultLanguage = "vi-VN",
                EstimatedDurationSeconds = 50,
                CreatedAt = now,
                UpdatedAt = now
            },
            new Poi
            {
                Name = "Trạm xe buýt Vĩnh Hội",
                Category = "check-in",
                Summary = "Điểm dừng chân để vào hoặc kết thúc lộ trình tham quan.",
                Description = "Tại đây visitor có thể quét QR, nghe tóm tắt và chọn hướng tiếp tục đi bộ vào phố ẩm thực.",
                Address = "Khu vực Vĩnh Hội, Quận 4, TP.HCM",
                Latitude = 10.761980,
                Longitude = 106.704030,
                Radius = 25,
                ApproachRadiusMeters = 45,
                Priority = 6,
                DebounceSeconds = 10,
                CooldownSeconds = 90,
                TriggerMode = "manual",
                MapUrl = "https://www.google.com/maps/search/?api=1&query=10.76198,106.70403",
                TtsScript = "Trạm xe buýt Vĩnh Hội phù hợp làm điểm kết tour hoặc trung chuyển. Nội dung QR tại đây giúp visitor nghe nhanh mà không phụ thuộc vào vị trí GPS.",
                DefaultLanguage = "vi-VN",
                EstimatedDurationSeconds = 50,
                CreatedAt = now,
                UpdatedAt = now
            },
            new Poi
            {
                Name = "Trạm xe buýt Xuân Chiếu",
                Category = "check-in",
                Summary = "Điểm QR cho visitor tiếp cận từ hướng Xuân Chiếu - Xóm Chiếu.",
                Description = "Nội dung được kích hoạt bằng QR để visitor nghe ngay khi vừa đến khu vực bằng xe buýt.",
                Address = "Khu vực Xuân Chiếu - Xóm Chiếu, Quận 4, TP.HCM",
                Latitude = 10.762530,
                Longitude = 106.704820,
                Radius = 25,
                ApproachRadiusMeters = 45,
                Priority = 5,
                DebounceSeconds = 10,
                CooldownSeconds = 90,
                TriggerMode = "manual",
                MapUrl = "https://www.google.com/maps/search/?api=1&query=10.76253,106.70482",
                TtsScript = "Đây là điểm vào từ hướng Xuân Chiếu, còn gọi là Xóm Chiếu. QR tại đây giúp visitor vào nội dung nhanh và không cần đợi app bật geofence.",
                DefaultLanguage = "vi-VN",
                EstimatedDurationSeconds = 50,
                CreatedAt = now,
                UpdatedAt = now
            },
            new Poi
            {
                Name = "Nhịp sống khu vực Vĩnh Khánh",
                Category = "culture",
                Summary = "Điểm kể chuyện về không khí đường phố, sinh hoạt và nhịp sống về đêm.",
                Description = "Ngoài ẩm thực, khu vực này còn hấp dẫn nhờ sự nhộn nhịp của người bán, khách đi bộ và không gian sinh hoạt sát nhau trên vỉa hè.",
                Address = "Trục đường Vĩnh Khánh, Quận 4, TP.HCM",
                Latitude = 10.761120,
                Longitude = 106.703580,
                Radius = 30,
                ApproachRadiusMeters = 70,
                Priority = 7,
                DebounceSeconds = 12,
                CooldownSeconds = 120,
                TriggerMode = "nearby",
                MapUrl = "https://www.google.com/maps/search/?api=1&query=10.76112,106.70358",
                TtsScript = "Điểm này giúp visitor hiểu thêm về đời sống phố phường ở Quận 4. Không chỉ có món ăn, Vĩnh Khánh còn là nơi thể hiện nhịp sống và văn hóa giao tiếp rất riêng.",
                DefaultLanguage = "vi-VN",
                EstimatedDurationSeconds = 60,
                CreatedAt = now,
                UpdatedAt = now
            }
        };

        context.Pois.AddRange(pois);
        await context.SaveChangesAsync();

        var foodStreet = await context.Pois.FirstAsync(x => x.Name == "Phố ẩm thực Vĩnh Khánh");
        context.PoiTranslations.AddRange(
            new PoiTranslation
            {
                PoiId = foodStreet.Id,
                Language = "vi-VN",
                Title = "Phố ẩm thực Vĩnh Khánh",
                Summary = "Điểm bắt đầu phố ẩm thực nổi tiếng của Quận 4.",
                Description = "Khu phố này sáng đèn từ chiều tới đêm khuya, nổi bật với các quán ốc, món nướng và không khí ăn đêm rất sôi động.",
                TtsScript = "Bạn đang đứng tại cửa ngõ phố ẩm thực Vĩnh Khánh. Đây là điểm hợp lý để bắt đầu tour và làm quen với không khí ăn đêm của Quận 4.",
                IsPublished = true,
                UpdatedAt = now
            },
            new PoiTranslation
            {
                PoiId = foodStreet.Id,
                Language = "en-US",
                Title = "Vinh Khanh Food Street",
                Summary = "A lively entry point to District 4 night-food culture.",
                Description = "This street lights up from late afternoon to night with seafood, grilled dishes, and dense street-side dining.",
                TtsScript = "You are at the entrance of Vinh Khanh Food Street. This is a good starting point to get the overview before walking deeper into the food corridor.",
                IsPublished = true,
                UpdatedAt = now
            },
            new PoiTranslation
            {
                PoiId = foodStreet.Id,
                Language = "zh-CN",
                Title = "永庆美食街",
                Summary = "第四郡夜间美食街的入口。",
                Description = "这条街从傍晚到深夜都很热闹，汇集了海鲜、烧烤和充满烟火气的人行道用餐空间。",
                TtsScript = "您现在来到永庆美食街的入口。这里很适合作为行程的起点，先感受第四郡热闹的夜间餐饮氛围。",
                IsPublished = true,
                UpdatedAt = now
            });

        context.QRCodes.AddRange(
            new QRCode { PoiId = (await context.Pois.FirstAsync(x => x.Name == "Trạm xe buýt Khánh Hội")).Id, Code = "BUS-KH-001", Note = "Điểm dừng xe buýt phường Khánh Hội" },
            new QRCode { PoiId = (await context.Pois.FirstAsync(x => x.Name == "Trạm xe buýt Vĩnh Hội")).Id, Code = "BUS-VH-002", Note = "Điểm dừng xe buýt phường Vĩnh Hội" },
            new QRCode { PoiId = (await context.Pois.FirstAsync(x => x.Name == "Trạm xe buýt Xuân Chiếu")).Id, Code = "BUS-XC-003", Note = "Điểm dừng xe buýt phường Xuân Chiếu / Xóm Chiếu" });

        var tour = new Tour
        {
            Name = "Đêm Vĩnh Khánh 45 phút",
            Description = "Lộ trình demo đi bộ từ trạm xe buýt đến phố ẩm thực và các điểm nhịp sống khu Vĩnh Khánh.",
            Language = "vi-VN",
            EstimatedDurationMinutes = 45,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        context.Tours.Add(tour);
        await context.SaveChangesAsync();

        var stops = new[]
        {
            new { PoiName = "Trạm xe buýt Khánh Hội", SortOrder = 1, AutoPlay = false, Note = "Điểm vào tour bằng QR hoặc tự chọn." },
            new { PoiName = "Phố ẩm thực Vĩnh Khánh", SortOrder = 2, AutoPlay = true, Note = "Tổng quan về phố ẩm thực." },
            new { PoiName = "Cụm quán ốc Vĩnh Khánh", SortOrder = 3, AutoPlay = true, Note = "Giới thiệu cụm quán ốc và món ăn đêm." },
            new { PoiName = "Nhịp sống khu vực Vĩnh Khánh", SortOrder = 4, AutoPlay = true, Note = "Kể chuyện về không khí và nhịp sống về đêm." },
            new { PoiName = "Trạm xe buýt Vĩnh Hội", SortOrder = 5, AutoPlay = false, Note = "Điểm kết tour và định hướng di chuyển tiếp." }
        };

        foreach (var stop in stops)
        {
            var poi = await context.Pois.FirstAsync(x => x.Name == stop.PoiName);
            context.TourStops.Add(new TourStop
            {
                TourId = tour.Id,
                PoiId = poi.Id,
                SortOrder = stop.SortOrder,
                AutoPlay = stop.AutoPlay,
                Note = stop.Note
            });
        }

        await context.SaveChangesAsync();
    }

    public static async Task EnsureDemoTranslationCoverageAsync(AppDbContext context)
    {
        if (!await context.Pois.AnyAsync())
        {
            return;
        }

        var now = DateTime.UtcNow;
        var poiMap = await context.Pois
            .Where(x =>
                x.Name == "Phố ẩm thực Vĩnh Khánh" ||
                x.Name == "Cụm quán ốc Vĩnh Khánh" ||
                x.Name == "Trạm xe buýt Khánh Hội" ||
                x.Name == "Trạm xe buýt Vĩnh Hội" ||
                x.Name == "Trạm xe buýt Xuân Chiếu" ||
                x.Name == "Nhịp sống khu vực Vĩnh Khánh")
            .ToDictionaryAsync(x => x.Name, x => x);

        if (poiMap.Count == 0)
        {
            return;
        }

        var poiIds = poiMap.Values.Select(x => x.Id).ToList();
        var existingTranslations = await context.PoiTranslations
            .Where(x => poiIds.Contains(x.PoiId))
            .ToListAsync();
        var translationMap = existingTranslations.ToDictionary(
            x => $"{x.PoiId}:{x.Language}",
            x => x,
            StringComparer.OrdinalIgnoreCase);

        var definitions = new (string PoiName, string Language, string Title, string Summary, string Description, string TtsScript)[]
        {
            ("Phố ẩm thực Vĩnh Khánh", "vi-VN", "Phố ẩm thực Vĩnh Khánh", "Điểm bắt đầu phố ẩm thực nổi tiếng của Quận 4.", "Khu phố này sáng đèn từ chiều tới đêm khuya, nổi bật với các quán ốc, món nướng và không khí ăn đêm rất sôi động.", "Bạn đang đứng tại cửa ngõ phố ẩm thực Vĩnh Khánh. Đây là điểm hợp lý để bắt đầu tour và làm quen với không khí ăn đêm của Quận 4."),
            ("Phố ẩm thực Vĩnh Khánh", "en-US", "Vinh Khanh Food Street", "A lively entry point to District 4 night-food culture.", "This street lights up from late afternoon to night with seafood, grilled dishes, and dense street-side dining.", "You are at the entrance of Vinh Khanh Food Street. This is a good starting point to get the overview before walking deeper into the food corridor."),
            ("Phố ẩm thực Vĩnh Khánh", "zh-CN", "永庆美食街", "第四郡夜间美食街的入口。", "这条街从傍晚到深夜都很热闹，汇集了海鲜、烧烤和充满烟火气的人行道用餐空间。", "您现在来到永庆美食街的入口。这里很适合作为行程的起点，先感受第四郡热闹的夜间餐饮氛围。"),
            ("Cụm quán ốc Vĩnh Khánh", "vi-VN", "Cụm quán ốc Vĩnh Khánh", "Cụm quán ốc và món ăn đêm đông khách nhất trên tuyến phố.", "Du khách thường dừng lại tại đây để thử ốc, hải sản, món nướng và nhiều phiên bản nước chấm đặc trưng của khu vực.", "Đây là cụm quán ốc tiêu biểu của Vĩnh Khánh. Nhịp phục vụ nhanh, bàn sát vỉa hè và mùi nướng tạo nên bản sắc rất riêng của khu phố này."),
            ("Cụm quán ốc Vĩnh Khánh", "en-US", "Vinh Khanh Seafood Cluster", "One of the busiest late-night seafood stretches on the route.", "Visitors often stop here for snails, shellfish, grilled plates, and dipping sauces that define the street-food identity of the area.", "This seafood cluster represents the most energetic dining section of Vinh Khanh. It is where the smell of grilled dishes and the sidewalk atmosphere feel strongest."),
            ("Cụm quán ốc Vĩnh Khánh", "zh-CN", "永庆海鲜小吃区", "这里是路线中最热闹的夜间海鲜聚集区之一。", "游客常在这里停留，品尝螺类、贝类、烧烤和带有本地特色的蘸酱。", "这里是永庆街最具代表性的海鲜小吃区。烧烤的香气和路边用餐的氛围最能体现这条街的特色。"),
            ("Trạm xe buýt Khánh Hội", "vi-VN", "Trạm xe buýt Khánh Hội", "Điểm vào tour bằng QR cho visitor đến bằng xe buýt.", "Khách có thể quét QR tại điểm này để nghe giới thiệu ngay mà không cần đợi GPS kích hoạt.", "Trạm xe buýt Khánh Hội là điểm vào nhanh cho tour. Nếu bạn vừa xuống xe, hãy quét QR để nghe tổng quan và bắt đầu hành trình ngay lập tức."),
            ("Trạm xe buýt Khánh Hội", "en-US", "Khánh Hội Bus Stop", "A QR entry point for visitors arriving by bus.", "Visitors can scan the code here and start listening immediately without waiting for GPS activation.", "This bus stop is a fast entry point to the tour. Scan the QR code here if you want to begin listening right after getting off the bus."),
            ("Trạm xe buýt Khánh Hội", "zh-CN", "庆会公交站", "乘公交到达的游客可从这里扫码开始。", "游客可以在这里扫码并立即收听介绍，不必等待 GPS 触发。", "庆会公交站是一个快速进入导览的起点。如果您刚下车，可以直接扫码，马上开始收听介绍。"),
            ("Trạm xe buýt Vĩnh Hội", "vi-VN", "Trạm xe buýt Vĩnh Hội", "Điểm dừng chân để vào hoặc kết thúc lộ trình tham quan.", "Tại đây visitor có thể quét QR, nghe tóm tắt và chọn hướng tiếp tục đi bộ vào phố ẩm thực.", "Trạm xe buýt Vĩnh Hội phù hợp làm điểm kết tour hoặc trung chuyển. Nội dung QR tại đây giúp visitor nghe nhanh mà không phụ thuộc vào vị trí GPS."),
            ("Trạm xe buýt Vĩnh Hội", "en-US", "Vĩnh Hội Bus Stop", "A flexible stop for entering or ending the walking route.", "From here visitors can scan a QR code, hear a short summary, and decide whether to continue into the food street on foot.", "Vĩnh Hội Bus Stop works well as a transfer point or tour ending point. The QR content here lets visitors listen quickly without depending on GPS."),
            ("Trạm xe buýt Vĩnh Hội", "zh-CN", "永会公交站", "这里适合作为步行美食路线的进入点或结束点。", "游客可以在这里先扫码听一段简介，再决定是否继续步行进入美食街。", "永会公交站既适合作为中转点，也适合作为行程终点。这里的二维码内容能让游客快速开始收听。"),
            ("Trạm xe buýt Xuân Chiếu", "vi-VN", "Trạm xe buýt Xuân Chiếu", "Điểm QR cho visitor tiếp cận từ hướng Xuân Chiếu - Xóm Chiếu.", "Nội dung được kích hoạt bằng QR để visitor nghe ngay khi vừa đến khu vực bằng xe buýt.", "Đây là điểm vào từ hướng Xuân Chiếu, còn gọi là Xóm Chiếu. QR tại đây giúp visitor vào nội dung nhanh và không cần đợi app bật geofence."),
            ("Trạm xe buýt Xuân Chiếu", "en-US", "Xuân Chiếu Bus Stop", "A QR point for visitors entering from the Xuân Chiếu direction.", "The narration here is designed to start right away for visitors who reach the area by bus from the Xuân Chiếu - Xóm Chiếu side.", "This is the QR entry point from the Xuân Chiếu side, also known as Xóm Chiếu. It helps visitors access the content quickly without waiting for geofence activation."),
            ("Trạm xe buýt Xuân Chiếu", "zh-CN", "春照公交站", "从春照或旧称 Xóm Chiếu 方向到达的游客可在这里扫码。", "这里的讲解内容为从春照一侧到达的游客设计，方便一到就开始收听。", "这是从春照，也就是旧称 Xóm Chiếu 方向进入的二维码点。不用等待 geofence，也能快速听到内容。"),
            ("Nhịp sống khu vực Vĩnh Khánh", "vi-VN", "Nhịp sống khu vực Vĩnh Khánh", "Điểm kể chuyện về không khí đường phố, sinh hoạt và nhịp sống về đêm.", "Ngoài ẩm thực, khu vực này còn hấp dẫn nhờ sự nhộn nhịp của người bán, khách đi bộ và không gian sinh hoạt sát nhau trên vỉa hè.", "Điểm này giúp visitor hiểu thêm về đời sống phố phường ở Quận 4. Không chỉ có món ăn, Vĩnh Khánh còn là nơi thể hiện nhịp sống và văn hóa giao tiếp rất riêng."),
            ("Nhịp sống khu vực Vĩnh Khánh", "en-US", "Vinh Khanh Street Life", "A stop that explains the nighttime rhythm and social life of the neighborhood.", "Beyond food, this area is memorable for its busy sidewalks, close-knit street trading, and the flow of people through the evening.", "This stop helps visitors understand the local street rhythm of District 4. Vinh Khanh is not only about food, but also about how people gather, trade, and socialize at night."),
            ("Nhịp sống khu vực Vĩnh Khánh", "zh-CN", "永庆街区生活节奏", "这个点介绍这里夜晚街头的节奏与生活气息。", "除了美食，这里繁忙的人行道、临近的小摊和不断流动的人群，也让第四郡的夜晚更有魅力。", "这个点帮助游客了解第四郡的街头生活。永庆不仅是吃东西的地方，也是观察人们交流、做生意和感受夜晚气氛的空间。")
        };

        foreach (var definition in definitions)
        {
            if (!poiMap.TryGetValue(definition.PoiName, out var poi))
            {
                continue;
            }

            if (string.Equals(definition.Language, "vi-VN", StringComparison.OrdinalIgnoreCase))
            {
                poi.Summary = definition.Summary;
                poi.Description = definition.Description;
                poi.TtsScript = definition.TtsScript;
                poi.DefaultLanguage = "vi-VN";
                poi.UpdatedAt = now;
            }

            var key = $"{poi.Id}:{definition.Language}";
            if (!translationMap.TryGetValue(key, out var translation))
            {
                translation = new PoiTranslation
                {
                    PoiId = poi.Id,
                    Language = definition.Language
                };
                context.PoiTranslations.Add(translation);
                translationMap[key] = translation;
            }

            translation.Title = definition.Title;
            translation.Summary = definition.Summary;
            translation.Description = definition.Description;
            translation.TtsScript = definition.TtsScript;
            translation.IsPublished = true;
            translation.UpdatedAt = now;
        }

        if (context.ChangeTracker.HasChanges())
        {
            await context.SaveChangesAsync();
        }
    }

    public static async Task EnsureDemoMediaLinksAsync(AppDbContext context)
    {
        var now = DateTime.UtcNow;
        var poiImageMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Phố ẩm thực Vĩnh Khánh"] = "/images/poi-vinh-khanh-food-street.png",
            ["Cụm quán ốc Vĩnh Khánh"] = "/images/poi-vinh-khanh-seafood-cluster.png",
            ["Trạm xe buýt Khánh Hội"] = "/images/poi-khanh-hoi-bus-stop.png",
            ["Trạm xe buýt Vĩnh Hội"] = "/images/poi-vinh-hoi-bus-stop.png",
            ["Trạm xe buýt Xuân Chiếu"] = "/images/poi-xuan-chieu-bus-stop.png",
            ["Nhịp sống khu vực Vĩnh Khánh"] = "/images/poi-vinh-khanh-street-life.png"
        };
        var poiAudioMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Phố ẩm thực Vĩnh Khánh"] = "/audio/poi-vinh-khanh-food-street-vi.wav",
            ["Cụm quán ốc Vĩnh Khánh"] = "/audio/poi-vinh-khanh-seafood-cluster-vi.wav",
            ["Trạm xe buýt Khánh Hội"] = "/audio/poi-khanh-hoi-bus-stop-vi.wav",
            ["Trạm xe buýt Vĩnh Hội"] = "/audio/poi-vinh-hoi-bus-stop-vi.wav",
            ["Trạm xe buýt Xuân Chiếu"] = "/audio/poi-xuan-chieu-bus-stop-vi.wav",
            ["Nhịp sống khu vực Vĩnh Khánh"] = "/audio/poi-vinh-khanh-street-life-vi.wav"
        };

        var pois = await context.Pois
            .Where(x => poiImageMap.Keys.Contains(x.Name))
            .ToListAsync();

        foreach (var poi in pois)
        {
            var desiredImage = poiImageMap[poi.Name];
            if (!string.Equals(poi.ImageUrl, desiredImage, StringComparison.OrdinalIgnoreCase))
            {
                poi.ImageUrl = desiredImage;
                poi.UpdatedAt = now;
            }

            var desiredAudio = poiAudioMap[poi.Name];
            if (!string.Equals(poi.AudioUrl, desiredAudio, StringComparison.OrdinalIgnoreCase))
            {
                poi.AudioUrl = desiredAudio;
                poi.UpdatedAt = now;
            }

            if (!string.Equals(poi.AudioMode, "tts-fallback", StringComparison.OrdinalIgnoreCase))
            {
                poi.AudioMode = "tts-fallback";
                poi.UpdatedAt = now;
            }
        }

        var tour = await context.Tours.FirstOrDefaultAsync(x => x.Name == "Đêm Vĩnh Khánh 45 phút");
        if (tour != null && !string.Equals(tour.CoverImageUrl, "/images/tour-dem-vinh-khanh-45-phut.png", StringComparison.OrdinalIgnoreCase))
        {
            tour.CoverImageUrl = "/images/tour-dem-vinh-khanh-45-phut.png";
        }

        if (context.ChangeTracker.HasChanges())
        {
            await context.SaveChangesAsync();
        }
    }

    public static async Task EnsureDemoEnglishAudioLinksAsync(AppDbContext context)
    {
        var translatedAudioMaps = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Phố ẩm thực Vĩnh Khánh"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["vi-VN"] = "/audio/poi-vinh-khanh-food-street-vi.wav",
                ["en-US"] = "/audio/poi-vinh-khanh-food-street-en.wav"
            },
            ["Cụm quán ốc Vĩnh Khánh"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["vi-VN"] = "/audio/poi-vinh-khanh-seafood-cluster-vi.wav",
                ["en-US"] = "/audio/poi-vinh-khanh-seafood-cluster-en.wav"
            },
            ["Trạm xe buýt Khánh Hội"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["vi-VN"] = "/audio/poi-khanh-hoi-bus-stop-vi.wav",
                ["en-US"] = "/audio/poi-khanh-hoi-bus-stop-en.wav"
            },
            ["Trạm xe buýt Vĩnh Hội"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["vi-VN"] = "/audio/poi-vinh-hoi-bus-stop-vi.wav",
                ["en-US"] = "/audio/poi-vinh-hoi-bus-stop-en.wav"
            },
            ["Trạm xe buýt Xuân Chiếu"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["vi-VN"] = "/audio/poi-xuan-chieu-bus-stop-vi.wav",
                ["en-US"] = "/audio/poi-xuan-chieu-bus-stop-en.wav"
            },
            ["Nhịp sống khu vực Vĩnh Khánh"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["vi-VN"] = "/audio/poi-vinh-khanh-street-life-vi.wav",
                ["en-US"] = "/audio/poi-vinh-khanh-street-life-en.wav"
            }
        };

        var pois = await context.Pois
            .Where(x => translatedAudioMaps.Keys.Contains(x.Name))
            .Select(x => new { x.Id, x.Name })
            .ToListAsync();

        var poiIds = pois.Select(x => x.Id).ToList();
        if (poiIds.Count == 0)
        {
            return;
        }

        var translations = await context.PoiTranslations
            .Where(x => poiIds.Contains(x.PoiId) && (x.Language == "vi-VN" || x.Language == "en-US"))
            .ToListAsync();

        foreach (var translation in translations)
        {
            var poiName = pois.First(x => x.Id == translation.PoiId).Name;
            if (!translatedAudioMaps.TryGetValue(poiName, out var languageAudioMap) ||
                !languageAudioMap.TryGetValue(translation.Language, out var desiredAudio))
            {
                continue;
            }

            if (!string.Equals(translation.AudioUrl, desiredAudio, StringComparison.OrdinalIgnoreCase))
            {
                translation.AudioUrl = desiredAudio;
                translation.UpdatedAt = DateTime.UtcNow;
            }
        }

        if (context.ChangeTracker.HasChanges())
        {
            await context.SaveChangesAsync();
        }
    }
}
