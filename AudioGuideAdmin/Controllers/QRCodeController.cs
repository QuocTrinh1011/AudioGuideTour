using AudioGuideAdmin.Data;
using AudioGuideAdmin.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AudioGuideAdmin.Controllers;

public class QRCodeController : Controller
{
    private readonly AppDbContext _context;

    public QRCodeController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var items = await _context.QRCodes
            .Include(x => x.Poi)
            .OrderBy(x => x.Code)
            .ToListAsync();

        return View(items);
    }

    public IActionResult Create()
    {
        PreparePoiOptions();
        return View(new QRCode());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(QRCode model)
    {
        Normalize(model);

        if (!_context.Pois.Any(x => x.Id == model.PoiId))
        {
            ModelState.AddModelError(nameof(model.PoiId), "POI khong hop le.");
        }

        if (_context.QRCodes.Any(x => x.Code == model.Code))
        {
            ModelState.AddModelError(nameof(model.Code), "Ma QR da ton tai.");
        }

        if (!ModelState.IsValid)
        {
            PreparePoiOptions(model.PoiId);
            return View(model);
        }

        _context.QRCodes.Add(model);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Da tao QR moi cho mobile app.";
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
        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(QRCode model)
    {
        Normalize(model);

        if (!_context.Pois.Any(x => x.Id == model.PoiId))
        {
            ModelState.AddModelError(nameof(model.PoiId), "POI khong hop le.");
        }

        if (_context.QRCodes.Any(x => x.Id != model.Id && x.Code == model.Code))
        {
            ModelState.AddModelError(nameof(model.Code), "Ma QR da ton tai.");
        }

        if (!ModelState.IsValid)
        {
            PreparePoiOptions(model.PoiId);
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
        TempData["Success"] = "Da cap nhat QR.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var item = await _context.QRCodes.FindAsync(id);
        if (item != null)
        {
            _context.QRCodes.Remove(item);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Da xoa QR.";
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
            new { Code = "BUS-KH-001", Label = "Khanh Hoi", Note = "Diem dung xe buyt phuong Khanh Hoi" },
            new { Code = "BUS-VH-002", Label = "Vinh Hoi", Note = "Diem dung xe buyt phuong Vinh Hoi" },
            new { Code = "BUS-XC-003", Label = "Xuan Chieu", Note = "Diem dung xe buyt phuong Xuan Chieu / Xom Chieu" }
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
            TempData["Success"] = $"Da tao/cap nhat bo QR xe buyt cho mobile app. Tao moi: {created}.";
        }

        if (missing.Count > 0)
        {
            TempData["Error"] = $"Chua tim thay POI thuc te cho: {string.Join(", ", missing)}. Hay tao POI that roi bam lai.";
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

    private static Poi? FindBusStopPoi(IEnumerable<Poi> pois, string label)
    {
        var keywords = label.ToLowerInvariant() switch
        {
            "khanh hoi" => new[] { "khanh hoi", "khánh hội" },
            "vinh hoi" => new[] { "vinh hoi", "vĩnh hội" },
            "xuan chieu" => new[] { "xuan chieu", "xuân chiếu", "xom chieu", "xóm chiếu" },
            _ => new[] { label.ToLowerInvariant() }
        };

        return pois.FirstOrDefault(p =>
        {
            var name = $"{p.Name} {p.Address}".ToLowerInvariant();
            return keywords.Any(name.Contains);
        });
    }
}
