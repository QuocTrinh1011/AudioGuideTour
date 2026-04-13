using AudioGuideAdmin.Data;
using AudioGuideAdmin.Models;
using AudioGuideAdmin.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using System.Net;
using System.Text;
using System.Text.Json;

namespace AudioGuideAdmin.Controllers;

public class QRCodeController : Controller
{
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

        if (!_context.Pois.Any(x => x.Id == model.PoiId))
        {
            ModelState.AddModelError(nameof(model.PoiId), "POI không hợp lệ.");
        }

        if (_context.QRCodes.Any(x => x.Code == model.Code))
        {
            ModelState.AddModelError(nameof(model.Code), "Mã QR đã tồn tại.");
        }

        if (!ModelState.IsValid)
        {
            PreparePoiOptions(model.PoiId);
            ViewBag.PublicQrBaseUrl = ResolvePublicQrBaseUrl();
            return View(model);
        }

        _context.QRCodes.Add(model);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Đã tạo QR mới.";
        return RedirectToAction(nameof(Index));
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

        if (!_context.Pois.Any(x => x.Id == model.PoiId))
        {
            ModelState.AddModelError(nameof(model.PoiId), "POI không hợp lệ.");
        }

        if (_context.QRCodes.Any(x => x.Id != model.Id && x.Code == model.Code))
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
        var model = new QrPublicPageViewModel
        {
            Code = qr.Code,
            Note = qr.Note,
            Title = translation?.Title ?? poi.Name,
            Summary = translation?.Summary ?? poi.Summary,
            Description = translation?.Description ?? poi.Description,
            Address = poi.Address,
            ImageUrl = NormalizeAssetUrl(poi.ImageUrl),
            AudioUrl = NormalizeAssetUrl(string.IsNullOrWhiteSpace(translation?.AudioUrl) ? poi.AudioUrl : translation!.AudioUrl),
            MapUrl = NormalizeMapUrl(poi.MapUrl, poi.Latitude, poi.Longitude),
            Language = string.IsNullOrWhiteSpace(translation?.Language) ? poi.DefaultLanguage : translation!.Language,
            DeepLinkUrl = $"audiotour://qr?code={Uri.EscapeDataString(qr.Code)}"
        };

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

    private static PoiTranslation? SelectTranslation(IEnumerable<PoiTranslation> translations, string language)
    {
        var normalized = string.IsNullOrWhiteSpace(language) ? "vi-VN" : language.Trim();
        var root = normalized.Split('-')[0];

        return translations.FirstOrDefault(x => x.IsPublished && x.Language.Equals(normalized, StringComparison.OrdinalIgnoreCase))
            ?? translations.FirstOrDefault(x => x.IsPublished && x.Language.StartsWith(root, StringComparison.OrdinalIgnoreCase));
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
