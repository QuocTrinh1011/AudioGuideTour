using AudioGuideAPI.Data;
using AudioGuideAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AudioGuideAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TranslationController : ControllerBase
{
    private readonly AppDbContext _context;

    public TranslationController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("{poiId}/{lang}")]
    public async Task<IActionResult> Get(int poiId, string lang)
    {
        var result = await _context.PoiTranslations
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.PoiId == poiId && x.Language == lang);

        if (result == null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    [HttpGet("poi/{poiId}")]
    public async Task<IActionResult> GetByPoi(int poiId)
    {
        var items = await _context.PoiTranslations
            .AsNoTracking()
            .Where(x => x.PoiId == poiId)
            .OrderBy(x => x.Language)
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create(PoiTranslation model)
    {
        var exists = await _context.PoiTranslations
            .AnyAsync(x => x.PoiId == model.PoiId && x.Language == model.Language);

        if (exists)
        {
            return Conflict("Translation already exists for this language.");
        }

        model.AudioUrl = string.Empty;
        model.UpdatedAt = DateTime.UtcNow;
        _context.PoiTranslations.Add(model);
        await _context.SaveChangesAsync();
        return Ok(model);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, PoiTranslation model)
    {
        var existing = await _context.PoiTranslations.FindAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        existing.Language = model.Language;
        existing.Title = model.Title;
        existing.Summary = model.Summary;
        existing.Description = model.Description;
        existing.AudioUrl = string.Empty;
        existing.TtsScript = model.TtsScript;
        existing.VoiceName = model.VoiceName;
        existing.IsPublished = model.IsPublished;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(existing);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _context.PoiTranslations.FindAsync(id);
        if (item == null)
        {
            return NotFound();
        }

        _context.PoiTranslations.Remove(item);
        await _context.SaveChangesAsync();
        return Ok();
    }
}
