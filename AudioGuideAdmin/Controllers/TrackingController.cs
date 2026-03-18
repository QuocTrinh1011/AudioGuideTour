using Microsoft.AspNetCore.Mvc;
using AudioGuideAdmin.Data;

public class TrackingController : Controller
{
    private readonly AppDbContext _context;

    public TrackingController(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        var data = _context.VisitHistories.ToList();
        return View(data);
    }
}