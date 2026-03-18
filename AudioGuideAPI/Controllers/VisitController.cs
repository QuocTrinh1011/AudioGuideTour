using Microsoft.AspNetCore.Mvc;
using AudioGuideAPI.Data;
using AudioGuideAPI.Models;

namespace AudioGuideAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VisitController : ControllerBase
{
    private readonly AppDbContext _context;

    public VisitController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> Save(VisitHistory visit)
    {
        _context.VisitHistories.Add(visit);

        await _context.SaveChangesAsync();

        return Ok();
    }
}