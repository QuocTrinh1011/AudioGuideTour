using AudioGuideAdmin.Data;
using AudioGuideAdmin.Models;
using AudioGuideAdmin.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AudioGuideAdmin.Controllers;

public class QRCodeController : Controller
{
    private const string AppEntryQrCode = "APP-ENTRY-ANDROID";
    private const string QrVisitorCookieName = "audio-guide-qr-visitor-id";
    private const string QrDeviceCookieName = "audio-guide-qr-device-id";
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public QRCodeController(AppDbContext context, IConfiguration configuration, IWebHostEnvironment environment)
    {
        _context = context;
        _configuration = configuration;
        _environment = environment;
    }

    public async Task<IActionResult> Index()
    {
        var items = await _context.QRCodes
            .Include(x => x.Poi)
            .OrderBy(x => x.Code)
            .ToListAsync();

        ViewBag.PublicQrBaseUrl = ResolvePublicQrBaseUrl();
        ViewBag.AppEntryDeepLinkUrl = BuildAppEntryDeepLinkUrl();
        return View(items);
    }

    public IActionResult Create()
    {
        PreparePoiOptions();
        ViewBag.PublicQrBaseUrl = ResolvePublicQrBaseUrl();
        return View(new QRCode());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(QRCode model)
    {
        Normalize(model);

        if (model.PoiId <= 0)
        {
            ModelState.AddModelError(nameof(model.PoiId), "Vui lòng chọn POI.");
        }

        if (string.IsNullOrWhiteSpace(model.Code))
        {
            ModelState.AddModelError(nameof(model.Code), "Mã QR không được để trống.");
        }

        if (!_context.Pois.Any(x => x.Id == model.PoiId))
        {
            ModelState.AddModelError(nameof(model.PoiId), "POI không hợp lệ.");
        }

        if (!string.IsNullOrWhiteSpace(model.Code) && _context.QRCodes.Any(x => x.Code == model.Code))
        {
            ModelState.AddModelError(nameof(model.Code), "Mã QR đã tồn tại.");
        }

        if (!ModelState.IsValid)
        {
            PreparePoiOptions(model.PoiId);
            ViewBag.PublicQrBaseUrl = ResolvePublicQrBaseUrl();
            return View(model);
        }

        try
        {
            _context.QRCodes.Add(model);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Đã tạo QR mới.";
        return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "Không thể tạo QR mới. Hãy kiểm tra lại mã QR và POI đã chọn.");
            PreparePoiOptions(model.PoiId);
            ViewBag.PublicQrBaseUrl = ResolvePublicQrBaseUrl();
            return View(model);
        }
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await _context.QRCodes.FindAsync(id);
        if (item == null)
        {
            return NotFound();
        }

        PreparePoiOptions(item.PoiId);
        ViewBag.PublicQrBaseUrl = ResolvePublicQrBaseUrl();
        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(QRCode model)
    {
        Normalize(model);

        if (model.PoiId <= 0)
        {
            ModelState.AddModelError(nameof(model.PoiId), "Vui lòng chọn POI.");
        }

        if (string.IsNullOrWhiteSpace(model.Code))
        {
            ModelState.AddModelError(nameof(model.Code), "Mã QR không được để trống.");
        }

        if (!_context.Pois.Any(x => x.Id == model.PoiId))
        {
            ModelState.AddModelError(nameof(model.PoiId), "POI không hợp lệ.");
        }

        if (!string.IsNullOrWhiteSpace(model.Code) && _context.QRCodes.Any(x => x.Id != model.Id && x.Code == model.Code))
        {
            ModelState.AddModelError(nameof(model.Code), "Mã QR đã tồn tại.");
        }

        if (!ModelState.IsValid)
        {
            PreparePoiOptions(model.PoiId);
            ViewBag.PublicQrBaseUrl = ResolvePublicQrBaseUrl();
            return View(model);
        }

        var existing = await _context.QRCodes.FindAsync(model.Id);
        if (existing == null)
        {
            return NotFound();
        }

        existing.PoiId = model.PoiId;
        existing.Code = model.Code;
        existing.Note = model.Note;

        await _context.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật QR.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("/QRCode/Open/{code}")]
    public async Task<IActionResult> Open(string code, [FromQuery] string language = "vi-VN")
    {
        var normalizedCode = code?.Trim().ToUpperInvariant() ?? string.Empty;
        var qr = await _context.QRCodes
            .AsNoTracking()
            .Include(x => x.Poi)
            .ThenInclude(x => x!.Translations)
            .FirstOrDefaultAsync(x => x.Code == normalizedCode);

        if (qr?.Poi == null)
        {
            return NotFound();
        }

        var poi = qr.Poi;
        var translation = SelectTranslation(poi.Translations, language);
        var resolvedLanguage = string.IsNullOrWhiteSpace(translation?.Language) ? poi.DefaultLanguage : translation!.Language;
        var qrVisitor = await TouchQrVisitorAsync(qr, poi, resolvedLanguage);
        var model = new QrPublicPageViewModel
        {
            Code = qr.Code,
            Note = qr.Note,
            Title = translation?.Title ?? poi.Name,
            Summary = translation?.Summary ?? poi.Summary,
            Description = translation?.Description ?? poi.Description,
            Address = poi.Address,
            ImageUrl = NormalizeAssetUrl(poi.ImageUrl),
            AudioUrl = NormalizeAssetUrl(ResolvePublicAudioUrl(poi, translation)),
            MapUrl = NormalizeMapUrl(poi.MapUrl, poi.Latitude, poi.Longitude),
            Language = resolvedLanguage,
            LanguageDisplayName = GetLanguageDisplayName(resolvedLanguage),
            NarrationText = ResolveNarrationText(poi, translation),
            NarrationSource = ResolveNarrationSource(poi, translation),
            DeepLinkUrl = BuildQrDeepLinkUrl(qr.Code, resolvedLanguage, qrVisitor?.Id, qrVisitor?.DeviceId)
        };
        model.AvailableLanguages = BuildLanguageOptions(qr.Code, poi, resolvedLanguage);

        ViewBag.HideAdminChrome = true;
        return View(model);
    }

    [HttpGet("/QRCode/RenderSvg/{id:int}")]
    public async Task<IActionResult> RenderSvg(int id)
    {
        var qr = await _context.QRCodes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (qr == null)
        {
            return NotFound();
        }

        return BuildQrSvgResult(qr.Code, BuildQrPayloadUrl(qr.Code));
    }

    [HttpGet("/QRCode/RenderSvgByCode")]
    public IActionResult RenderSvgByCode([FromQuery] string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return NotFound();
        }

        var normalizedCode = code.Trim().ToUpperInvariant();
        return BuildQrSvgResult(normalizedCode, BuildQrPayloadUrl(normalizedCode));
    }

    [HttpGet("/QRCode/DownloadSvg/{id:int}")]
    public async Task<IActionResult> DownloadSvg(int id)
    {
        var qr = await _context.QRCodes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (qr == null)
        {
            return NotFound();
        }

        return BuildQrSvgResult(qr.Code, BuildQrPayloadUrl(qr.Code), download: true);
    }

    [HttpGet("/QRCode/RenderPng/{id:int}")]
    public async Task<IActionResult> RenderPng(int id)
    {
        var qr = await _context.QRCodes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (qr == null)
        {
            return NotFound();
        }

        return BuildQrPngResult(qr.Code, BuildQrPayloadUrl(qr.Code));
    }

    [HttpGet("/QRCode/RenderPngByCode")]
    public IActionResult RenderPngByCode([FromQuery] string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return NotFound();
        }

        var normalizedCode = code.Trim().ToUpperInvariant();
        return BuildQrPngResult(normalizedCode, BuildQrPayloadUrl(normalizedCode));
    }

    [HttpGet("/QRCode/DownloadPng/{id:int}")]
    public async Task<IActionResult> DownloadPng(int id)
    {
        var qr = await _context.QRCodes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (qr == null)
        {
            return NotFound();
        }

        return BuildQrPngResult(qr.Code, BuildQrPayloadUrl(qr.Code), download: true);
    }

    [HttpGet("/QRCode/RenderAppEntryPng")]
    public IActionResult RenderAppEntryPng()
    {
        return BuildQrPngResult(AppEntryQrCode, BuildAppEntryDeepLinkUrl());
    }

    [HttpGet("/QRCode/DownloadAppEntryPng")]
    public IActionResult DownloadAppEntryPng()
    {
        return BuildQrPngResult(AppEntryQrCode, BuildAppEntryDeepLinkUrl(), download: true);
    }

    [HttpGet("/QRCode/RenderAppEntrySvg")]
    public IActionResult RenderAppEntrySvg()
    {
        return BuildQrSvgResult(AppEntryQrCode, BuildAppEntryDeepLinkUrl());
    }

    [HttpGet("/QRCode/DownloadAppEntrySvg")]
    public IActionResult DownloadAppEntrySvg()
    {
        return BuildQrSvgResult(AppEntryQrCode, BuildAppEntryDeepLinkUrl(), download: true);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _context.QRCodes.FindAsync(id);
        if (item != null)
        {
            _context.QRCodes.Remove(item);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã xóa QR.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateBusStopSet()
    {
        var pois = await _context.Pois
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync();

        var specs = new[]
        {
            new { Code = "BUS-KH-001", Label = "Khánh Hội", Note = "Điểm dừng xe buýt phường Khánh Hội" },
            new { Code = "BUS-VH-002", Label = "Vĩnh Hội", Note = "Điểm dừng xe buýt phường Vĩnh Hội" },
            new { Code = "BUS-XC-003", Label = "Xuân Chiếu", Note = "Điểm dừng xe buýt phường Xuân Chiếu / Xóm Chiếu" }
        };

        var created = 0;
        var missing = new List<string>();

        foreach (var spec in specs)
        {
            var poi = FindBusStopPoi(pois, spec.Label);
            if (poi == null)
            {
                missing.Add(spec.Label);
                continue;
            }

            var existing = await _context.QRCodes.FirstOrDefaultAsync(x => x.Code == spec.Code);
            if (existing == null)
            {
                _context.QRCodes.Add(new QRCode
                {
                    PoiId = poi.Id,
                    Code = spec.Code,
                    Note = spec.Note
                });
                created++;
                continue;
            }

            existing.PoiId = poi.Id;
            existing.Note = spec.Note;
        }

        await _context.SaveChangesAsync();

        if (created > 0)
        {
            TempData["Success"] = $"Đã tạo/cập nhật bộ QR xe buýt. Tạo mới: {created}.";
        }

        if (missing.Count > 0)
        {
            TempData["Error"] = $"Chưa tìm thấy POI cho: {string.Join(", ", missing)}. Hãy tạo POI thật rồi bấm lại.";
        }

        return RedirectToAction(nameof(Index));
    }

    private void PreparePoiOptions(int? selectedPoiId = null)
    {
        ViewBag.PoiOptions = _context.Pois
            .OrderBy(x => x.Name)
            .Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = x.Name,
                Selected = selectedPoiId == x.Id
            })
            .ToList();
    }

    private static void Normalize(QRCode model)
    {
        model.Code = model.Code?.Trim().ToUpperInvariant() ?? string.Empty;
        model.Note = model.Note?.Trim() ?? string.Empty;
    }

    private IActionResult BuildQrSvgResult(string code, string payload, bool download = false)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        using var qrGenerator = new QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        var qrSvg = new SvgQRCode(qrData);
        var svg = qrSvg.GetGraphic(12);

        if (!download)
        {
            return new ContentResult
            {
                Content = svg,
                ContentType = "image/svg+xml",
                StatusCode = 200
            };
        }

        var bytes = Encoding.UTF8.GetBytes(svg);
        return new FileContentResult(bytes, "image/svg+xml")
        {
            FileDownloadName = $"{normalizedCode}.svg"
        };
    }

    private IActionResult BuildQrPngResult(string code, string payload, bool download = false)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        using var qrGenerator = new QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        var qrPng = new PngByteQRCode(qrData);
        var bytes = qrPng.GetGraphic(20);

        return new FileContentResult(bytes, "image/png")
        {
            FileDownloadName = download ? $"{normalizedCode}.png" : null
        };
    }

    private string BuildQrPayloadUrl(string code)
    {
        return $"{ResolvePublicQrBaseUrl().TrimEnd('/')}/QRCode/Open/{Uri.EscapeDataString(code.Trim().ToUpperInvariant())}";
    }

    private string BuildQrDeepLinkUrl(string code, string? language, string? visitorId, string? deviceId)
    {
        var normalizedCode = code?.Trim().ToUpperInvariant() ?? string.Empty;
        var normalizedLanguage = string.IsNullOrWhiteSpace(language) ? "vi-VN" : language.Trim();
        var apiBaseUrl = ResolvePublicApiBaseUrl();
        var builder = new StringBuilder();
        builder.Append("audiotour://qr?code=");
        builder.Append(Uri.EscapeDataString(normalizedCode));
        builder.Append("&language=");
        builder.Append(Uri.EscapeDataString(normalizedLanguage));
        builder.Append("&apiBaseUrl=");
        builder.Append(Uri.EscapeDataString(apiBaseUrl));

        if (!string.IsNullOrWhiteSpace(visitorId))
        {
            builder.Append("&visitorId=");
            builder.Append(Uri.EscapeDataString(visitorId));
        }

        if (!string.IsNullOrWhiteSpace(deviceId))
        {
            builder.Append("&deviceId=");
            builder.Append(Uri.EscapeDataString(deviceId));
        }

        return builder.ToString();
    }

    private string BuildAppEntryDeepLinkUrl()
    {
        var builder = new StringBuilder();
        builder.Append("audiotour://qr?entry=app");
        builder.Append("&language=");
        builder.Append(Uri.EscapeDataString("vi-VN"));
        builder.Append("&apiBaseUrl=");
        builder.Append(Uri.EscapeDataString(ResolvePublicApiBaseUrl()));
        builder.Append("&source=");
        builder.Append(Uri.EscapeDataString("qr-app-entry"));
        return builder.ToString();
    }

    private async Task<VisitorProfile?> TouchQrVisitorAsync(QRCode qr, Poi poi, string language)
    {
        var userAgent = Request.Headers.UserAgent.ToString();
        if (!IsLikelyMobileUserAgent(userAgent))
        {
            return null;
        }

        var visitorId = Request.Cookies[QrVisitorCookieName];
        var deviceId = Request.Cookies[QrDeviceCookieName];

        if (string.IsNullOrWhiteSpace(visitorId))
        {
            visitorId = Guid.NewGuid().ToString("N");
            AppendQrCookie(QrVisitorCookieName, visitorId);
        }

        if (string.IsNullOrWhiteSpace(deviceId))
        {
            deviceId = $"qr-web-{RandomNumberGenerator.GetHexString(12).ToLowerInvariant()}";
            AppendQrCookie(QrDeviceCookieName, deviceId);
        }

        var now = DateTime.UtcNow;
        var visitor = await _context.Visitors.FirstOrDefaultAsync(x => x.Id == visitorId || x.DeviceId == deviceId);
        if (visitor == null)
        {
            visitor = new VisitorProfile
            {
                Id = visitorId,
                DeviceId = deviceId,
                DisplayName = BuildQrVisitorDisplayName(userAgent),
                Language = string.IsNullOrWhiteSpace(language) ? "vi-VN" : language,
                AllowAutoPlay = false,
                AllowBackgroundTracking = false,
                CreatedAt = now,
                LastSeenAt = now
            };
            _context.Visitors.Add(visitor);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(visitor.DisplayName) ||
                visitor.DisplayName.Contains("ẩn danh", StringComparison.OrdinalIgnoreCase) ||
                visitor.DisplayName.Contains("quét QR", StringComparison.OrdinalIgnoreCase))
            {
                visitor.DisplayName = BuildQrVisitorDisplayName(userAgent);
            }

            visitor.Language = string.IsNullOrWhiteSpace(language) ? visitor.Language : language;
            visitor.LastSeenAt = now;
        }

        _context.VisitHistories.Add(new VisitHistory
        {
            UserId = visitor.Id,
            PoiId = poi.Id,
            Language = string.IsNullOrWhiteSpace(language) ? "vi-VN" : language,
            StartTime = now,
            EndTime = now,
            Duration = 0,
            TriggerType = "qr-public",
            PlaybackMode = "web",
            WasAutoPlayed = false,
            WasCompleted = true,
            ActivationDistanceMeters = 0
        });

        await _context.SaveChangesAsync();
        return visitor;
    }

    private void AppendQrCookie(string name, string value)
    {
        Response.Cookies.Append(name, value, new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddDays(180),
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Secure = Request.IsHttps
        });
    }

    private static bool IsLikelyMobileUserAgent(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return false;
        }

        return userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase)
            || userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase)
            || userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase)
            || userAgent.Contains("Mobile", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildQrVisitorDisplayName(string userAgent)
    {
        if (userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase))
        {
            return "Android quét QR";
        }

        if (userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase))
        {
            return "iPhone quét QR";
        }

        if (userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase))
        {
            return "iPad quét QR";
        }

        return "Thiết bị quét QR";
    }

    private string ResolvePublicQrBaseUrl()
    {
        var configured = _configuration["Qr:PublicBaseUrl"]?.Trim();
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return NormalizeConfiguredPublicBaseUrl(configured).TrimEnd('/');
        }

        var useHttpsRedirection = _configuration.GetValue("Networking:UseHttpsRedirection", false);
        var requestHost = Request.Host.Host;
        var requestPort = Request.Host.Port;
        var resolvedHost = IsLoopbackHost(requestHost) || IsEmulatorAliasHost(requestHost)
            ? TryResolveLanAddress() ?? requestHost
            : requestHost;

        if (!useHttpsRedirection)
        {
            var httpPort = ResolveHttpPortFromLaunchSettings() ?? requestPort ?? 5038;
            return $"http://{resolvedHost}:{httpPort}";
        }

        return requestPort.HasValue
            ? $"{Request.Scheme}://{resolvedHost}:{requestPort.Value}"
            : $"{Request.Scheme}://{resolvedHost}";
    }

    private string ResolvePublicApiBaseUrl()
    {
        var configured = _configuration["Api:PublicBaseUrl"]?.Trim();
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured.TrimEnd('/');
        }

        var qrBaseUrl = ResolvePublicQrBaseUrl();
        if (Uri.TryCreate(qrBaseUrl, UriKind.Absolute, out var qrUri))
        {
            var apiPort = ResolveApiHttpPortFromLaunchSettings() ?? 5297;
            return $"http://{qrUri.Host}:{apiPort}";
        }

        return "http://10.0.2.2:5297";
    }

    private string NormalizeConfiguredPublicBaseUrl(string configured)
    {
        if (!Uri.TryCreate(configured, UriKind.Absolute, out var uri))
        {
            return configured;
        }

        var useHttpsRedirection = _configuration.GetValue("Networking:UseHttpsRedirection", false);
        if (useHttpsRedirection || !string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase))
        {
            return configured;
        }

        if (!IsLoopbackHost(uri.Host) && !IsPrivateIpHost(uri.Host))
        {
            return configured;
        }

        var host = IsLoopbackHost(uri.Host) || IsEmulatorAliasHost(uri.Host)
            ? TryResolveLanAddress() ?? uri.Host
            : uri.Host;
        var port = ResolveHttpPortFromLaunchSettings() ?? 5038;
        return $"http://{host}:{port}";
    }

    private string? TryResolveLanAddress()
    {
        try
        {
            var address = Dns.GetHostAddresses(Dns.GetHostName())
                .FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip));

            return address?.ToString();
        }
        catch
        {
            return null;
        }
    }

    private int? ResolveHttpPortFromLaunchSettings()
    {
        try
        {
            var launchSettingsPath = Path.Combine(_environment.ContentRootPath, "Properties", "launchSettings.json");
            if (!System.IO.File.Exists(launchSettingsPath))
            {
                return null;
            }

            using var stream = System.IO.File.OpenRead(launchSettingsPath);
            using var document = JsonDocument.Parse(stream);
            if (!document.RootElement.TryGetProperty("profiles", out var profiles))
            {
                return null;
            }

            foreach (var profile in profiles.EnumerateObject())
            {
                if (!profile.Value.TryGetProperty("applicationUrl", out var applicationUrlElement))
                {
                    continue;
                }

                var applicationUrl = applicationUrlElement.GetString();
                if (string.IsNullOrWhiteSpace(applicationUrl))
                {
                    continue;
                }

                foreach (var candidate in applicationUrl.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    if (Uri.TryCreate(candidate, UriKind.Absolute, out var uri) &&
                        string.Equals(uri.Scheme, "http", StringComparison.OrdinalIgnoreCase))
                    {
                        return uri.Port;
                    }
                }
            }
        }
        catch
        {
        }

        return null;
    }

    private int? ResolveApiHttpPortFromLaunchSettings()
    {
        try
        {
            var apiLaunchSettingsPath = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, "..", "AudioGuideAPI", "Properties", "launchSettings.json"));
            if (!System.IO.File.Exists(apiLaunchSettingsPath))
            {
                return null;
            }

            using var stream = System.IO.File.OpenRead(apiLaunchSettingsPath);
            using var document = JsonDocument.Parse(stream);
            if (!document.RootElement.TryGetProperty("profiles", out var profiles))
            {
                return null;
            }

            foreach (var profile in profiles.EnumerateObject())
            {
                if (!profile.Value.TryGetProperty("applicationUrl", out var applicationUrlElement))
                {
                    continue;
                }

                var applicationUrl = applicationUrlElement.GetString();
                if (string.IsNullOrWhiteSpace(applicationUrl))
                {
                    continue;
                }

                foreach (var candidate in applicationUrl.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    if (Uri.TryCreate(candidate, UriKind.Absolute, out var uri) &&
                        string.Equals(uri.Scheme, "http", StringComparison.OrdinalIgnoreCase))
                    {
                        return uri.Port;
                    }
                }
            }
        }
        catch
        {
        }

        return null;
    }

    private static PoiTranslation? SelectTranslation(IEnumerable<PoiTranslation> translations, string language)
    {
        var normalized = string.IsNullOrWhiteSpace(language) ? "vi-VN" : language.Trim();
        var root = normalized.Split('-')[0];

        return translations.FirstOrDefault(x => x.IsPublished && x.Language.Equals(normalized, StringComparison.OrdinalIgnoreCase))
            ?? translations.FirstOrDefault(x => x.IsPublished && x.Language.StartsWith(root, StringComparison.OrdinalIgnoreCase));
    }

    private static string ResolvePublicAudioUrl(Poi poi, PoiTranslation? translation)
    {
        if (!string.IsNullOrWhiteSpace(translation?.AudioUrl))
        {
            return translation.AudioUrl;
        }

        var translationLanguage = translation?.Language?.Trim();
        if (!string.IsNullOrWhiteSpace(translationLanguage) &&
            !translationLanguage.Equals(poi.DefaultLanguage, StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        return poi.AudioUrl;
    }

    private static string ResolveNarrationText(Poi poi, PoiTranslation? translation)
    {
        return FirstNonEmpty(
            translation?.TtsScript,
            translation?.Description,
            translation?.Summary,
            poi.TtsScript,
            poi.Description,
            poi.Summary);
    }

    private static string ResolveNarrationSource(Poi poi, PoiTranslation? translation)
    {
        if (!string.IsNullOrWhiteSpace(translation?.TtsScript))
        {
            return "TTS Script theo ngôn ngữ đang chọn";
        }

        if (!string.IsNullOrWhiteSpace(translation?.Description) || !string.IsNullOrWhiteSpace(translation?.Summary))
        {
            return "Nội dung bản dịch đang chọn";
        }

        if (!string.IsNullOrWhiteSpace(poi.TtsScript))
        {
            return "TTS Script mặc định của POI";
        }

        return "Mô tả mặc định của POI";
    }

    private List<QrPublicLanguageOption> BuildLanguageOptions(string code, Poi poi, string selectedLanguage)
    {
        var items = new List<QrPublicLanguageOption>();
        var seenCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddLanguage(string? languageCode)
        {
            var normalized = NormalizeLanguageCode(languageCode);
            if (string.IsNullOrWhiteSpace(normalized) || !seenCodes.Add(normalized))
            {
                return;
            }

            items.Add(new QrPublicLanguageOption
            {
                Code = normalized,
                Label = GetLanguageDisplayName(normalized),
                Url = Url.Action(nameof(Open), "QRCode", new { code, language = normalized }) ?? $"/QRCode/Open/{Uri.EscapeDataString(code)}?language={Uri.EscapeDataString(normalized)}",
                IsSelected = normalized.Equals(selectedLanguage, StringComparison.OrdinalIgnoreCase)
            });
        }

        AddLanguage(poi.DefaultLanguage);

        foreach (var translation in poi.Translations.Where(x => x.IsPublished))
        {
            AddLanguage(translation.Language);
        }

        return items
            .OrderBy(x => GetLanguageSortOrder(x.Code))
            .ThenBy(x => x.Label, StringComparer.CurrentCulture)
            .ToList();
    }

    private static string NormalizeLanguageCode(string? languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
        {
            return "vi-VN";
        }

        return languageCode.Trim();
    }

    private static string GetLanguageDisplayName(string? languageCode)
    {
        var normalized = NormalizeLanguageCode(languageCode);
        return normalized.ToLowerInvariant() switch
        {
            "vi-vn" => "Tiếng Việt",
            "en-us" => "English",
            "zh-cn" => "简体中文",
            _ => TryGetNativeLanguageName(normalized)
        };
    }

    private static string TryGetNativeLanguageName(string languageCode)
    {
        try
        {
            return new CultureInfo(languageCode).NativeName;
        }
        catch
        {
            return languageCode;
        }
    }

    private static int GetLanguageSortOrder(string languageCode)
    {
        return NormalizeLanguageCode(languageCode).ToLowerInvariant() switch
        {
            "vi-vn" => 0,
            "en-us" => 1,
            "zh-cn" => 2,
            _ => 99
        };
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return string.Empty;
    }

    private string NormalizeAssetUrl(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        if (Uri.TryCreate(raw, UriKind.Absolute, out _))
        {
            return raw;
        }

        return $"{ResolvePublicQrBaseUrl().TrimEnd('/')}/{raw.TrimStart('/')}";
    }

    private static string NormalizeMapUrl(string? raw, double latitude, double longitude)
    {
        if (!string.IsNullOrWhiteSpace(raw))
        {
            return raw;
        }

        return latitude == 0 || longitude == 0
            ? string.Empty
            : $"https://www.google.com/maps/search/?api=1&query={latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)},{longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
    }

    private static Poi? FindBusStopPoi(IEnumerable<Poi> pois, string label)
    {
        var keywords = label.ToLowerInvariant() switch
        {
            "khánh hội" => new[] { "khanh hoi", "khánh hội" },
            "vĩnh hội" => new[] { "vinh hoi", "vĩnh hội" },
            "xuân chiếu" => new[] { "xuan chieu", "xuân chiếu", "xom chieu", "xóm chiếu" },
            _ => new[] { label.ToLowerInvariant() }
        };

        return pois.FirstOrDefault(p =>
        {
            var name = $"{p.Name} {p.Address}".ToLowerInvariant();
            return keywords.Any(name.Contains);
        });
    }

    private static bool IsLoopbackHost(string host)
    {
        return string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
            || string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsEmulatorAliasHost(string host)
    {
        return string.Equals(host, "10.0.2.2", StringComparison.OrdinalIgnoreCase)
            || string.Equals(host, "10.0.3.2", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPrivateIpHost(string host)
    {
        if (!IPAddress.TryParse(host, out var address))
        {
            return false;
        }

        var bytes = address.GetAddressBytes();
        return bytes.Length == 4 && (
            bytes[0] == 10 ||
            (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
            (bytes[0] == 192 && bytes[1] == 168));
    }
}
