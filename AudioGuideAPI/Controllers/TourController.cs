using AudioGuideAPI.Data;
using AudioGuideAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AudioGuideAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TourController : ControllerBase
{
    private readonly AppDbContext _context;

    public TourController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool activeOnly = true)
    {
        var query = _context.Tours
            .AsNoTracking()
            .Include(x => x.Stops.OrderBy(s => s.SortOrder))
            .ThenInclude(x => x.Poi)
            .AsQueryable();

        if (activeOnly)
        {
            query = query.Where(x => x.IsActive);
        }

        return Ok(await query.OrderBy(x => x.Name).ToListAsync());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var tour = await _context.Tours
            .Include(x => x.Stops.OrderBy(s => s.SortOrder))
            .ThenInclude(x => x.Poi)
            .ThenInclude(x => x!.Translations)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (tour == null)
        {
            return NotFound();
        }

        return Ok(tour);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Tour tour)
    {
        tour.CreatedAt = DateTime.UtcNow;
        tour.UpdatedAt = DateTime.UtcNow;
        _context.Tours.Add(tour);
        await _context.SaveChangesAsync();
        return Ok(tour);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Tour tour)
    {
        var existing = await _context.Tours
            .Include(x => x.Stops)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (existing == null)
        {
            return NotFound();
        }

        existing.Name = tour.Name;
        existing.Description = tour.Description;
        existing.Language = tour.Language;
        existing.CoverImageUrl = tour.CoverImageUrl;
        existing.IsActive = tour.IsActive;
        existing.EstimatedDurationMinutes = tour.EstimatedDurationMinutes;
        existing.UpdatedAt = DateTime.UtcNow;

        if (tour.Stops.Count > 0)
        {
            _context.TourStops.RemoveRange(existing.Stops);
            existing.Stops = tour.Stops
                .OrderBy(x => x.SortOrder)
                .ToList();
        }

        await _context.SaveChangesAsync();
        return Ok(existing);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var tour = await _context.Tours.FindAsync(id);
        if (tour == null)
        {
            return NotFound();
        }

        _context.Tours.Remove(tour);
        await _context.SaveChangesAsync();
        return Ok();
    }
}
