using AudioGuideAdmin.Controllers.Data;
using AudioGuideAdmin.Helpers;
using AudioGuideAdmin.Models;
using AudioGuideAdmin.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AudioGuideAdmin.Controllers;

public class OwnerPoiController : Controller
{
    private readonly AppDbContext _context;
    private readonly ImageStorageOptions _imageStorageOptions;

    public OwnerPoiController(AppDbContext context, ImageStorageOptions imageStorageOptions)
    {
        _context = context;
        _imageStorageOptions = imageStorageOptions;
    }

    public async Task<IActionResult> Index()
    {
        var owner = await GetCurrentOwnerAsync();
        if (owner == null)
        {
            return RedirectToAction("Login", "OwnerAuth");
        }

        var model = new OwnerPoiDashboardViewModel
        {
            Owner = owner,
            LivePois = await _context.Pois
                .AsNoTracking()
                .Where(x => x.OwnerId == owner.Id)
                .OrderByDescending(x => x.UpdatedAt)
                .ThenBy(x => x.Name)
                .ToListAsync(),
            Submissions = await _context.PoiSubmissions
                .AsNoTracking()
                .Where(x => x.OwnerId == owner.Id)
                .OrderByDescending(x => x.UpdatedAt)
                .ToListAsync()
        };

        return View(model);
    }

    public async Task<IActionResult> Create()
    {
        var owner = await RequireApprovedOwnerAsync();
        if (owner == null)
        {
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Categories = await BuildCategoryOptionsAsync();
        ViewBag.Languages = await BuildLanguageOptionsAsync();
        return View(new PoiSubmission
        {
            OwnerId = owner.Id,
            SubmissionType = "create",
            Status = PoiSubmissionStatus.Draft
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PoiSubmission submission, string submitAction = "draft")
    {
        var owner = await RequireApprovedOwnerAsync();
        if (owner == null)
        {
            return RedirectToAction(nameof(Index));
        }

        await PrepareSubmissionAsync(submission, owner.Id, null, submitAction);

        if (!ModelState.IsValid)
        {
            ViewBag.Categories = await BuildCategoryOptionsAsync(submission.Category);
            ViewBag.Languages = await BuildLanguageOptionsAsync(submission.DefaultLanguage);
            return View(submission);
        }

        _context.PoiSubmissions.Add(submission);
        await _context.SaveChangesAsync();

        TempData["Success"] = submission.Status == PoiSubmissionStatus.PendingReview
            ? "Đã gửi POI lên cho admin duyệt."
            : "Đã lưu bản nháp POI.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(string id)
    {
        var owner = await RequireApprovedOwnerAsync();
        if (owner == null)
        {
            return RedirectToAction(nameof(Index));
        }

        var submission = await _context.PoiSubmissions
            .FirstOrDefaultAsync(x => x.Id == id && x.OwnerId == owner.Id);

        if (submission == null)
        {
            return NotFound();
        }

        if (submission.Status == PoiSubmissionStatus.Approved || submission.Status == PoiSubmissionStatus.Rejected)
        {
            TempData["Error"] = "Submission này đã kết thúc vòng duyệt, bạn hãy tạo một submission chỉnh sửa mới.";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Categories = await BuildCategoryOptionsAsync(submission.Category);
        ViewBag.Languages = await BuildLanguageOptionsAsync(submission.DefaultLanguage);
        return View(submission);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(PoiSubmission submission, string submitAction = "draft")
    {
        var owner = await RequireApprovedOwnerAsync();
        if (owner == null)
        {
            return RedirectToAction(nameof(Index));
        }

        var existing = await _context.PoiSubmissions
            .FirstOrDefaultAsync(x => x.Id == submission.Id && x.OwnerId == owner.Id);

        if (existing == null)
        {
            return NotFound();
        }

        await PrepareSubmissionAsync(submission, owner.Id, existing, submitAction);

        if (!ModelState.IsValid)
        {
            ViewBag.Categories = await BuildCategoryOptionsAsync(submission.Category);
            ViewBag.Languages = await BuildLanguageOptionsAsync(submission.DefaultLanguage);
            return View(submission);
        }

        existing.PoiId = submission.PoiId;
        existing.SubmissionType = submission.SubmissionType;
        existing.Status = submission.Status;
        existing.ReviewNote = submission.ReviewNote;
        existing.Name = submission.Name;
        existing.Category = submission.Category;
        existing.Summary = submission.Summary;
        existing.Description = submission.Description;
        existing.Address = submission.Address;
        existing.Latitude = submission.Latitude;
        existing.Longitude = submission.Longitude;
        existing.Radius = submission.Radius;
        existing.ApproachRadiusMeters = submission.ApproachRadiusMeters;
        existing.Priority = submission.Priority;
        existing.DebounceSeconds = submission.DebounceSeconds;
        existing.CooldownSeconds = submission.CooldownSeconds;
        existing.TriggerMode = submission.TriggerMode;
        existing.ImageUrl = submission.ImageUrl;
        existing.MapUrl = submission.MapUrl;
        existing.IsActive = submission.IsActive;
        existing.AudioMode = submission.AudioMode;
        existing.AudioUrl = submission.AudioUrl;
        existing.TtsScript = submission.TtsScript;
        existing.DefaultLanguage = submission.DefaultLanguage;
        existing.EstimatedDurationSeconds = submission.EstimatedDurationSeconds;
        existing.SubmittedAt = submission.SubmittedAt;
        existing.UpdatedAt = submission.UpdatedAt;
        existing.ReviewedAt = null;
        existing.ReviewedByAdminId = null;

        await _context.SaveChangesAsync();

        TempData["Success"] = existing.Status == PoiSubmissionStatus.PendingReview
            ? "Đã cập nhật và gửi submission lên cho admin duyệt."
            : "Đã lưu bản nháp POI.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StartUpdate(int id)
    {
        var owner = await RequireApprovedOwnerAsync();
        if (owner == null)
        {
            return RedirectToAction(nameof(Index));
        }

        var poi = await _context.Pois.FirstOrDefaultAsync(x => x.Id == id && x.OwnerId == owner.Id);
        if (poi == null)
        {
            return NotFound();
        }

        var existingDraft = await _context.PoiSubmissions
            .FirstOrDefaultAsync(x =>
                x.OwnerId == owner.Id &&
                x.PoiId == id &&
                (x.Status == PoiSubmissionStatus.Draft || x.Status == PoiSubmissionStatus.ChangesRequested || x.Status == PoiSubmissionStatus.PendingReview));

        if (existingDraft != null)
        {
            TempData["Success"] = "Đã mở submission chỉnh sửa hiện có cho POI này.";
            return RedirectToAction(nameof(Edit), new { id = existingDraft.Id });
        }

        var draft = PoiWorkflowHelper.CreateSubmissionFromPoi(poi, owner.Id);
        _context.PoiSubmissions.Add(draft);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Đã tạo submission chỉnh sửa mới từ POI live.";
        return RedirectToAction(nameof(Edit), new { id = draft.Id });
    }

    private async Task PrepareSubmissionAsync(PoiSubmission submission, string ownerId, PoiSubmission? existing, string submitAction)
    {
        submission.OwnerId = ownerId;
        submission.AudioMode = PoiWorkflowHelper.NormalizeAudioMode(submission.AudioMode, submission.AudioUrl);

        if ((string.Equals(submission.AudioMode, "audio", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(submission.AudioMode, "audio-priority", StringComparison.OrdinalIgnoreCase)) &&
            string.IsNullOrWhiteSpace(submission.AudioUrl))
        {
            ModelState.AddModelError(nameof(submission.AudioUrl), "Chế độ audio cần có file audio URL.");
        }

        if (existing != null)
        {
            submission.ImageUrl = await PoiImageStorageHelper.SaveImageAsync(submission.ImageFile, submission.ImageUrl, _imageStorageOptions);
            submission.CreatedAt = existing.CreatedAt;
        }
        else
        {
            submission.Id = Guid.NewGuid().ToString("N");
            submission.ImageUrl = await PoiImageStorageHelper.SaveImageAsync(submission.ImageFile, submission.ImageUrl, _imageStorageOptions);
            submission.CreatedAt = DateTime.UtcNow;
        }

        submission.UpdatedAt = DateTime.UtcNow;
        submission.Status = string.Equals(submitAction, "submit", StringComparison.OrdinalIgnoreCase)
            ? PoiSubmissionStatus.PendingReview
            : PoiSubmissionStatus.Draft;
        submission.SubmittedAt = submission.Status == PoiSubmissionStatus.PendingReview ? DateTime.UtcNow : null;
        submission.ReviewedAt = null;
        submission.ReviewedByAdminId = null;
        submission.ReviewNote = existing?.Status == PoiSubmissionStatus.ChangesRequested ? existing.ReviewNote : string.Empty;
    }

    private async Task<ShopOwner?> GetCurrentOwnerAsync()
    {
        var ownerId = OwnerSessionHelper.GetOwnerId(HttpContext);
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            return null;
        }

        return await _context.ShopOwners.FirstOrDefaultAsync(x => x.Id == ownerId);
    }

    private async Task<ShopOwner?> RequireApprovedOwnerAsync()
    {
        var owner = await GetCurrentOwnerAsync();
        if (owner == null)
        {
            return null;
        }

        if (!string.Equals(owner.Status, ShopOwnerStatus.Approved, StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = owner.Status == ShopOwnerStatus.Pending
                ? "Tài khoản chủ quán của bạn vẫn đang chờ admin duyệt."
                : "Tài khoản chủ quán của bạn hiện đang bị tạm khóa.";
            return null;
        }

        return owner;
    }

    private async Task<List<SelectListItem>> BuildCategoryOptionsAsync(string? selected = null)
    {
        return await _context.Categories
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new SelectListItem(x.Name, x.Slug, x.Slug == selected))
            .ToListAsync();
    }

    private async Task<List<SelectListItem>> BuildLanguageOptionsAsync(string? selected = null)
    {
        return await _context.LanguageOptions
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new SelectListItem($"{x.Name} ({x.Code})", x.Code, x.Code == selected))
            .ToListAsync();
    }
}
