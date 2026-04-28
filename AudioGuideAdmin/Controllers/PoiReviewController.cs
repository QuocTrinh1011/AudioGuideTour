using AudioGuideAdmin.Controllers.Data;
using AudioGuideAdmin.Helpers;
using AudioGuideAdmin.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AudioGuideAdmin.Controllers;

public class PoiReviewController : Controller
{
    private readonly AppDbContext _context;

    public PoiReviewController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? status = null)
    {
        var effectiveStatus = string.IsNullOrWhiteSpace(status) ? PoiSubmissionStatus.PendingReview : status;
        ViewBag.Status = effectiveStatus;

        var submissions = await _context.PoiSubmissions
            .AsNoTracking()
            .Include(x => x.Owner)
            .Include(x => x.Poi)
            .Include(x => x.TranslationSubmissions)
            .Where(x => x.Status == effectiveStatus)
            .OrderByDescending(x => x.SubmittedAt ?? x.UpdatedAt)
            .ToListAsync();

        return View(submissions);
    }

    public async Task<IActionResult> Review(string id)
    {
        var submission = await _context.PoiSubmissions
            .Include(x => x.Owner)
            .Include(x => x.Poi)
            .ThenInclude(x => x!.Translations)
            .Include(x => x.TranslationSubmissions)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (submission == null)
        {
            return NotFound();
        }

        return View(submission);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(string id, string? reviewNote = null)
    {
        var submission = await _context.PoiSubmissions
            .Include(x => x.Poi)
            .ThenInclude(x => x!.Translations)
            .Include(x => x.TranslationSubmissions)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (submission == null)
        {
            return NotFound();
        }

        Poi livePoi;
        var requiresLivePoiInsert = false;
        if (submission.PoiId.HasValue)
        {
            livePoi = submission.Poi ?? await _context.Pois
                .Include(x => x.Translations)
                .FirstOrDefaultAsync(x => x.Id == submission.PoiId.Value) ?? new Poi
            {
                CreatedAt = DateTime.UtcNow
            };

            if (livePoi.Id == 0)
            {
                _context.Pois.Add(livePoi);
                requiresLivePoiInsert = true;
            }
        }
        else
        {
            livePoi = new Poi
            {
                CreatedAt = DateTime.UtcNow
            };
            _context.Pois.Add(livePoi);
            requiresLivePoiInsert = true;
        }

        PoiWorkflowHelper.ApplySubmissionToPoi(livePoi, submission);
        if (requiresLivePoiInsert)
        {
            await _context.SaveChangesAsync();
            submission.PoiId = livePoi.Id;
        }

        PoiWorkflowHelper.ApplyTranslationSubmissionsToPoi(
            livePoi,
            submission,
            submission.TranslationSubmissions.OrderBy(x => x.SortOrder).ThenBy(x => x.Language));

        submission.Status = PoiSubmissionStatus.Approved;
        submission.ReviewNote = reviewNote?.Trim() ?? string.Empty;
        submission.ReviewedAt = DateTime.UtcNow;
        submission.ReviewedByAdminId = await ResolveAdminReviewerIdAsync();
        submission.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Đã duyệt submission và cập nhật POI live.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestChanges(string id, string reviewNote)
    {
        var submission = await _context.PoiSubmissions.FirstOrDefaultAsync(x => x.Id == id);
        if (submission == null)
        {
            return NotFound();
        }

        submission.Status = PoiSubmissionStatus.ChangesRequested;
        submission.ReviewNote = reviewNote?.Trim() ?? string.Empty;
        submission.ReviewedAt = DateTime.UtcNow;
        submission.ReviewedByAdminId = await ResolveAdminReviewerIdAsync();
        submission.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["Success"] = "Đã trả submission về cho chủ quán chỉnh sửa.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(string id, string reviewNote)
    {
        var submission = await _context.PoiSubmissions.FirstOrDefaultAsync(x => x.Id == id);
        if (submission == null)
        {
            return NotFound();
        }

        submission.Status = PoiSubmissionStatus.Rejected;
        submission.ReviewNote = reviewNote?.Trim() ?? string.Empty;
        submission.ReviewedAt = DateTime.UtcNow;
        submission.ReviewedByAdminId = await ResolveAdminReviewerIdAsync();
        submission.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["Success"] = "Đã từ chối submission.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<int?> ResolveAdminReviewerIdAsync()
    {
        var username = HttpContext.Session.GetString("user");
        if (string.IsNullOrWhiteSpace(username))
        {
            return null;
        }

        return await _context.Users
            .Where(x => x.Username == username)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync();
    }
}
