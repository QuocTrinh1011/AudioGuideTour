using AudioGuideAdmin.Data;
using AudioGuideAdmin.Helpers;
using AudioGuideAdmin.Models;
using AudioGuideAdmin.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AudioGuideAdmin.Controllers;

public class TourController : Controller
{
    private readonly AppDbContext _context;
    private readonly ImageStorageOptions _imageStorageOptions;

    public TourController(AppDbContext context, ImageStorageOptions imageStorageOptions)
    {
        _context = context;
        _imageStorageOptions = imageStorageOptions;
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

        model.Tour.CoverImageUrl = await SaveImageAsync(model.CoverImageFile, model.Tour.CoverImageUrl);
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
        existing.CoverImageUrl = await SaveImageAsync(model.CoverImageFile, model.Tour.CoverImageUrl);
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

    private async Task<string> SaveImageAsync(IFormFile? imageFile, string? currentUrl)
    {
        if (imageFile == null || imageFile.Length <= 0)
        {
            return (currentUrl ?? string.Empty).Trim();
        }

        Directory.CreateDirectory(_imageStorageOptions.RootPath);
        var extension = Path.GetExtension(imageFile.FileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".png";
        }

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var physicalPath = Path.Combine(_imageStorageOptions.RootPath, fileName);
        await using var stream = new FileStream(physicalPath, FileMode.Create);
        await imageFile.CopyToAsync(stream);
        return $"/images/{fileName}";
    }
}
