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
            await SeedLanguagesAsync(context);
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
        await SeedLanguagesAsync(context);
        await SeedCategoriesAsync(context);
        await SeedDemoContentAsync(context);
        await EnsureDemoTranslationCoverageAsync(context);
        await EnsureDemoMediaLinksAsync(context);
        await EnsureDemoEnglishAudioLinksAsync(context);
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
                NativeName = "Tieng Viet",
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
                NativeName = "Zhongwen",
                Locale = "zh-CN",
                SortOrder = 3
            },
            new LanguageOption
            {
                Code = "ja-JP",
                Name = "Japanese",
                NativeName = "Nihongo",
                Locale = "ja-JP",
                SortOrder = 4
            });

        await context.SaveChangesAsync();
    }

    public static async Task SeedCategoriesAsync(AppDbContext context)
    {
        if (await context.Categories.AnyAsync())
        {
            return;
        }

        context.Categories.AddRange(
            new Category { Slug = "food-street", Name = "Pho am thuc", Description = "Danh muc mon an va cum quan an.", ThemeColor = "#c97732", SortOrder = 1 },
            new Category { Slug = "history", Name = "Lich su dia phuong", Description = "Cac diem ke chuyen lich su khu vuc.", ThemeColor = "#17324d", SortOrder = 2 },
            new Category { Slug = "culture", Name = "Van hoa - doi song", Description = "Net sinh hoat va van hoa pho Vinh Khanh.", ThemeColor = "#2a9d8f", SortOrder = 3 },
            new Category { Slug = "check-in", Name = "Check-in - trai nghiem", Description = "Cac diem dung chan va chup anh.", ThemeColor = "#6d597a", SortOrder = 4 });

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
                Name = "Pho am thuc Vinh Khanh",
                Category = "food-street",
                Summary = "Diem bat dau pho am thuc noi tieng cua Quan 4.",
                Description = "Khu pho nay sang den tu chieu toi den khuya, noi bat voi cac quan oc, mon nuong va khong khi an dem rat soi dong.",
                Address = "Duong Vinh Khanh, Phuong 8, Quan 4, TP.HCM",
                Latitude = 10.760950,
                Longitude = 106.704120,
                Radius = 45,
                ApproachRadiusMeters = 95,
                Priority = 10,
                DebounceSeconds = 15,
                CooldownSeconds = 120,
                TriggerMode = "both",
                MapUrl = "https://www.google.com/maps/search/?api=1&query=10.76095,106.70412",
                TtsScript = "Ban dang dung tai cua ngo pho am thuc Vinh Khanh. Day la diem hop ly de bat dau tour va lam quen voi khong khi an dem cua Quan 4.",
                DefaultLanguage = "vi-VN",
                EstimatedDurationSeconds = 90,
                CreatedAt = now,
                UpdatedAt = now
            },
            new Poi
            {
                Name = "Cum quan oc Vinh Khanh",
                Category = "food-street",
                Summary = "Cum quan oc va mon an dem dong khach nhat tren tuyen pho.",
                Description = "Du khach thuong dung lai tai day de thu oc, hai san, mon nuong va nhieu phien ban nuoc cham dac trung cua khu vuc.",
                Address = "Giua pho Vinh Khanh, Quan 4, TP.HCM",
                Latitude = 10.760620,
                Longitude = 106.703760,
                Radius = 35,
                ApproachRadiusMeters = 80,
                Priority = 9,
                DebounceSeconds = 12,
                CooldownSeconds = 120,
                TriggerMode = "both",
                MapUrl = "https://www.google.com/maps/search/?api=1&query=10.76062,106.70376",
                TtsScript = "Day la cum quan oc tieu bieu cua Vinh Khanh. Nhac do an nhanh, ban ngan sat via he va mui nuong tao nen ban sac rat rieng cua khu pho nay.",
                DefaultLanguage = "vi-VN",
                EstimatedDurationSeconds = 75,
                CreatedAt = now,
                UpdatedAt = now
            },
            new Poi
            {
                Name = "Tram xe buyt Khanh Hoi",
                Category = "check-in",
                Summary = "Diem vao tour bang QR cho visitor den bang xe buyt.",
                Description = "Khach co the quet QR tai diem nay de nghe gioi thieu ngay ma khong can doi GPS kich hoat.",
                Address = "Khu vuc Khanh Hoi, Quan 4, TP.HCM",
                Latitude = 10.761480,
                Longitude = 106.703020,
                Radius = 25,
                ApproachRadiusMeters = 45,
                Priority = 6,
                DebounceSeconds = 10,
                CooldownSeconds = 90,
                TriggerMode = "manual",
                MapUrl = "https://www.google.com/maps/search/?api=1&query=10.76148,106.70302",
                TtsScript = "Tram xe buyt Khanh Hoi la diem vao nhanh cho tour. Neu ban vua xuong xe, hay quet QR de nghe tong quan va bat dau hanh trinh ngay lap tuc.",
                DefaultLanguage = "vi-VN",
                EstimatedDurationSeconds = 50,
                CreatedAt = now,
                UpdatedAt = now
            },
            new Poi
            {
                Name = "Tram xe buyt Vinh Hoi",
                Category = "check-in",
                Summary = "Diem dung chan de vao hoac ket thuc lo trinh tham quan.",
                Description = "Tai day visitor co the quet QR, nghe tom tat va chon huong tiep tuc di bo vao pho am thuc.",
                Address = "Khu vuc Vinh Hoi, Quan 4, TP.HCM",
                Latitude = 10.761980,
                Longitude = 106.704030,
                Radius = 25,
                ApproachRadiusMeters = 45,
                Priority = 6,
                DebounceSeconds = 10,
                CooldownSeconds = 90,
                TriggerMode = "manual",
                MapUrl = "https://www.google.com/maps/search/?api=1&query=10.76198,106.70403",
                TtsScript = "Tram xe buyt Vinh Hoi phu hop lam diem ket tour hoac trung chuyen. Noi dung QR tai day giup visitor nghe nhanh ma khong phu thuoc vao vi tri GPS.",
                DefaultLanguage = "vi-VN",
                EstimatedDurationSeconds = 50,
                CreatedAt = now,
                UpdatedAt = now
            },
            new Poi
            {
                Name = "Tram xe buyt Xuan Chieu",
                Category = "check-in",
                Summary = "Diem QR cho visitor tiep can tu huong Xuan Chieu - Xom Chieu.",
                Description = "Noi dung duoc kich hoat bang QR de visitor nghe ngay khi vua den khu vuc bang xe buyt.",
                Address = "Khu vuc Xuan Chieu - Xom Chieu, Quan 4, TP.HCM",
                Latitude = 10.762530,
                Longitude = 106.704820,
                Radius = 25,
                ApproachRadiusMeters = 45,
                Priority = 5,
                DebounceSeconds = 10,
                CooldownSeconds = 90,
                TriggerMode = "manual",
                MapUrl = "https://www.google.com/maps/search/?api=1&query=10.76253,106.70482",
                TtsScript = "Day la diem vao tu huong Xuan Chieu, con goi la Xom Chieu. QR tai day giup visitor vao noi dung nhanh va khong can doi app bat geofence.",
                DefaultLanguage = "vi-VN",
                EstimatedDurationSeconds = 50,
                CreatedAt = now,
                UpdatedAt = now
            },
            new Poi
            {
                Name = "Nhip song khu vuc Vinh Khanh",
                Category = "culture",
                Summary = "Diem ke chuyen ve khong khi duong pho, sinh hoat va nhip song ve dem.",
                Description = "Ngoai am thuc, khu vuc nay con hap dan nho su nhon nhip cua nguoi ban, khach di bo va khong gian sinh hoat sat nhau tren via he.",
                Address = "Truc duong Vinh Khanh, Quan 4, TP.HCM",
                Latitude = 10.761120,
                Longitude = 106.703580,
                Radius = 30,
                ApproachRadiusMeters = 70,
                Priority = 7,
                DebounceSeconds = 12,
                CooldownSeconds = 120,
                TriggerMode = "nearby",
                MapUrl = "https://www.google.com/maps/search/?api=1&query=10.76112,106.70358",
                TtsScript = "Diem nay giup visitor hieu them ve doi song pho phuong o Quan 4. Khong chi co mon an, Vinh Khanh con la noi the hien nhip song va van hoa giao tiep rat rieng.",
                DefaultLanguage = "vi-VN",
                EstimatedDurationSeconds = 60,
                CreatedAt = now,
                UpdatedAt = now
            }
        };

        context.Pois.AddRange(pois);
        await context.SaveChangesAsync();

        var foodStreet = await context.Pois.FirstAsync(x => x.Name == "Pho am thuc Vinh Khanh");
        context.PoiTranslations.AddRange(
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
                Title = "Vinh Khanh Mei Shi Jie",
                Summary = "Qu 4 ye jian mei shi jie de ru kou.",
                Description = "Zhe tiao jie zai bang wan hou bian de re nao, ji zhong le hai xian, shao kao he jie bian yong can khong gian.",
                TtsScript = "Nin xianzai zai Vinh Khanh mei shi jie ru kou. Zheli shi kaishi zhe tiao yesheng canyin luxian de hao difang.",
                IsPublished = true,
                UpdatedAt = now
            },
            new PoiTranslation
            {
                PoiId = foodStreet.Id,
                Language = "ja-JP",
                Title = "Vinh Khanh Gurume Street",
                Summary = "4-ku no yoru no shokugai e no iriguchi desu.",
                Description = "Yuugata kara yoru ni kakete, seafood, grilled food, soshite rojou no shokubunka de kono toori wa totemo nigiyaka ni narimasu.",
                TtsScript = "Koko wa Vinh Khanh gurume street no iriguchi desu. Mazu wa koko de kuiki no funiki o tsukande kara aruite susumu no ga osusume desu.",
                IsPublished = true,
                UpdatedAt = now
            });

        context.QRCodes.AddRange(
            new QRCode { PoiId = (await context.Pois.FirstAsync(x => x.Name == "Tram xe buyt Khanh Hoi")).Id, Code = "BUS-KH-001", Note = "Diem dung xe buyt phuong Khanh Hoi" },
            new QRCode { PoiId = (await context.Pois.FirstAsync(x => x.Name == "Tram xe buyt Vinh Hoi")).Id, Code = "BUS-VH-002", Note = "Diem dung xe buyt phuong Vinh Hoi" },
            new QRCode { PoiId = (await context.Pois.FirstAsync(x => x.Name == "Tram xe buyt Xuan Chieu")).Id, Code = "BUS-XC-003", Note = "Diem dung xe buyt phuong Xuan Chieu / Xom Chieu" });

        var tour = new Tour
        {
            Name = "Dem Vinh Khanh 45 phut",
            Description = "Lo trinh demo di bo tu tram xe buyt den pho am thuc va cac diem nhip song khu Vinh Khanh.",
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
            new { PoiName = "Tram xe buyt Khanh Hoi", SortOrder = 1, AutoPlay = false, Note = "Diem vao tour bang QR hoac tu chon." },
            new { PoiName = "Pho am thuc Vinh Khanh", SortOrder = 2, AutoPlay = true, Note = "Tong quan ve pho am thuc." },
            new { PoiName = "Cum quan oc Vinh Khanh", SortOrder = 3, AutoPlay = true, Note = "Gioi thieu cum quan oc va mon an dem." },
            new { PoiName = "Nhip song khu vuc Vinh Khanh", SortOrder = 4, AutoPlay = true, Note = "Ke chuyen ve khong khi va nhan nhip ve dem." },
            new { PoiName = "Tram xe buyt Vinh Hoi", SortOrder = 5, AutoPlay = false, Note = "Diem ket tour va dinh huong di chuyen tiep." }
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
        var poiIds = await context.Pois
            .Where(x =>
                x.Name == "Pho am thuc Vinh Khanh" ||
                x.Name == "Cum quan oc Vinh Khanh" ||
                x.Name == "Tram xe buyt Khanh Hoi" ||
                x.Name == "Tram xe buyt Vinh Hoi" ||
                x.Name == "Tram xe buyt Xuan Chieu" ||
                x.Name == "Nhip song khu vuc Vinh Khanh")
            .ToDictionaryAsync(x => x.Name, x => x.Id);

        if (poiIds.Count == 0)
        {
            return;
        }

        var existingKeys = (await context.PoiTranslations
                .Where(x => poiIds.Values.Contains(x.PoiId))
                .Select(x => new { x.PoiId, x.Language })
                .ToListAsync())
            .Select(x => $"{x.PoiId}:{x.Language}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var definitions = new (string PoiName, string Language, string Title, string Summary, string Description, string TtsScript)[]
        {
            ("Pho am thuc Vinh Khanh", "en-US", "Vinh Khanh Food Street", "A lively entry point to District 4 night-food culture.", "This street lights up from late afternoon to night with seafood, grilled dishes, and dense street-side dining.", "You are at the entrance of Vinh Khanh Food Street. This is a good starting point to get the overview before walking deeper into the food corridor."),
            ("Pho am thuc Vinh Khanh", "zh-CN", "Vinh Khanh Mei Shi Jie", "Qu 4 ye jian mei shi jie de ru kou.", "Zhe tiao jie zai bang wan hou bian de re nao, ji zhong le hai xian, shao kao he jie bian yong can khong gian.", "Nin xianzai zai Vinh Khanh mei shi jie ru kou. Zheli shi kaishi zhe tiao yesheng canyin luxian de hao difang."),
            ("Pho am thuc Vinh Khanh", "ja-JP", "Vinh Khanh Gurume Street", "4-ku no yoru no shokugai e no iriguchi desu.", "Yuugata kara yoru ni kakete, seafood, grilled food, soshite rojou no shokubunka de kono toori wa totemo nigiyaka ni narimasu.", "Koko wa Vinh Khanh gurume street no iriguchi desu. Mazu wa koko de kuiki no funiki o tsukande kara aruite susumu no ga osusume desu."),
            ("Cum quan oc Vinh Khanh", "en-US", "Vinh Khanh Seafood Cluster", "One of the busiest late-night seafood stretches on the route.", "Visitors often stop here for snails, shellfish, grilled plates, and dipping sauces that define the street-food identity of the area.", "This seafood cluster represents the most energetic dining section of Vinh Khanh. It is where the smell of grilled dishes and the sidewalk atmosphere feel strongest."),
            ("Cum quan oc Vinh Khanh", "zh-CN", "Vinh Khanh Hai Xian Qu", "Zhe shi zhe tiao jie zui re nao de yejian hai xian qu.", "Youke chang chang zai zheli ting liu, pinchang luo, bei lei, shao kao he dai you difang tese de jiangliao.", "Zheli shi Vinh Khanh zui ju daibiao xing de hai xian qu. Shaokao xiangqi he lu bian canyin de fenwei tebie qianglie."),
            ("Cum quan oc Vinh Khanh", "ja-JP", "Vinh Khanh Seafood Zone", "Kono toori de mottomo nigiwai no aru yoru no kaisen eria desu.", "Kankoukyaku wa koko de tamemono, kai, yakimono, soshite chiiki rashii sauce o tanoshimu koto ga dekimasu.", "Koko wa Vinh Khanh no kaisen eria no chuushin desu. Yakimono no kaori to rojou no kuuki ga kono machi rashisa o tsukutteimasu."),
            ("Tram xe buyt Khanh Hoi", "en-US", "Khanh Hoi Bus Stop", "A QR entry point for visitors arriving by bus.", "Visitors can scan the code here and start listening immediately without waiting for GPS activation.", "This bus stop is a fast entry point to the tour. Scan the QR code here if you want to begin listening right after getting off the bus."),
            ("Tram xe buyt Khanh Hoi", "zh-CN", "Khanh Hoi Gong Che Zhan", "Da ba shi daoda de youke keyi cong zheli saoma kaishi.", "Youke keyi zai zheli saoma bing liji shiting, er bu xu dengdai GPS chufa.", "Khanh Hoi gongchezhan shi yi ge kuaisu jinru dian. Ruguo nin gang xiache, keyi zhijie saoma kaishi ting jie shao."),
            ("Tram xe buyt Khanh Hoi", "ja-JP", "Khanh Hoi Bus Stop", "Basu de kita hito no tame no QR start point desu.", "Koko de QR o yomeba, GPS no handou o matazu ni sugu ni annai o kiku koto ga dekimasu.", "Koko wa Khanh Hoi no basutei desu. Basu o orita ato, sugu ni QR de tour o hajimeru no ni muiteimasu."),
            ("Tram xe buyt Vinh Hoi", "en-US", "Vinh Hoi Bus Stop", "A flexible stop for entering or ending the walking route.", "From here visitors can scan a QR code, hear a short summary, and decide whether to continue into the food street on foot.", "Vinh Hoi Bus Stop works well as a transfer point or tour ending point. The QR content here lets visitors listen quickly without depending on GPS."),
            ("Tram xe buyt Vinh Hoi", "zh-CN", "Vinh Hoi Gong Che Zhan", "Zheli shi jinru huo jieshu canyin bushixingcheng de linghuo zhandian.", "Youke keyi zai zheli saoma, xian ting yi duan jianjie, zai jueding shifou jixu zou jin mei shijie.", "Vinh Hoi gongchezhan keyi zuowei zhuancheng dian huo jieshu dian. Zheli de QR neirong neng rang youke kuaisu shiting."),
            ("Tram xe buyt Vinh Hoi", "ja-JP", "Vinh Hoi Bus Stop", "Aruki route no hajimari ni mo owari ni mo tsukaeru basutei desu.", "Koko de QR o yonde mijikai setsumei o kiite kara, aruite susumu ka douka o erabu koto ga dekimasu.", "Vinh Hoi no basutei wa tour no shuuten ni mo chuukei ni mo muki masu. QR o tsukatte sugu ni naiyou o kaku nin dekimasu."),
            ("Tram xe buyt Xuan Chieu", "en-US", "Xuan Chieu Bus Stop", "A QR point for visitors entering from the Xuan Chieu direction.", "The narration here is designed to start right away for visitors who reach the area by bus from the Xuan Chieu - Xom Chieu side.", "This is the QR entry point from the Xuan Chieu side, also known as Xom Chieu. It helps visitors access the content quickly without waiting for geofence activation."),
            ("Tram xe buyt Xuan Chieu", "zh-CN", "Xuan Chieu Gong Che Zhan", "Cong Xuan Chieu fangxiang dao da de youke keyi zai zheli saoma.", "Zheli de neirong sheji gei cong Xuan Chieu huo Xom Chieu yi ce daoda de youke, keyi liji kaishi shiting.", "Zhe shi cong Xuan Chieu huo Xom Chieu fangxiang jinru de QR dian. Ta keyi rang youke bu yong deng geofence ye neng kuaisu ting dao neirong."),
            ("Tram xe buyt Xuan Chieu", "ja-JP", "Xuan Chieu Bus Stop", "Xuan Chieu gawa kara hairu hito no tame no QR point desu.", "Koko no annai wa, Xuan Chieu ya Xom Chieu no houkou kara kuru hito ga sugu ni kikeru you ni settei sareteimasu.", "Koko wa Xuan Chieu gawa kara no nyuuryokuten desu. Geofence o matanai demo, QR de sugu ni annai naiyou ni haireru no ga tokuchou desu."),
            ("Nhip song khu vuc Vinh Khanh", "en-US", "Vinh Khanh Street Life", "A stop that explains the nighttime rhythm and social life of the neighborhood.", "Beyond food, this area is memorable for its busy sidewalks, close-knit street trading, and the flow of people through the evening.", "This stop helps visitors understand the local street rhythm of District 4. Vinh Khanh is not only about food, but also about how people gather, trade, and socialize at night."),
            ("Nhip song khu vuc Vinh Khanh", "zh-CN", "Vinh Khanh Jie Tou Sheng Huo", "Zhe ge dian jieshao de shi zheli de yejian jietou jiezhou he shenghuo qiwei.", "Chu le meishi, zhe li hai yin manglu de renxingdao, linjin de xiaotan he buduan liudong de renqun er rang ren jixu lianshang.", "Zhe ge dian bangzhu youke liaojie Qu 4 de jietou shenghuo. Vinh Khanh bu zhi shi chi de difang, ye shi yi ge neng kanjian renmen jiaoliu he yewan qifen de kongjian."),
            ("Nhip song khu vuc Vinh Khanh", "ja-JP", "Vinh Khanh Street Rhythm", "Kono point de wa, yoru no machi no rizumu to kurashi no kuuki o shoukai shimasu.", "Tabemono dake de naku, isogashii hodou, chikaku de eigyou suru mise, soshite yoru no hito no nagare ga kono chiiki no miryoku desu.", "Koko de wa Quan 4 no machi no seikatsu kan o rikaisuru koto ga dekimasu. Vinh Khanh wa tabemono dake de naku, yoru no kouryuu ga mienai tokoro made tsunagatteimasu.")
        };

        foreach (var definition in definitions)
        {
            if (!poiIds.TryGetValue(definition.PoiName, out var poiId))
            {
                continue;
            }

            var key = $"{poiId}:{definition.Language}";
            if (!existingKeys.Add(key))
            {
                continue;
            }

            context.PoiTranslations.Add(new PoiTranslation
            {
                PoiId = poiId,
                Language = definition.Language,
                Title = definition.Title,
                Summary = definition.Summary,
                Description = definition.Description,
                TtsScript = definition.TtsScript,
                IsPublished = true,
                UpdatedAt = now
            });
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
            ["Pho am thuc Vinh Khanh"] = "/images/poi-vinh-khanh-food-street.png",
            ["Cum quan oc Vinh Khanh"] = "/images/poi-vinh-khanh-seafood-cluster.png",
            ["Tram xe buyt Khanh Hoi"] = "/images/poi-khanh-hoi-bus-stop.png",
            ["Tram xe buyt Vinh Hoi"] = "/images/poi-vinh-hoi-bus-stop.png",
            ["Tram xe buyt Xuan Chieu"] = "/images/poi-xuan-chieu-bus-stop.png",
            ["Nhip song khu vuc Vinh Khanh"] = "/images/poi-vinh-khanh-street-life.png"
        };
        var poiAudioMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Pho am thuc Vinh Khanh"] = "/audio/poi-vinh-khanh-food-street-vi.wav",
            ["Cum quan oc Vinh Khanh"] = "/audio/poi-vinh-khanh-seafood-cluster-vi.wav",
            ["Tram xe buyt Khanh Hoi"] = "/audio/poi-khanh-hoi-bus-stop-vi.wav",
            ["Tram xe buyt Vinh Hoi"] = "/audio/poi-vinh-hoi-bus-stop-vi.wav",
            ["Tram xe buyt Xuan Chieu"] = "/audio/poi-xuan-chieu-bus-stop-vi.wav",
            ["Nhip song khu vuc Vinh Khanh"] = "/audio/poi-vinh-khanh-street-life-vi.wav"
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

            if (!string.Equals(poi.AudioMode, "audio-priority", StringComparison.OrdinalIgnoreCase))
            {
                poi.AudioMode = "audio-priority";
                poi.UpdatedAt = now;
            }
        }

        var tour = await context.Tours.FirstOrDefaultAsync(x => x.Name == "Dem Vinh Khanh 45 phut");
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
        var poiAudioMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Pho am thuc Vinh Khanh"] = "/audio/poi-vinh-khanh-food-street-en.wav",
            ["Cum quan oc Vinh Khanh"] = "/audio/poi-vinh-khanh-seafood-cluster-en.wav",
            ["Tram xe buyt Khanh Hoi"] = "/audio/poi-khanh-hoi-bus-stop-en.wav",
            ["Tram xe buyt Vinh Hoi"] = "/audio/poi-vinh-hoi-bus-stop-en.wav",
            ["Tram xe buyt Xuan Chieu"] = "/audio/poi-xuan-chieu-bus-stop-en.wav",
            ["Nhip song khu vuc Vinh Khanh"] = "/audio/poi-vinh-khanh-street-life-en.wav"
        };

        var pois = await context.Pois
            .Where(x => poiAudioMap.Keys.Contains(x.Name))
            .Select(x => new { x.Id, x.Name })
            .ToListAsync();

        var poiIds = pois.Select(x => x.Id).ToList();
        if (poiIds.Count == 0)
        {
            return;
        }

        var translations = await context.PoiTranslations
            .Where(x => x.Language == "en-US" && poiIds.Contains(x.PoiId))
            .ToListAsync();

        foreach (var translation in translations)
        {
            var poiName = pois.First(x => x.Id == translation.PoiId).Name;
            var desiredAudio = poiAudioMap[poiName];
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
