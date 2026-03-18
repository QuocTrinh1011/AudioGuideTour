using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AudioGuideAPI.Data;
using AudioGuideAPI.Models;

namespace AudioGuideAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PoiController : ControllerBase
{
    private readonly AppDbContext _context;

    public PoiController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var pois = await _context.Pois.ToListAsync();
        return Ok(pois);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var poi = await _context.Pois.FindAsync(id);

        if (poi == null)
            return NotFound();

        return Ok(poi);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Poi poi)
    {
        _context.Pois.Add(poi);
        await _context.SaveChangesAsync();

        return Ok(poi);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Poi poi)
    {
        if (id != poi.Id)
            return BadRequest();

        _context.Entry(poi).State = EntityState.Modified;

        await _context.SaveChangesAsync();

        return Ok(poi);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var poi = await _context.Pois.FindAsync(id);

        if (poi == null)
            return NotFound();

        _context.Pois.Remove(poi);

        await _context.SaveChangesAsync();

        return Ok();
    }
}