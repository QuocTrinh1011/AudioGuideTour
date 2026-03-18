using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AudioGuideAdmin.Data;
using AudioGuideAdmin.Models;

namespace AudioGuideAdmin.Controllers;

public class PoiController : Controller
{
    private readonly AppDbContext _context;

    public PoiController(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult Index(string search)
    {
        var pois = _context.Pois.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            pois = pois.Where(p => p.Name.Contains(search));
        }

        return View(pois.ToList());
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(Poi poi)
    {
        _context.Pois.Add(poi);

        await _context.SaveChangesAsync();

        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Details(int id)
    {
        var poi = await _context.Pois
            .Include(p => p.Translations)
            .FirstOrDefaultAsync(p => p.Id == id);

        return View(poi);
    }

    public async Task<IActionResult> Delete(int id)
    {
        var poi = await _context.Pois.FindAsync(id);

        if (poi != null)
        {
            _context.Pois.Remove(poi);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Edit(int id)
    {
        var poi = await _context.Pois.FindAsync(id);
        if (poi == null) return NotFound();

        return View(poi);
    }

    public IActionResult Map()
    {
        var pois = _context.Pois.ToList();
        return View(pois);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Poi poi)
    {
        var existing = await _context.Pois.FindAsync(poi.Id);
        if (existing == null) return NotFound();

        existing.Name = poi.Name;
        existing.Latitude = poi.Latitude;
        existing.Longitude = poi.Longitude;
        existing.Radius = poi.Radius;
        existing.ImageUrl = poi.ImageUrl;
        existing.IsActive = poi.IsActive;

        await _context.SaveChangesAsync();

        return RedirectToAction("Index");
    }
}