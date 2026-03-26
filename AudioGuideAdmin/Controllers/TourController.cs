using AudioGuideAdmin.Data;
using AudioGuideAdmin.Models;
using AudioGuideAdmin.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AudioGuideAdmin.Controllers;

public class TourController : Controller
{
    private readonly AppDbContext _context;

    public TourController(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        var tours = _context.Tours
            .Include(x => x.Stops)
            .OrderBy(x => x.Name)
            .ToList();

        return View(tours);
    }

    public IActionResult Create()
    {
        return View(BuildForm(new Tour()));
    }

    [HttpPost]
    public async Task<IActionResult> Create(TourFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(BuildForm(model.Tour, model.StopPoiIds));
        }

        model.Tour.CreatedAt = DateTime.UtcNow;
        model.Tour.UpdatedAt = DateTime.UtcNow;
        model.Tour.Stops = BuildStops(model.StopPoiIds, model.Tour.Id);

        _context.Tours.Add(model.Tour);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var tour = await _context.Tours
            .Include(x => x.Stops.OrderBy(s => s.SortOrder))
            .FirstOrDefaultAsync(x => x.Id == id);

        if (tour == null)
        {
            return NotFound();
        }

        var stopPoiIds = string.Join(",", tour.Stops.OrderBy(x => x.SortOrder).Select(x => x.PoiId));
        return View(BuildForm(tour, stopPoiIds));
    }

    [HttpPost]
    public async Task<IActionResult> Edit(TourFormViewModel model)
    {
        var existing = await _context.Tours
            .Include(x => x.Stops)
            .FirstOrDefaultAsync(x => x.Id == model.Tour.Id);

        if (existing == null)
        {
            return NotFound();
        }

        existing.Name = model.Tour.Name;
        existing.Description = model.Tour.Description;
        existing.Language = model.Tour.Language;
        existing.CoverImageUrl = model.Tour.CoverImageUrl;
        existing.IsActive = model.Tour.IsActive;
        existing.EstimatedDurationMinutes = model.Tour.EstimatedDurationMinutes;
        existing.UpdatedAt = DateTime.UtcNow;

        _context.TourStops.RemoveRange(existing.Stops);
        existing.Stops = BuildStops(model.StopPoiIds, existing.Id);

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var tour = await _context.Tours.FindAsync(id);
        if (tour != null)
        {
            _context.Tours.Remove(tour);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private TourFormViewModel BuildForm(Tour tour, string stopPoiIds = "")
    {
        return new TourFormViewModel
        {
            Tour = tour,
            StopPoiIds = stopPoiIds,
            PoiOptions = _context.Pois
                .OrderBy(x => x.Name)
                .Select(x => new SelectListItem($"{x.Name} (#{x.Id})", x.Id.ToString()))
                .ToList()
        };
    }

    private static List<TourStop> BuildStops(string stopPoiIds, int tourId)
    {
        return stopPoiIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select((value, index) => int.TryParse(value, out var poiId)
                ? new TourStop
                {
                    TourId = tourId,
                    PoiId = poiId,
                    SortOrder = index + 1,
                    AutoPlay = true
                }
                : null)
            .Where(x => x != null)
            .Cast<TourStop>()
            .ToList();
    }
}
