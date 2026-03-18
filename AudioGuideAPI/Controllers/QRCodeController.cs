using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AudioGuideAPI.Data;

namespace AudioGuideAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QRCodeController : ControllerBase
{
    private readonly AppDbContext _context;

    public QRCodeController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("{code}")]
    public async Task<IActionResult> GetByCode(string code)
    {
        var qr = await _context.QRCodes
            .FirstOrDefaultAsync(x => x.Code == code);

        if (qr == null)
            return NotFound();

        return Ok(qr);
    }
}