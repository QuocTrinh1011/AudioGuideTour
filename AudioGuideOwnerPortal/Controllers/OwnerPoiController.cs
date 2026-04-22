using AudioGuideOwnerPortal.Data;
using AudioGuideOwnerPortal.Helpers;
using AudioGuideOwnerPortal.Models;
using AudioGuideOwnerPortal.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AudioGuideOwnerPortal.Controllers;

public class OwnerPoiController : Controller
{
    private sealed class PoiTemplateTranslationPreset
    {
        public string Language { get; init; } = "vi-VN";
        public string Title { get; init; } = "";
        public string Summary { get; init; } = "";
        public string Description { get; init; } = "";
        public string TtsScript { get; init; } = "";
        public string AudioUrl { get; init; } = "";
        public string VoiceName { get; init; } = "";
    }

    private sealed class PoiTemplatePreset
    {
        public string Key { get; init; } = "";
        public string Title { get; init; } = "";
        public string Summary { get; init; } = "";
        public string Category { get; init; } = "";
        public string Address { get; init; } = "";
        public double Latitude { get; init; }
        public double Longitude { get; init; }
        public int Radius { get; init; }
        public int ApproachRadiusMeters { get; init; }
        public int Priority { get; init; }
        public int DebounceSeconds { get; init; }
        public int CooldownSeconds { get; init; }
        public string TriggerMode { get; init; } = "both";
        public string MapUrl { get; init; } = "";
        public string Description { get; init; } = "";
        public string TtsScript { get; init; } = "";
        public string DefaultLanguage { get; init; } = "vi-VN";
        public int EstimatedDurationSeconds { get; init; }
        public string ImageUrl { get; init; } = "";
        public string AudioMode { get; init; } = "tts";
        public string AudioUrl { get; init; } = "";
        public IReadOnlyList<PoiTemplateTranslationPreset> Translations { get; init; } = Array.Empty<PoiTemplateTranslationPreset>();
    }

    private static readonly IReadOnlyList<PoiTemplatePreset> CreatePresets =
    [
        new PoiTemplatePreset
        {
            Key = "bus-khanh-hoi",
            Title = "Trạm xe buýt Khánh Hội",
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
            ImageUrl = "/images/poi-khanh-hoi-bus-stop.png",
            AudioMode = "tts-fallback",
            AudioUrl = "/audio/poi-khanh-hoi-bus-stop-vi.wav",
            Translations =
            [
                new PoiTemplateTranslationPreset
                {
                    Language = "vi-VN",
                    Title = "Trạm xe buýt Khánh Hội",
                    Summary = "Điểm vào tour bằng QR cho visitor đến bằng xe buýt.",
                    Description = "Khách có thể quét QR tại điểm này để nghe giới thiệu ngay mà không cần đợi GPS kích hoạt.",
                    TtsScript = "Trạm xe buýt Khánh Hội là điểm vào nhanh cho tour. Nếu bạn vừa xuống xe, hãy quét QR để nghe tổng quan và bắt đầu hành trình ngay lập tức.",
                    AudioUrl = "/audio/poi-khanh-hoi-bus-stop-vi.wav"
                },
                new PoiTemplateTranslationPreset
                {
                    Language = "en-US",
                    Title = "Khanh Hoi Bus Stop",
                    Summary = "A QR entry point for visitors arriving by bus.",
                    Description = "Visitors can scan the code here and start listening immediately without waiting for GPS activation.",
                    TtsScript = "This bus stop is a fast entry point to the tour. Scan the QR code here if you want to begin listening right after getting off the bus.",
                    AudioUrl = "/audio/poi-khanh-hoi-bus-stop-en.wav"
                },
                new PoiTemplateTranslationPreset
                {
                    Language = "zh-CN",
                    Title = "庆会公交站",
                    Summary = "乘公交到达的游客可从这里扫码开始。",
                    Description = "游客可以在这里扫码并立即收听介绍，不必等待 GPS 触发。",
                    TtsScript = "庆会公交站是一个快速进入导览的起点。如果您刚下车，可以直接扫码，马上开始收听介绍。"
                }
            ]
        },
        new PoiTemplatePreset
        {
            Key = "food-street",
            Title = "Phố ẩm thực Vĩnh Khánh",
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
            ImageUrl = "/images/poi-vinh-khanh-food-street.png",
            AudioMode = "tts-fallback",
            AudioUrl = "",
            Translations =
            [
                new PoiTemplateTranslationPreset
                {
                    Language = "vi-VN",
                    Title = "Phố ẩm thực Vĩnh Khánh",
                    Summary = "Điểm bắt đầu phố ẩm thực nổi tiếng của Quận 4.",
                    Description = "Khu phố này sáng đèn từ chiều tới đêm khuya, nổi bật với các quán ốc, món nướng và không khí ăn đêm rất sôi động.",
                    TtsScript = "Bạn đang đứng tại cửa ngõ phố ẩm thực Vĩnh Khánh. Đây là điểm hợp lý để bắt đầu tour và làm quen với không khí ăn đêm của Quận 4."
                },
                new PoiTemplateTranslationPreset
                {
                    Language = "en-US",
                    Title = "Vinh Khanh Food Street",
                    Summary = "A lively entry point to District 4 night-food culture.",
                    Description = "This street lights up from late afternoon to night with seafood, grilled dishes, and dense street-side dining.",
                    TtsScript = "You are at the entrance of Vinh Khanh Food Street. This is a good starting point to get the overview before walking deeper into the food corridor.",
                    AudioUrl = "/audio/poi-vinh-khanh-food-street-en.wav"
                },
                new PoiTemplateTranslationPreset
                {
                    Language = "zh-CN",
                    Title = "永庆美食街",
                    Summary = "第四郡夜间美食街的入口。",
                    Description = "这条街从傍晚到深夜都很热闹，汇集了海鲜、烧烤和充满烟火气的人行道用餐空间。",
                    TtsScript = "您现在来到永庆美食街的入口。这里很适合作为行程的起点，先感受第四郡热闹的夜间餐饮氛围。"
                }
            ]
        },
        new PoiTemplatePreset
        {
            Key = "seafood-cluster",
            Title = "Cụm quán ốc Vĩnh Khánh",
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
            ImageUrl = "/images/poi-vinh-khanh-seafood-cluster.png",
            AudioMode = "tts-fallback",
            AudioUrl = "/audio/poi-vinh-khanh-seafood-cluster-vi.wav",
            Translations =
            [
                new PoiTemplateTranslationPreset
                {
                    Language = "vi-VN",
                    Title = "Cụm quán ốc Vĩnh Khánh",
                    Summary = "Cụm quán ốc và món ăn đêm đông khách nhất trên tuyến phố.",
                    Description = "Du khách thường dừng lại tại đây để thử ốc, hải sản, món nướng và nhiều phiên bản nước chấm đặc trưng của khu vực.",
                    TtsScript = "Đây là cụm quán ốc tiêu biểu của Vĩnh Khánh. Nhịp phục vụ nhanh, bàn sát vỉa hè và mùi nướng tạo nên bản sắc rất riêng của khu phố này.",
                    AudioUrl = "/audio/poi-vinh-khanh-seafood-cluster-vi.wav"
                },
                new PoiTemplateTranslationPreset
                {
                    Language = "en-US",
                    Title = "Vinh Khanh Seafood Cluster",
                    Summary = "One of the busiest late-night seafood stretches on the route.",
                    Description = "Visitors often stop here for snails, shellfish, grilled plates, and dipping sauces that define the street-food identity of the area.",
                    TtsScript = "This seafood cluster represents the most energetic dining section of Vinh Khanh. It is where the smell of grilled dishes and the sidewalk atmosphere feel strongest.",
                    AudioUrl = "/audio/poi-vinh-khanh-seafood-cluster-en.wav"
                },
                new PoiTemplateTranslationPreset
                {
                    Language = "zh-CN",
                    Title = "永庆海鲜小吃区",
                    Summary = "这里是路线中最热闹的夜间海鲜聚集区之一。",
                    Description = "游客常在这里停留，品尝螺类、贝类、烧烤和带有本地特色的蘸酱。",
                    TtsScript = "这里是永庆街最具代表性的海鲜小吃区。烧烤的香气和路边用餐的氛围最能体现这条街的特色。"
                }
            ]
        },
        new PoiTemplatePreset
        {
            Key = "street-life",
            Title = "Nhịp sống khu vực Vĩnh Khánh",
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
            ImageUrl = "/images/poi-vinh-khanh-street-life.png",
            AudioMode = "tts",
            AudioUrl = "",
            Translations =
            [
                new PoiTemplateTranslationPreset
                {
                    Language = "vi-VN",
                    Title = "Nhịp sống khu vực Vĩnh Khánh",
                    Summary = "Điểm kể chuyện về không khí đường phố, sinh hoạt và nhịp sống về đêm.",
                    Description = "Ngoài ẩm thực, khu vực này còn hấp dẫn nhờ sự nhộn nhịp của người bán, khách đi bộ và không gian sinh hoạt sát nhau trên vỉa hè.",
                    TtsScript = "Điểm này giúp visitor hiểu thêm về đời sống phố phường ở Quận 4. Không chỉ có món ăn, Vĩnh Khánh còn là nơi thể hiện nhịp sống và văn hóa giao tiếp rất riêng."
                },
                new PoiTemplateTranslationPreset
                {
                    Language = "en-US",
                    Title = "Vinh Khanh Street Life",
                    Summary = "A stop that explains the nighttime rhythm and social life of the neighborhood.",
                    Description = "Beyond food, this area is memorable for its busy sidewalks, close-knit street trading, and the flow of people through the evening.",
                    TtsScript = "This stop helps visitors understand the local street rhythm of District 4. Vinh Khanh is not only about food, but also about how people gather, trade, and socialize at night.",
                    AudioUrl = "/audio/poi-vinh-khanh-street-life-en.wav"
                },
                new PoiTemplateTranslationPreset
                {
                    Language = "zh-CN",
                    Title = "永庆街区生活节奏",
                    Summary = "这个点介绍这里夜晚街头的节奏与生活气息。",
                    Description = "除了美食，这里繁忙的人行道、临近的小摊和不断流动的人群，也让第四郡的夜晚更有魅力。",
                    TtsScript = "这个点帮助游客了解第四郡的街头生活。永庆不仅是吃东西的地方，也是观察人们交流、做生意和感受夜晚气氛的空间。"
                }
            ]
        },
        new PoiTemplatePreset
        {
            Key = "bus-vinh-hoi",
            Title = "Trạm xe buýt Vĩnh Hội",
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
            ImageUrl = "/images/poi-vinh-hoi-bus-stop.png",
            AudioMode = "tts-fallback",
            AudioUrl = "/audio/poi-vinh-hoi-bus-stop-vi.wav",
            Translations =
            [
                new PoiTemplateTranslationPreset
                {
                    Language = "vi-VN",
                    Title = "Trạm xe buýt Vĩnh Hội",
                    Summary = "Điểm dừng chân để vào hoặc kết thúc lộ trình tham quan.",
                    Description = "Tại đây visitor có thể quét QR, nghe tóm tắt và chọn hướng tiếp tục đi bộ vào phố ẩm thực.",
                    TtsScript = "Trạm xe buýt Vĩnh Hội phù hợp làm điểm kết tour hoặc trung chuyển. Nội dung QR tại đây giúp visitor nghe nhanh mà không phụ thuộc vào vị trí GPS.",
                    AudioUrl = "/audio/poi-vinh-hoi-bus-stop-vi.wav"
                },
                new PoiTemplateTranslationPreset
                {
                    Language = "en-US",
                    Title = "Vinh Hoi Bus Stop",
                    Summary = "A flexible stop for entering or ending the walking route.",
                    Description = "From here visitors can scan a QR code, hear a short summary, and decide whether to continue into the food street on foot.",
                    TtsScript = "Vinh Hoi Bus Stop works well as a transfer point or tour ending point. The QR content here lets visitors listen quickly without depending on GPS.",
                    AudioUrl = "/audio/poi-vinh-hoi-bus-stop-en.wav"
                },
                new PoiTemplateTranslationPreset
                {
                    Language = "zh-CN",
                    Title = "永会公交站",
                    Summary = "这里适合作为步行美食路线的进入点或结束点。",
                    Description = "游客可以在这里先扫码听一段简介，再决定是否继续步行进入美食街。",
                    TtsScript = "永会公交站既适合作为中转点，也适合作为行程终点。这里的二维码内容能让游客快速开始收听。"
                }
            ]
        }
    ];

    private readonly AppDbContext _context;
    private readonly ImageStorageOptions _imageStorageOptions;
    private readonly AudioStorageOptions _audioStorageOptions;

    public OwnerPoiController(
        AppDbContext context,
        ImageStorageOptions imageStorageOptions,
        AudioStorageOptions audioStorageOptions)
    {
        _context = context;
        _imageStorageOptions = imageStorageOptions;
        _audioStorageOptions = audioStorageOptions;
    }

    public async Task<IActionResult> Index()
    {
        var owner = await GetCurrentOwnerAsync();
        if (owner == null)
        {
            return RedirectToAction("Login", "OwnerAuth");
        }

        var model = new OwnerPoiDashboardViewModel
        {
            Owner = owner,
            LivePois = await _context.Pois
                .AsNoTracking()
                .Include(x => x.Translations)
                .Where(x => x.OwnerId == owner.Id)
                .OrderByDescending(x => x.UpdatedAt)
                .ThenBy(x => x.Name)
                .ToListAsync(),
            Submissions = await _context.PoiSubmissions
                .AsNoTracking()
                .Include(x => x.TranslationSubmissions)
                .Where(x => x.OwnerId == owner.Id)
                .OrderByDescending(x => x.UpdatedAt)
                .ToListAsync()
        };

        return View(model);
    }

    public async Task<IActionResult> Create(string? templateKey = null)
    {
        var owner = await RequireApprovedOwnerAsync();
        if (owner == null)
        {
            return RedirectToAction(nameof(Index));
        }

        var activeLanguages = await GetActiveLanguagesAsync();
        ViewBag.Categories = await BuildCategoryOptionsAsync();
        ViewBag.Languages = await BuildLanguageOptionsAsync();
        PopulateTemplateOptions(templateKey);

        var submission = new PoiSubmission
        {
            OwnerId = owner.Id,
            SubmissionType = "create",
            Status = PoiSubmissionStatus.Draft
        };

        if (!string.IsNullOrWhiteSpace(templateKey) && TryApplyCreatePreset(submission, templateKey))
        {
            ViewBag.SelectedTemplateTitle = CreatePresets.First(x => string.Equals(x.Key, templateKey, StringComparison.OrdinalIgnoreCase)).Title;
        }

        submission.TranslationSubmissions = PoiWorkflowHelper.EnsureSubmissionTranslations(
            activeLanguages,
            submission.DefaultLanguage,
            submission.TranslationSubmissions);
        PopulateTranslationLanguageOptions(activeLanguages, submission.DefaultLanguage);

        return View(submission);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PoiSubmission submission, string submitAction = "draft")
    {
        var owner = await RequireApprovedOwnerAsync();
        if (owner == null)
        {
            return RedirectToAction(nameof(Index));
        }

        await PrepareSubmissionAsync(submission, owner.Id, null, submitAction);

        if (!ModelState.IsValid)
        {
            ViewBag.Categories = await BuildCategoryOptionsAsync(submission.Category);
            ViewBag.Languages = await BuildLanguageOptionsAsync(submission.DefaultLanguage);
            PopulateTranslationLanguageOptions(await GetActiveLanguagesAsync(), submission.DefaultLanguage);
            PopulateTemplateOptions();
            return View(submission);
        }

        _context.PoiSubmissions.Add(submission);
        await _context.SaveChangesAsync();

        TempData["Success"] = submission.Status == PoiSubmissionStatus.PendingReview
            ? "Đã gửi POI cùng gói bản dịch/audio lên cho admin duyệt."
            : "Đã lưu bản nháp POI.";
        return RedirectToAction(nameof(Index));
    }

    private static bool TryApplyCreatePreset(PoiSubmission submission, string templateKey)
    {
        var preset = CreatePresets.FirstOrDefault(x => string.Equals(x.Key, templateKey, StringComparison.OrdinalIgnoreCase));
        if (preset == null)
        {
            return false;
        }

        submission.Name = preset.Title;
        submission.Category = preset.Category;
        submission.Summary = preset.Summary;
        submission.Description = preset.Description;
        submission.Address = preset.Address;
        submission.Latitude = preset.Latitude;
        submission.Longitude = preset.Longitude;
        submission.Radius = preset.Radius;
        submission.ApproachRadiusMeters = preset.ApproachRadiusMeters;
        submission.Priority = preset.Priority;
        submission.DebounceSeconds = preset.DebounceSeconds;
        submission.CooldownSeconds = preset.CooldownSeconds;
        submission.TriggerMode = preset.TriggerMode;
        submission.MapUrl = preset.MapUrl;
        submission.TtsScript = preset.TtsScript;
        submission.DefaultLanguage = preset.DefaultLanguage;
        submission.EstimatedDurationSeconds = preset.EstimatedDurationSeconds;
        submission.ImageUrl = preset.ImageUrl;
        submission.AudioMode = preset.AudioMode;
        submission.AudioUrl = preset.AudioUrl;
        submission.IsActive = true;
        submission.TranslationSubmissions = preset.Translations
            .Select((translation, index) => new PoiTranslationSubmission
            {
                Language = translation.Language,
                Title = translation.Title,
                Summary = translation.Summary,
                Description = translation.Description,
                TtsScript = translation.TtsScript,
                AudioUrl = translation.AudioUrl,
                VoiceName = translation.VoiceName,
                SortOrder = index,
                UpdatedAt = DateTime.UtcNow
            })
            .ToList();
        return true;
    }

    private void PopulateTemplateOptions(string? selectedKey = null)
    {
        ViewBag.TemplatePresets = CreatePresets
            .Select(x => new OwnerPoiTemplatePresetViewModel
            {
                Key = x.Key,
                Title = x.Title,
                Summary = x.Summary,
                Category = x.Category,
                Address = x.Address
            })
            .ToList();
        ViewBag.SelectedTemplateKey = selectedKey ?? string.Empty;
    }

    public async Task<IActionResult> Edit(string id)
    {
        var owner = await RequireApprovedOwnerAsync();
        if (owner == null)
        {
            return RedirectToAction(nameof(Index));
        }

        var submission = await _context.PoiSubmissions
            .Include(x => x.TranslationSubmissions)
            .FirstOrDefaultAsync(x => x.Id == id && x.OwnerId == owner.Id);

        if (submission == null)
        {
            return NotFound();
        }

        if (submission.Status == PoiSubmissionStatus.Approved || submission.Status == PoiSubmissionStatus.Rejected)
        {
            TempData["Error"] = "Submission này đã kết thúc vòng duyệt, bạn hãy tạo một submission chỉnh sửa mới.";
            return RedirectToAction(nameof(Index));
        }

        var activeLanguages = await GetActiveLanguagesAsync();
        submission.TranslationSubmissions = PoiWorkflowHelper.EnsureSubmissionTranslations(
            activeLanguages,
            submission.DefaultLanguage,
            submission.TranslationSubmissions);

        ViewBag.Categories = await BuildCategoryOptionsAsync(submission.Category);
        ViewBag.Languages = await BuildLanguageOptionsAsync(submission.DefaultLanguage);
        PopulateTranslationLanguageOptions(activeLanguages, submission.DefaultLanguage);
        return View(submission);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(PoiSubmission submission, string submitAction = "draft")
    {
        var owner = await RequireApprovedOwnerAsync();
        if (owner == null)
        {
            return RedirectToAction(nameof(Index));
        }

        var existing = await _context.PoiSubmissions
            .Include(x => x.TranslationSubmissions)
            .FirstOrDefaultAsync(x => x.Id == submission.Id && x.OwnerId == owner.Id);

        if (existing == null)
        {
            return NotFound();
        }

        await PrepareSubmissionAsync(submission, owner.Id, existing, submitAction);

        if (!ModelState.IsValid)
        {
            ViewBag.Categories = await BuildCategoryOptionsAsync(submission.Category);
            ViewBag.Languages = await BuildLanguageOptionsAsync(submission.DefaultLanguage);
            PopulateTranslationLanguageOptions(await GetActiveLanguagesAsync(), submission.DefaultLanguage);
            return View(submission);
        }

        existing.PoiId = submission.PoiId;
        existing.SubmissionType = submission.SubmissionType;
        existing.Status = submission.Status;
        existing.ReviewNote = submission.ReviewNote;
        existing.Name = submission.Name;
        existing.Category = submission.Category;
        existing.Summary = submission.Summary;
        existing.Description = submission.Description;
        existing.Address = submission.Address;
        existing.Latitude = submission.Latitude;
        existing.Longitude = submission.Longitude;
        existing.Radius = submission.Radius;
        existing.ApproachRadiusMeters = submission.ApproachRadiusMeters;
        existing.Priority = submission.Priority;
        existing.DebounceSeconds = submission.DebounceSeconds;
        existing.CooldownSeconds = submission.CooldownSeconds;
        existing.TriggerMode = submission.TriggerMode;
        existing.ImageUrl = submission.ImageUrl;
        existing.MapUrl = submission.MapUrl;
        existing.IsActive = submission.IsActive;
        existing.AudioMode = submission.AudioMode;
        existing.AudioUrl = submission.AudioUrl;
        existing.TtsScript = submission.TtsScript;
        existing.DefaultLanguage = submission.DefaultLanguage;
        existing.EstimatedDurationSeconds = submission.EstimatedDurationSeconds;
        existing.SubmittedAt = submission.SubmittedAt;
        existing.UpdatedAt = submission.UpdatedAt;
        existing.ReviewedAt = null;
        existing.ReviewedByAdminId = null;

        _context.PoiTranslationSubmissions.RemoveRange(existing.TranslationSubmissions);
        existing.TranslationSubmissions = submission.TranslationSubmissions
            .Select(x => new PoiTranslationSubmission
            {
                SubmissionId = existing.Id,
                Language = x.Language,
                Title = x.Title,
                Summary = x.Summary,
                Description = x.Description,
                AudioUrl = x.AudioUrl,
                TtsScript = x.TtsScript,
                VoiceName = x.VoiceName,
                SortOrder = x.SortOrder,
                UpdatedAt = x.UpdatedAt
            })
            .ToList();

        await _context.SaveChangesAsync();

        TempData["Success"] = existing.Status == PoiSubmissionStatus.PendingReview
            ? "Đã cập nhật submission và gửi lại gói POI/bản dịch/audio cho admin duyệt."
            : "Đã lưu bản nháp POI.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StartUpdate(int id)
    {
        var owner = await RequireApprovedOwnerAsync();
        if (owner == null)
        {
            return RedirectToAction(nameof(Index));
        }

        var poi = await _context.Pois
            .Include(x => x.Translations)
            .FirstOrDefaultAsync(x => x.Id == id && x.OwnerId == owner.Id);
        if (poi == null)
        {
            return NotFound();
        }

        var existingDraft = await _context.PoiSubmissions
            .FirstOrDefaultAsync(x =>
                x.OwnerId == owner.Id &&
                x.PoiId == id &&
                (x.Status == PoiSubmissionStatus.Draft || x.Status == PoiSubmissionStatus.ChangesRequested || x.Status == PoiSubmissionStatus.PendingReview));

        if (existingDraft != null)
        {
            TempData["Success"] = "Đã mở submission chỉnh sửa hiện có cho POI này.";
            return RedirectToAction(nameof(Edit), new { id = existingDraft.Id });
        }

        var draft = PoiWorkflowHelper.CreateSubmissionFromPoi(poi, owner.Id);
        foreach (var translation in draft.TranslationSubmissions)
        {
            translation.SubmissionId = draft.Id;
        }

        _context.PoiSubmissions.Add(draft);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Đã tạo submission chỉnh sửa mới từ POI live.";
        return RedirectToAction(nameof(Edit), new { id = draft.Id });
    }

    private async Task PrepareSubmissionAsync(PoiSubmission submission, string ownerId, PoiSubmission? existing, string submitAction)
    {
        submission.OwnerId = ownerId;
        submission.AudioMode = PoiWorkflowHelper.NormalizeAudioMode(submission.AudioMode, submission.AudioUrl);
        submission.TranslationSubmissions ??= new List<PoiTranslationSubmission>();

        if ((string.Equals(submission.AudioMode, "audio", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(submission.AudioMode, "audio-priority", StringComparison.OrdinalIgnoreCase)) &&
            string.IsNullOrWhiteSpace(submission.AudioUrl))
        {
            ModelState.AddModelError(nameof(submission.AudioUrl), "Chế độ audio cần có file audio URL.");
        }

        if (existing != null)
        {
            submission.ImageUrl = await PoiImageStorageHelper.SaveImageAsync(submission.ImageFile, submission.ImageUrl, _imageStorageOptions);
            submission.CreatedAt = existing.CreatedAt;
        }
        else
        {
            submission.Id = Guid.NewGuid().ToString("N");
            submission.ImageUrl = await PoiImageStorageHelper.SaveImageAsync(submission.ImageFile, submission.ImageUrl, _imageStorageOptions);
            submission.CreatedAt = DateTime.UtcNow;
        }

        var activeLanguages = await GetActiveLanguagesAsync();
        submission.TranslationSubmissions = PoiWorkflowHelper.EnsureSubmissionTranslations(
            activeLanguages,
            submission.DefaultLanguage,
            submission.TranslationSubmissions);

        for (var index = 0; index < submission.TranslationSubmissions.Count; index++)
        {
            var translation = submission.TranslationSubmissions[index];
            translation.SubmissionId = submission.Id;
            translation.SortOrder = index;
            translation.AudioUrl = await PoiAudioStorageHelper.SaveAudioAsync(
                translation.AudioFile,
                translation.AudioUrl,
                _audioStorageOptions);
            translation.UpdatedAt = DateTime.UtcNow;
        }

        submission.UpdatedAt = DateTime.UtcNow;
        submission.Status = string.Equals(submitAction, "submit", StringComparison.OrdinalIgnoreCase)
            ? PoiSubmissionStatus.PendingReview
            : PoiSubmissionStatus.Draft;
        submission.SubmittedAt = submission.Status == PoiSubmissionStatus.PendingReview ? DateTime.UtcNow : null;
        submission.ReviewedAt = null;
        submission.ReviewedByAdminId = null;
        submission.ReviewNote = existing?.Status == PoiSubmissionStatus.ChangesRequested ? existing.ReviewNote : string.Empty;
    }

    private async Task<ShopOwner?> GetCurrentOwnerAsync()
    {
        var ownerId = OwnerSessionHelper.GetOwnerId(HttpContext);
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            return null;
        }

        return await _context.ShopOwners.FirstOrDefaultAsync(x => x.Id == ownerId);
    }

    private async Task<ShopOwner?> RequireApprovedOwnerAsync()
    {
        var owner = await GetCurrentOwnerAsync();
        if (owner == null)
        {
            return null;
        }

        if (!string.Equals(owner.Status, ShopOwnerStatus.Approved, StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = owner.Status == ShopOwnerStatus.Pending
                ? "Tài khoản chủ quán của bạn vẫn đang chờ admin duyệt."
                : "Tài khoản chủ quán của bạn hiện đang bị tạm khóa.";
            return null;
        }

        return owner;
    }

    private async Task<List<SelectListItem>> BuildCategoryOptionsAsync(string? selected = null)
    {
        return await _context.Categories
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new SelectListItem(x.Name, x.Slug, x.Slug == selected))
            .ToListAsync();
    }

    private async Task<List<SelectListItem>> BuildLanguageOptionsAsync(string? selected = null)
    {
        return await _context.LanguageOptions
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new SelectListItem($"{x.Name} ({x.Code})", x.Code, x.Code == selected))
            .ToListAsync();
    }

    private async Task<List<LanguageOption>> GetActiveLanguagesAsync()
    {
        return await _context.LanguageOptions
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync();
    }

    private void PopulateTranslationLanguageOptions(IEnumerable<LanguageOption> languages, string defaultLanguage)
    {
        ViewBag.TranslationLanguages = languages.ToList();
        ViewBag.DefaultTranslationLanguage = defaultLanguage;
    }
}
