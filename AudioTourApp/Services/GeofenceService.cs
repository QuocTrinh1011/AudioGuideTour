using AudioTourApp.Models;
using Microsoft.Maui.Devices.Sensors;

namespace AudioTourApp.Services;

public class GeofenceService
{
    private List<POI> _pois = new();

    private Location _lastLocation;

    private double MIN_DISTANCE_CHANGE = 10; // mét
    private Dictionary<int, DateTime> _lastTriggered = new();

    private int COOLDOWN_SECONDS = 30;

    // 🔥 trạng thái POI (đang ở trong hay không)
    private HashSet<int> _insidePOIs = new();

    public event Action<POI> OnPOIEnter;
    public event Action<POI> OnPOIExit;
    public event Action<POI> OnPOINear;

    public void SetPOIs(List<POI> pois)
    {
        _pois = pois;
    }

    public void CheckLocation(Location userLocation)
    {
        // 🔥 DEBOUNCE
        if (_lastLocation != null)
        {
            double moved = Location.CalculateDistance(
                _lastLocation,
                userLocation,
                DistanceUnits.Kilometers
            ) * 1000;

            if (moved < MIN_DISTANCE_CHANGE)
                return;
        }

        _lastLocation = userLocation;

        foreach (var poi in _pois.OrderByDescending(p => p.Priority))
        {
            double distance = Location.CalculateDistance(
                userLocation,
                new Location(poi.Latitude, poi.Longitude),
                DistanceUnits.Kilometers
            ) * 1000;

            bool isInside = distance <= poi.RadiusMeters;
            bool isNear = distance <= poi.RadiusMeters + 30; // vùng gần (+30m)

            // 🔥 ENTER
            if (isInside && !_insidePOIs.Contains(poi.Id))
            {
                if (!IsInCooldown(poi.Id))
                {
                    _insidePOIs.Add(poi.Id);
                    _lastTriggered[poi.Id] = DateTime.Now;

                    OnPOIEnter?.Invoke(poi);
                }
            }

            // 🔥 EXIT
            if (!isInside && _insidePOIs.Contains(poi.Id))
            {
                _insidePOIs.Remove(poi.Id);
                OnPOIExit?.Invoke(poi);
            }

            // 🔥 NEAR (bonus)
            if (isNear && !_insidePOIs.Contains(poi.Id))
            {
                OnPOINear?.Invoke(poi);
            }
        }
    }

    private bool IsInCooldown(int poiId)
    {
        if (!_lastTriggered.ContainsKey(poiId))
            return false;

        var lastTime = _lastTriggered[poiId];

        return (DateTime.Now - lastTime).TotalSeconds < COOLDOWN_SECONDS;
    }
}