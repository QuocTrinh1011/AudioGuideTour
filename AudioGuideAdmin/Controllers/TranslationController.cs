using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AudioGuideAdmin.Data;
using AudioGuideAdmin.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Hosting;

namespace AudioGuideAdmin.Controllers
{
    public class TranslationController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public TranslationController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ================== LIST ==================
        public async Task<IActionResult> Index()
        {
            var data = await _context.PoiTranslations
                            .Include(x => x.Poi)
                            .ToListAsync();

            return View(data);
        }

        // ================== CREATE ==================
        public IActionResult Create()
        {
            ViewBag.Pois = new SelectList(_context.Pois, "Id", "Name");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(PoiTranslation model, IFormFile audioFile)
        {
            if (audioFile != null)
            {
                var folder = Path.Combine(_env.WebRootPath, "audio");

                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                var fileName = Guid.NewGuid() + Path.GetExtension(audioFile.FileName);
                var path = Path.Combine(folder, fileName);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await audioFile.CopyToAsync(stream);
                }

                model.AudioUrl = "/audio/" + fileName;

                var exists = await _context.PoiTranslations
    .AnyAsync(x => x.PoiId == model.PoiId && x.Language == model.Language);

                if (exists)
                {
                    ModelState.AddModelError("", "Ngôn ngữ này đã tồn tại cho POI này!");
                    ViewBag.Pois = new SelectList(_context.Pois, "Id", "Name");
                    return View(model);
                }
            }

            _context.PoiTranslations.Add(model);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // ================== EDIT ==================
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _context.PoiTranslations.FindAsync(id);
            if (item == null) return NotFound();

            ViewBag.Pois = new SelectList(_context.Pois, "Id", "Name", item.PoiId);

            return View(item);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(PoiTranslation model, IFormFile audioFile)
        {
            var existing = await _context.PoiTranslations.FindAsync(model.Id);
            if (existing == null) return NotFound();

            existing.PoiId = model.PoiId;
            existing.Language = model.Language;
            existing.Title = model.Title;
            existing.Description = model.Description;

            if (audioFile != null)
            {
                var folder = Path.Combine(_env.WebRootPath, "audio");

                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                var fileName = Guid.NewGuid() + Path.GetExtension(audioFile.FileName);
                var path = Path.Combine(folder, fileName);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await audioFile.CopyToAsync(stream);
                }

                existing.AudioUrl = "/audio/" + fileName;
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // ================== DELETE ==================
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.PoiTranslations.FindAsync(id);

            if (item != null)
            {
                _context.PoiTranslations.Remove(item);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }
    }
}