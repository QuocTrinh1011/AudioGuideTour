using AudioGuideAdmin.Models;
using Microsoft.EntityFrameworkCore;

namespace AudioGuideAdmin.Data;

public static class AppDataInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await EnsureCategoryTableAsync(context);
        await EnsureLanguageTableAsync(context);
        await SeedCategoriesAsync(context);
        await SeedLanguagesAsync(context);
        await SeedSampleDataAsync(context);
        await SeedAnalyticsDataAsync(context);
    }

    public static async Task SeedAllDemoDataAsync(AppDbContext context)
    {
        await EnsureCategoryTableAsync(context);
        await EnsureLanguageTableAsync(context);
        await SeedCategoriesAsync(context);
        await SeedLanguagesAsync(context);
        await SeedSampleDataAsync(context);
        await SeedAnalyticsDataAsync(context);
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

    public static async Task SeedCategoriesAsync(AppDbContext context)
    {
        if (await context.Categories.AnyAsync())
        {
            return;
        }

        context.Categories.AddRange(
            new Category
            {
                Slug = "food-street",
                Name = "Pho am thuc",
                Description = "Cac diem an uong, mon dac trung va khu pho nhon nhip.",
                ThemeColor = "#c97732",
                SortOrder = 1
            },
            new Category
            {
                Slug = "history",
                Name = "Lich su dia phuong",
                Description = "Noi dung ke chuyen lich su, hinh thanh va dau moc cua khu vuc.",
                ThemeColor = "#17324d",
                SortOrder = 2
            },
            new Category
            {
                Slug = "culture",
                Name = "Van hoa - doi song",
                Description = "Cac diem noi bat ve sinh hoat, phong cach va net van hoa Vinh Khanh.",
                ThemeColor = "#2a9d8f",
                SortOrder = 3
            },
            new Category
            {
                Slug = "check-in",
                Name = "Check-in - trai nghiem",
                Description = "Cac diem de dung chan, chup anh, trai nghiem khong gian.",
                ThemeColor = "#6d597a",
                SortOrder = 4
            });

        await context.SaveChangesAsync();
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

    public static async Task SeedSampleDataAsync(AppDbContext context)
    {
        var poiSeeds = new List<Poi>
        {
            new()
            {
                Name = "Cong chao pho am thuc Vinh Khanh",
                Category = "food-street",
                Summary = "Diem bat dau gioi thieu tong quan khu pho am thuc.",
                Description = "Khu pho am thuc Vinh Khanh noi tieng voi khong khi nhon nhip ve dem, tap trung nhieu quan an, mon nuong va hai san hap dan.",
                Address = "Duong Vinh Khanh, Quan 4, TP.HCM",
                Latitude = 10.757521,
                Longitude = 106.703417,
                Radius = 35,
                ApproachRadiusMeters = 90,
                Priority = 10,
                TriggerMode = "both",
                ImageUrl = "https://images.unsplash.com/photo-1517248135467-4c7edcad34c4?auto=format&fit=crop&w=1200&q=80",
                MapUrl = "https://maps.google.com/?q=10.757521,106.703417",
                TtsScript = "Chao mung ban den voi pho am thuc Vinh Khanh. Day la diem bat dau ly tuong de kham pha nhung mon an dac sac va khong khi soi dong cua khu vuc nay.",
                DefaultLanguage = "vi-VN",
                EstimatedDurationSeconds = 45,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true
            },
            new()
            {
                Name = "Cum quan oc va hai san",
                Category = "food-street",
                Summary = "Khu vuc nhon nhip nhat ve cac mon oc, hai san va mon nuong.",
                Description = "Day la noi du khach thuong dung lai lau nhat de trai nghiem mon oc xao, nuong, hap va cac mon hai san tuoi song.",
                Address = "Gan nga ba Vinh Khanh - Ton Dan",
                Latitude = 10.759114,
                Longitude = 106.701694,
                Radius = 35,
                ApproachRadiusMeters = 85,
                Priority = 9,
                TriggerMode = "nearby",
                ImageUrl = "https://images.unsplash.com/photo-1559847844-5315695dadae?auto=format&fit=crop&w=1200&q=80",
                MapUrl = "https://maps.google.com/?q=10.759114,106.701694",
                TtsScript = "Ban dang den gan cum quan oc va hai san, diem nhan am thuc noi bat cua Vinh Khanh. Mui thom mon nuong va am thanh goi mon tao nen khong khi rat dac trung.",
                DefaultLanguage = "vi-VN",
                EstimatedDurationSeconds = 50,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true
            },
            new()
            {
                Name = "Goc check-in dem Vinh Khanh",
                Category = "check-in",
                Summary = "Goc dung chan nhin toan canh pho am thuc luc len den.",
                Description = "Diem nay phu hop de chup anh, quan sat dong nguoi va ghi lai khong khi dem dac trung cua khu pho.",
                Address = "Doan giua pho Vinh Khanh",
                Latitude = 10.758209,
                Longitude = 106.702604,
                Radius = 30,
                ApproachRadiusMeters = 70,
                Priority = 7,
                TriggerMode = "enter",
                ImageUrl = "https://images.unsplash.com/photo-1504674900247-0877df9cc836?auto=format&fit=crop&w=1200&q=80",
                MapUrl = "https://maps.google.com/?q=10.758209,106.702604",
                TtsScript = "Ban dang dung tai mot goc check in dep cua pho Vinh Khanh. Tu day co the cam nhan ro nhat su soi dong cua duong pho va anh den bien hieu lung linh vao buoi toi.",
                DefaultLanguage = "vi-VN",
                EstimatedDurationSeconds = 35,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true
            }
        };

        var existingPois = await context.Pois.ToListAsync();
        var createdPois = new List<Poi>();

        foreach (var seed in poiSeeds)
        {
            var existing = existingPois.FirstOrDefault(x => x.Name == seed.Name);
            if (existing != null)
            {
                createdPois.Add(existing);
                continue;
            }

            context.Pois.Add(seed);
            createdPois.Add(seed);
        }

        await context.SaveChangesAsync();

        var pois = createdPois;

        var translationSeeds = new List<PoiTranslation>
        {
            new()
            {
                PoiId = pois[0].Id,
                Language = "vi-VN",
                Title = "Cong chao pho am thuc Vinh Khanh",
                Summary = "Tong quan khu pho am thuc.",
                Description = "Noi gioi thieu tong quan va dinh huong hanh trinh kham pha pho am thuc Vinh Khanh.",
                TtsScript = pois[0].TtsScript,
                VoiceName = "vi-VN",
                IsPublished = true,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                PoiId = pois[0].Id,
                Language = "en-US",
                Title = "Vinh Khanh food street gateway",
                Summary = "An overview of the lively food street.",
                Description = "This is the starting point where visitors are introduced to the atmosphere and specialties of Vinh Khanh food street.",
                TtsScript = "Welcome to Vinh Khanh food street. This is a great starting point to explore the local dishes, seafood stalls, and the lively evening atmosphere.",
                VoiceName = "en-US",
                IsPublished = true,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                PoiId = pois[1].Id,
                Language = "vi-VN",
                Title = "Cum quan oc va hai san",
                Summary = "Diem nong nhat ve cac mon oc va mon nuong.",
                Description = "Noi thu hut nhieu du khach nho thuc don phong phu va khong khi luc nao cung soi dong.",
                TtsScript = pois[1].TtsScript,
                VoiceName = "vi-VN",
                IsPublished = true,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                PoiId = pois[2].Id,
                Language = "vi-VN",
                Title = "Goc check-in dem Vinh Khanh",
                Summary = "Diem chup anh, nghi chan va nhin pho ve dem.",
                Description = "Mot diem dung chan nho de tan huong khong khi pho am thuc va ghi lai nhung khoanh khac dep.",
                TtsScript = pois[2].TtsScript,
                VoiceName = "vi-VN",
                IsPublished = true,
                UpdatedAt = DateTime.UtcNow
            }
        };

        foreach (var seed in translationSeeds)
        {
            var exists = await context.PoiTranslations.AnyAsync(x => x.PoiId == seed.PoiId && x.Language == seed.Language);
            if (!exists)
            {
                context.PoiTranslations.Add(seed);
            }
        }

        await context.SaveChangesAsync();

        var tour = await context.Tours.FirstOrDefaultAsync(x => x.Name == "Tour mau pho am thuc Vinh Khanh");
        if (tour == null)
        {
            tour = new Tour
            {
                Name = "Tour mau pho am thuc Vinh Khanh",
                Description = "Lo trinh ngan gioi thieu tong quan, khu hai san va diem check-in.",
                Language = "vi-VN",
                CoverImageUrl = "https://images.unsplash.com/photo-1517248135467-4c7edcad34c4?auto=format&fit=crop&w=1200&q=80",
                IsActive = true,
                EstimatedDurationMinutes = 20,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Tours.Add(tour);
            await context.SaveChangesAsync();
        }

        var stopSeeds = new List<TourStop>
        {
            new() { TourId = tour.Id, PoiId = pois[0].Id, SortOrder = 1, AutoPlay = true, Note = "Diem bat dau" },
            new() { TourId = tour.Id, PoiId = pois[1].Id, SortOrder = 2, AutoPlay = true, Note = "Cum am thuc noi bat" },
            new() { TourId = tour.Id, PoiId = pois[2].Id, SortOrder = 3, AutoPlay = true, Note = "Ket tour va check-in" }
        };

        foreach (var seed in stopSeeds)
        {
            var exists = await context.TourStops.AnyAsync(x => x.TourId == seed.TourId && x.PoiId == seed.PoiId && x.SortOrder == seed.SortOrder);
            if (!exists)
            {
                context.TourStops.Add(seed);
            }
        }

        await context.SaveChangesAsync();
    }

    public static async Task SeedAnalyticsDataAsync(AppDbContext context)
    {
        if (!await context.Pois.AnyAsync())
        {
            return;
        }

        var pois = await context.Pois.OrderBy(x => x.Id).Take(3).ToListAsync();
        if (pois.Count == 0)
        {
            return;
        }

        var demoUsers = new[]
        {
            "demo-visitor-001",
            "demo-visitor-002",
            "demo-visitor-003"
        };

        if (await context.UserTrackings.AnyAsync(x => demoUsers.Contains(x.UserId)))
        {
            return;
        }

        var now = DateTime.UtcNow;
        var baseRoute = new[]
        {
            new { Lat = 10.757420, Lng = 106.703180 },
            new { Lat = 10.757780, Lng = 106.702940 },
            new { Lat = 10.758240, Lng = 106.702510 },
            new { Lat = 10.758760, Lng = 106.702050 },
            new { Lat = 10.759080, Lng = 106.701760 }
        };

        for (var u = 0; u < demoUsers.Length; u++)
        {
            for (var i = 0; i < baseRoute.Length; i++)
            {
                context.UserTrackings.Add(new UserTracking
                {
                    UserId = demoUsers[u],
                    Latitude = baseRoute[i].Lat + (u * 0.00018),
                    Longitude = baseRoute[i].Lng - (u * 0.00014),
                    Accuracy = 8 + u,
                    SpeedMetersPerSecond = 0.9 + (u * 0.2),
                    Bearing = 110 + (i * 5),
                    Source = "gps",
                    IsForeground = i < 3,
                    RecordedAt = now.AddMinutes(-(30 - (u * 4) - i))
                });
            }
        }

        context.VisitHistories.AddRange(
            new VisitHistory
            {
                UserId = demoUsers[0],
                PoiId = pois[0].Id,
                Language = "vi-VN",
                StartTime = now.AddMinutes(-34),
                EndTime = now.AddMinutes(-33),
                Duration = 48,
                TriggerType = "enter",
                PlaybackMode = "tts",
                WasAutoPlayed = true,
                WasCompleted = true,
                ActivationDistanceMeters = 18.5
            },
            new VisitHistory
            {
                UserId = demoUsers[1],
                PoiId = pois[Math.Min(1, pois.Count - 1)].Id,
                Language = "vi-VN",
                StartTime = now.AddMinutes(-27),
                EndTime = now.AddMinutes(-26),
                Duration = 52,
                TriggerType = "nearby",
                PlaybackMode = "tts",
                WasAutoPlayed = true,
                WasCompleted = true,
                ActivationDistanceMeters = 24.2
            },
            new VisitHistory
            {
                UserId = demoUsers[2],
                PoiId = pois[Math.Min(2, pois.Count - 1)].Id,
                Language = "en-US",
                StartTime = now.AddMinutes(-21),
                EndTime = now.AddMinutes(-20),
                Duration = 38,
                TriggerType = "manual",
                PlaybackMode = "tts",
                WasAutoPlayed = false,
                WasCompleted = true,
                ActivationDistanceMeters = 12.4
            },
            new VisitHistory
            {
                UserId = demoUsers[0],
                PoiId = pois[Math.Min(1, pois.Count - 1)].Id,
                Language = "vi-VN",
                StartTime = now.AddMinutes(-14),
                EndTime = now.AddMinutes(-13),
                Duration = 44,
                TriggerType = "nearby",
                PlaybackMode = "tts",
                WasAutoPlayed = true,
                WasCompleted = false,
                ActivationDistanceMeters = 29.1
            });

        context.GeofenceTriggers.AddRange(
            new GeofenceTrigger
            {
                UserId = demoUsers[0],
                PoiId = pois[0].Id,
                Language = "vi-VN",
                TriggerType = "enter",
                Status = "triggered",
                DistanceMeters = 18.5,
                RecordedAt = now.AddMinutes(-33),
                CooldownUntil = now.AddMinutes(-31)
            },
            new GeofenceTrigger
            {
                UserId = demoUsers[1],
                PoiId = pois[Math.Min(1, pois.Count - 1)].Id,
                Language = "vi-VN",
                TriggerType = "nearby",
                Status = "triggered",
                DistanceMeters = 24.2,
                RecordedAt = now.AddMinutes(-26),
                CooldownUntil = now.AddMinutes(-24)
            },
            new GeofenceTrigger
            {
                UserId = demoUsers[0],
                PoiId = pois[Math.Min(1, pois.Count - 1)].Id,
                Language = "vi-VN",
                TriggerType = "nearby",
                Status = "cooldown",
                DistanceMeters = 29.1,
                RecordedAt = now.AddMinutes(-13),
                CooldownUntil = now.AddMinutes(-11)
            });

        await context.SaveChangesAsync();
    }
}
