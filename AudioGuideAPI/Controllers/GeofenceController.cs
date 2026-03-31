using AudioGuideAPI.Data;
using AudioGuideAPI.DTOs;
using AudioGuideAPI.Helpers;
using AudioGuideAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AudioGuideAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GeofenceController : ControllerBase
{
    private readonly AppDbContext _context;

    public GeofenceController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> CheckLocation(LocationRequest request)
    {
        var now = request.RecordedAt == default ? DateTime.UtcNow : request.RecordedAt.ToUniversalTime();
        var user = await EnsureUserAsync(request, now);

        var pois = await _context.Pois
            .AsNoTracking()
            .Include(x => x.Translations.Where(t => t.IsPublished))
            .Where(x => x.IsActive)
            .ToListAsync();

        var nearby = pois
            .Select(poi =>
            {
                var distance = GeoMath.DistanceInMeters(request.Latitude, request.Longitude, poi.Latitude, poi.Longitude);
                return new
                {
                    Poi = poi,
                    Distance = distance,
                    IsInside = distance <= poi.Radius,
                    IsApproaching = distance <= poi.ApproachRadiusMeters
                };
            })
            .Where(x => x.IsInside || x.IsApproaching)
            .OrderByDescending(x => x.IsInside)
            .ThenByDescending(x => x.Poi.Priority)
            .ThenBy(x => x.Distance)
            .ToList();

        if (nearby.Count == 0)
        {
            return Ok(new GeofenceCheckResponse
            {
                ShouldPlay = false,
                Reason = "outside-geofence"
            });
        }

        var response = new GeofenceCheckResponse
        {
            ShouldPlay = false,
            Reason = "nearby-only",
            NearbyPois = nearby.Take(5)
                .Select(x => MapPoi(x.Poi, request.Language, x.Distance))
                .ToList()
        };

        var candidate = nearby.FirstOrDefault(x => MatchesTriggerMode(x.Poi, x.IsInside, x.IsApproaching));
        if (candidate == null)
        {
            return Ok(response);
        }

        var lastTrigger = await _context.GeofenceTriggers
            .Where(x => x.UserId == user.Id && x.PoiId == candidate.Poi.Id)
            .OrderByDescending(x => x.RecordedAt)
            .FirstOrDefaultAsync();

        if (lastTrigger != null)
        {
            if (lastTrigger.CooldownUntil > now)
            {
                response.Reason = "cooldown";
                response.NextEligibleAt = lastTrigger.CooldownUntil;
                return Ok(response);
            }

            if ((now - lastTrigger.RecordedAt).TotalSeconds < candidate.Poi.DebounceSeconds)
            {
                response.Reason = "debounce";
                response.NextEligibleAt = lastTrigger.RecordedAt.AddSeconds(candidate.Poi.DebounceSeconds);
                return Ok(response);
            }
        }

        var trigger = new GeofenceTrigger
        {
            UserId = user.Id,
            PoiId = candidate.Poi.Id,
            Language = request.Language,
            TriggerType = candidate.IsInside ? "enter" : "nearby",
            Status = "triggered",
            DistanceMeters = candidate.Distance,
            RecordedAt = now,
            CooldownUntil = now.AddSeconds(Math.Max(candidate.Poi.CooldownSeconds, 1))
        };

        _context.GeofenceTriggers.Add(trigger);
        await _context.SaveChangesAsync();

        response.ShouldPlay = true;
        response.Reason = trigger.TriggerType;
        response.NextEligibleAt = trigger.CooldownUntil;
        response.TriggeredPoi = MapPoi(candidate.Poi, request.Language, candidate.Distance);

        return Ok(response);
    }

    private async Task<User> EnsureUserAsync(LocationRequest request, DateTime now)
    {
        User? user = null;

        if (!string.IsNullOrWhiteSpace(request.UserId))
        {
            user = await _context.Users.FirstOrDefaultAsync(x => x.Id == request.UserId);
        }

        if (user == null && !string.IsNullOrWhiteSpace(request.DeviceId))
        {
            user = await _context.Users.FirstOrDefaultAsync(x => x.DeviceId == request.DeviceId);
        }

        if (user == null)
        {
            user = new User
            {
                Id = string.IsNullOrWhiteSpace(request.UserId) ? Guid.NewGuid().ToString("N") : request.UserId,
                DeviceId = string.IsNullOrWhiteSpace(request.DeviceId) ? Guid.NewGuid().ToString("N") : request.DeviceId,
                Language = request.Language,
                LastSeenAt = now
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        user.Language = request.Language;
        user.LastSeenAt = now;
        await _context.SaveChangesAsync();
        return user;
    }

    private static bool MatchesTriggerMode(Poi poi, bool isInside, bool isApproaching)
    {
        return poi.TriggerMode.ToLowerInvariant() switch
        {
            "enter" => isInside,
            "nearby" => !isInside && isApproaching,
            "manual" => false,
            _ => isInside || isApproaching
        };
    }

    private static GeofencePoiResponse MapPoi(Poi poi, string language, double distance)
    {
        var translation = poi.Translations
            .FirstOrDefault(x => x.Language.Equals(language, StringComparison.OrdinalIgnoreCase))
            ?? poi.Translations.FirstOrDefault(x => x.Language.StartsWith(language.Split('-')[0], StringComparison.OrdinalIgnoreCase))
            ?? poi.Translations.FirstOrDefault();

        return new GeofencePoiResponse
        {
            Id = poi.Id,
            Name = poi.Name,
            Category = poi.Category,
            Title = translation?.Title ?? poi.Name,
            Language = translation?.Language ?? poi.DefaultLanguage,
            Summary = translation?.Summary ?? poi.Summary,
            Description = translation?.Description ?? poi.Description,
            Address = poi.Address,
            TtsScript = string.IsNullOrWhiteSpace(translation?.TtsScript) ? poi.TtsScript : translation.TtsScript,
            AudioUrl = string.IsNullOrWhiteSpace(translation?.AudioUrl) ? poi.AudioUrl : translation.AudioUrl,
            AudioMode = poi.AudioMode,
            VoiceName = translation?.VoiceName ?? string.Empty,
            ImageUrl = poi.ImageUrl,
            MapUrl = poi.MapUrl,
            TriggerMode = poi.TriggerMode,
            DistanceMeters = Math.Round(distance, 2),
            Priority = poi.Priority,
            Radius = poi.Radius,
            ApproachRadiusMeters = poi.ApproachRadiusMeters,
            CooldownSeconds = poi.CooldownSeconds,
            DebounceSeconds = poi.DebounceSeconds,
            EstimatedDurationSeconds = poi.EstimatedDurationSeconds
        };
    }
}
