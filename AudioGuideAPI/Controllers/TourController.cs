using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AudioGuideAPI.Data;
using AudioGuideAPI.Models;

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
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _context.Set<Tour>().ToListAsync());
    }

    [HttpPost]
    public async Task<IActionResult> Create(Tour tour)
    {
        _context.Set<Tour>().Add(tour);

        await _context.SaveChangesAsync();

        return Ok(tour);
    }
}