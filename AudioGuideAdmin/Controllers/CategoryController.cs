using AudioGuideAdmin.Data;
using AudioGuideAdmin.Models;
using Microsoft.AspNetCore.Mvc;

namespace AudioGuideAdmin.Controllers;

public class CategoryController : Controller
{
    private readonly AppDbContext _context;

    public CategoryController(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        var items = _context.Categories
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToList();

        return View(items);
    }

    public IActionResult Create()
    {
        return View(new Category());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Category model)
    {
        Normalize(model);

        if (_context.Categories.Any(x => x.Slug == model.Slug))
        {
            ModelState.AddModelError(nameof(model.Slug), "Ma danh muc da ton tai.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        _context.Categories.Add(model);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Da tao danh muc moi.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await _context.Categories.FindAsync(id);
        return item == null ? NotFound() : View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Category model)
    {
        Normalize(model);

        if (_context.Categories.Any(x => x.Id != model.Id && x.Slug == model.Slug))
        {
            ModelState.AddModelError(nameof(model.Slug), "Ma danh muc da ton tai.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var existing = await _context.Categories.FindAsync(model.Id);
        if (existing == null)
        {
            return NotFound();
        }

        existing.Name = model.Name;
        existing.Slug = model.Slug;
        existing.Description = model.Description;
        existing.ThemeColor = model.ThemeColor;
        existing.SortOrder = model.SortOrder;
        existing.IsActive = model.IsActive;

        await _context.SaveChangesAsync();
        TempData["Success"] = "Da cap nhat danh muc.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null)
        {
            return RedirectToAction(nameof(Index));
        }

        var poiCount = _context.Pois.Count(x => x.Category == category.Slug);
        if (poiCount > 0)
        {
            TempData["Error"] = $"Khong the xoa danh muc nay vi dang co {poiCount} POI su dung.";
            return RedirectToAction(nameof(Index));
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Da xoa danh muc.";
        return RedirectToAction(nameof(Index));
    }

    private static void Normalize(Category model)
    {
        model.Slug = model.Slug.Trim().ToLowerInvariant();
        model.Name = model.Name.Trim();
        model.Description = model.Description?.Trim() ?? "";
        model.ThemeColor = string.IsNullOrWhiteSpace(model.ThemeColor) ? "#17324d" : model.ThemeColor.Trim();
    }
}
