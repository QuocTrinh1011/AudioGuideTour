using AudioTourApp.Models;
using AudioTourApp.Pages;
using AudioTourApp.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace AudioTourApp.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private const string DefaultApiUrl = "http://10.0.2.2:5297";
    private const string PreferenceApiBaseUrl = "audio-tour-api-base-url";
    private const string PreferenceUserId = "audio-tour-user-id";
    private const string PreferenceDeviceId = "audio-tour-device-id";
    private const string PreferenceVisitorName = "audio-tour-visitor-name";
    private const string PreferenceVisitorLanguage = "audio-tour-visitor-language";
    private const string PreferenceAllowAutoPlay = "audio-tour-allow-auto-play";
    private const string PreferenceAllowBackground = "audio-tour-allow-background";

    private readonly ApiClient _apiClient;
    private readonly LocationTrackingService _locationService;
    private readonly AudioQueueService _audioQueueService;
    private readonly Dictionary<int, DateTime> _recentAutoTriggers = new();
    private readonly Dictionary<int, PoiGeofenceState> _poiGeofenceStates = new();

    private string _status = "San sang";
    private string _currentLocation = "Chua co vi tri";
    private string _mapHtml = "<html><body style='font-family:sans-serif;padding:20px'>Bam Bootstrap de tai du lieu ban do.</body></html>";
    private string _poiSearchText = "";
    private string _selectedCategoryFilter = "Tat ca";
    private bool _isTracking;
    private bool _isBusy;
    private string _apiBaseUrl;
    private string _qrCodeInput = "";
    private readonly string _userId;
    private readonly string _deviceId;
    private readonly List<PoiItem> _allPois = new();
    private Location? _latestLocation;
    private PoiItem? _selectedPoi;
    private LanguageItem? _selectedLanguage;
    private TourItem? _selectedTour;
    private TourItem? _activeTour;
    private int _activeTourStopIndex = -1;
    private string _visitorDisplayName;
    private bool _allowAutoPlay;
    private bool _allowBackgroundTracking;
    private bool _appIsForeground = true;
    private bool _isSyncingVisitor;

    public MainViewModel(ApiClient apiClient, LocationTrackingService locationService, AudioQueueService audioQueueService)
    {
        _apiClient = apiClient;
        _locationService = locationService;
        _audioQueueService = audioQueueService;
        _apiBaseUrl = Preferences.Default.Get(PreferenceApiBaseUrl, apiClient.BaseUrl);
        _userId = GetOrCreatePreference(PreferenceUserId, () => Guid.NewGuid().ToString("N"));
        _deviceId = GetOrCreatePreference(PreferenceDeviceId, () => $"{DeviceInfo.Current.Platform}-{Guid.NewGuid().ToString("N")[..8]}");
        _visitorDisplayName = Preferences.Default.Get(PreferenceVisitorName, "Khach an danh");
        _allowAutoPlay = Preferences.Default.Get(PreferenceAllowAutoPlay, true);
        _allowBackgroundTracking = Preferences.Default.Get(PreferenceAllowBackground, true);
        var savedLanguage = Preferences.Default.Get(PreferenceVisitorLanguage, "vi-VN");
        _selectedLanguage = new LanguageItem { Code = savedLanguage, NativeName = savedLanguage, Name = savedLanguage, Locale = savedLanguage };

        _locationService.LocationChanged += OnLocationChanged;
        _audioQueueService.StatusChanged += (_, status) => Status = status;
        _audioQueueService.QueueChanged += (_, _) =>
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PlaybackStatusText)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(QueueSummaryText)));
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<PoiItem> NearbyPois { get; } = new();
    public ObservableCollection<PoiItem> AllPois { get; } = new();
    public ObservableCollection<PoiItem> VisiblePois { get; } = new();
    public ObservableCollection<TourItem> Tours { get; } = new();
    public ObservableCollection<LanguageItem> Languages { get; } = new();
    public ObservableCollection<CategoryItem> Categories { get; } = new();
    public ObservableCollection<string> CategoryFilterOptions { get; } = new();

    public string ApiBaseUrl
    {
        get => _apiBaseUrl;
        set
        {
            if (SetField(ref _apiBaseUrl, value))
            {
                Preferences.Default.Set(PreferenceApiBaseUrl, value ?? string.Empty);
            }
        }
    }

    public string Status
    {
        get => _status;
        set => SetField(ref _status, value);
    }

    public string CurrentLocation
    {
        get => _currentLocation;
        set => SetField(ref _currentLocation, value);
    }

    public string PoiSearchText
    {
        get => _poiSearchText;
        set
        {
            if (SetField(ref _poiSearchText, value))
            {
                ApplyPoiFilters();
            }
        }
    }

    public string SelectedCategoryFilter
    {
        get => _selectedCategoryFilter;
        set
        {
            if (SetField(ref _selectedCategoryFilter, value))
            {
                ApplyPoiFilters();
            }
        }
    }

    public string MapHtml
    {
        get => _mapHtml;
        set => SetField(ref _mapHtml, value);
    }

    public bool IsTracking
    {
        get => _isTracking;
        set
        {
            if (SetField(ref _isTracking, value))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TrackingStatusText)));
            }
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => SetField(ref _isBusy, value);
    }

    public string QrCodeInput
    {
        get => _qrCodeInput;
        set => SetField(ref _qrCodeInput, value);
    }

    public string VisitorDisplayName
    {
        get => _visitorDisplayName;
        set
        {
            if (SetField(ref _visitorDisplayName, value))
            {
                Preferences.Default.Set(PreferenceVisitorName, value ?? string.Empty);
            }
        }
    }

    public bool AllowAutoPlay
    {
        get => _allowAutoPlay;
        set
        {
            if (SetField(ref _allowAutoPlay, value))
            {
                Preferences.Default.Set(PreferenceAllowAutoPlay, value);
            }
        }
    }

    public bool AllowBackgroundTracking
    {
        get => _allowBackgroundTracking;
        set
        {
            if (SetField(ref _allowBackgroundTracking, value))
            {
                Preferences.Default.Set(PreferenceAllowBackground, value);
            }
        }
    }

    public LanguageItem? SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            if (SetField(ref _selectedLanguage, value) && value != null)
            {
                Preferences.Default.Set(PreferenceVisitorLanguage, value.Code ?? "vi-VN");
            }
        }
    }

    public TourItem? SelectedTour
    {
        get => _selectedTour;
        set
        {
            if (SetField(ref _selectedTour, value))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasSelectedTour)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedTourStops)));
            }
        }
    }

    public PoiItem? SelectedPoi
    {
        get => _selectedPoi;
        set
        {
            if (SetField(ref _selectedPoi, value))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasSelectedPoi)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedPoiSubtitle)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedPoiNarrationText)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedPoiNarrationSourceText)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedPoiMetaText)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedPoiCoordinateText)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedPoiAudioText)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedPoiVoiceText)));
                RefreshMap();
            }
        }
    }

    public bool HasSelectedPoi => SelectedPoi != null;
    public bool HasSelectedTour => SelectedTour != null;
    public bool HasActiveTour => ActiveTour != null;
    public string TrackingStatusText => IsTracking ? "Dang bat" : "Dang tat";
    public string TrackingActionText => IsTracking ? "Tat tracking" : "Bat tracking";
    public string PlaybackStatusText => _audioQueueService.CurrentItem == null
        ? "Chua co audio dang phat"
        : $"Dang phat: {_audioQueueService.CurrentItem.Title}";
    public string QueueSummaryText => _audioQueueService.PendingItems.Count == 0
        ? "Hang doi rong"
        : $"Hang doi: {string.Join(", ", _audioQueueService.PendingItems.Select(x => x.Title))}";

    public string SelectedPoiSubtitle => SelectedPoi == null
        ? "Chon 1 POI gan day hoac quet QR de xem thong tin chi tiet."
        : $"{SelectedPoi.Language} | {SelectedPoi.DistanceMeters:F0}m | Priority {SelectedPoi.Priority}";
    public string SelectedPoiNarrationText => SelectedPoi == null
        ? "Chua co POI nao duoc chon."
        : !string.IsNullOrWhiteSpace(SelectedPoi.TtsScript)
            ? SelectedPoi.TtsScript
            : !string.IsNullOrWhiteSpace(SelectedPoi.Description)
                ? SelectedPoi.Description
                : (string.IsNullOrWhiteSpace(SelectedPoi.Summary) ? "POI nay chua co noi dung thuyet minh." : SelectedPoi.Summary);
    public string SelectedPoiNarrationSourceText => SelectedPoi == null
        ? "Chua co nguon thuyet minh."
        : !string.IsNullOrWhiteSpace(SelectedPoi.TtsScript)
            ? "Nguon doc: TTS Script tu admin"
            : !string.IsNullOrWhiteSpace(SelectedPoi.Description)
                ? "Nguon doc: Mo ta POI"
                : !string.IsNullOrWhiteSpace(SelectedPoi.Summary)
                    ? "Nguon doc: Tom tat POI"
                    : "POI nay chua co script doc.";
    public string SelectedPoiMetaText => SelectedPoi == null
        ? "Chon 1 POI de xem thong tin kich hoat."
        : $"{SelectedPoi.Category} | Trigger {SelectedPoi.TriggerMode} | Radius {SelectedPoi.Radius}m | Nearby {SelectedPoi.ApproachRadiusMeters}m";
    public string SelectedPoiCoordinateText => SelectedPoi == null
        ? "Chua co toa do POI."
        : $"{SelectedPoi.Latitude:F6}, {SelectedPoi.Longitude:F6}";
    public string SelectedPoiVoiceText => SelectedPoi == null
        ? "Chua co voice."
        : string.IsNullOrWhiteSpace(SelectedPoi.VoiceName)
            ? $"Ngon ngu doc: {SelectedPoi.Language}"
            : $"Voice uu tien: {SelectedPoi.VoiceName} | Ngon ngu: {SelectedPoi.Language}";
    public string SelectedPoiAudioText => SelectedPoi == null
        ? "Chua co thong tin audio."
        : string.IsNullOrWhiteSpace(SelectedPoi.AudioUrl)
            ? $"Che do: {SelectedPoi.AudioMode}. Dang uu tien TTS."
            : $"Che do: {SelectedPoi.AudioMode}. Co audio fallback.";
    public string VisiblePoisSummary => VisiblePois.Count == 0
        ? "Khong co POI nao khop bo loc hien tai."
        : $"Dang hien {VisiblePois.Count} diem thuyet minh.";
    public IReadOnlyList<TourStopItem> SelectedTourStops => SelectedTour?.Stops == null
        ? Array.Empty<TourStopItem>()
        : SelectedTour.Stops
            .OrderBy(x => x.SortOrder)
            .ToList();

    public TourItem? ActiveTour
    {
        get => _activeTour;
        private set
        {
            if (SetField(ref _activeTour, value))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasActiveTour)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ActiveTourStatus)));
            }
        }
    }

    public string ActiveTourStatus
    {
        get
        {
            if (ActiveTour == null)
            {
                return "Chua bat dau tour. Chon 1 tour de phat theo diem dung.";
            }

            var stops = ActiveTour.Stops ?? new List<TourStopItem>();
            var totalStops = stops.Count;
            var currentStop = totalStops == 0 || _activeTourStopIndex < 0 || _activeTourStopIndex >= totalStops
                ? null
                : stops[_activeTourStopIndex];

            return currentStop?.Poi == null
                ? $"Tour {ActiveTour.Name} chua co diem dung hop le."
                : $"Tour {ActiveTour.Name}: diem {_activeTourStopIndex + 1}/{totalStops} - {currentStop.Poi.Title}";
        }
    }

    public async Task BootstrapAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            var requestedLanguage = SelectedLanguage?.Code ?? "vi-VN";
            var previousSelectedPoiId = SelectedPoi?.Id;
            var previousSelectedTourId = SelectedTour?.Id;
            var previousActiveTourId = ActiveTour?.Id;
            Exception? lastError = null;

            foreach (var candidateUrl in GetApiBaseUrlCandidates())
            {
                try
                {
                    _apiClient.BaseUrl = candidateUrl;
                    var visitor = await SyncVisitorProfileAsync(requestedLanguage, cancellationToken, suppressError: true);
                    var effectiveLanguage = string.IsNullOrWhiteSpace(visitor?.Language) ? requestedLanguage : visitor.Language;
                    var bootstrap = await _apiClient.BootstrapAsync(effectiveLanguage, cancellationToken);

                    ApiBaseUrl = candidateUrl;
                    ReplaceCollection(Languages, bootstrap.Languages);
                    ReplaceCollection(Categories, bootstrap.Categories);
                    ReplaceCollection(Tours, bootstrap.Tours);
                    ResetCategoryFilterOptions();

                    _allPois.Clear();
                    _allPois.AddRange(bootstrap.Pois);
                    ApplyPoiFilters();

                    SelectedLanguage = Languages.FirstOrDefault(x => x.Code == bootstrap.RequestedLanguage)
                        ?? Languages.FirstOrDefault(x => x.Code == effectiveLanguage)
                        ?? Languages.FirstOrDefault(x => x.Code == requestedLanguage)
                        ?? Languages.FirstOrDefault();
                    SelectedTour = previousSelectedTourId.HasValue
                        ? Tours.FirstOrDefault(x => x.Id == previousSelectedTourId.Value) ?? Tours.FirstOrDefault()
                        : Tours.FirstOrDefault();

                    ActiveTour = previousActiveTourId.HasValue
                        ? Tours.FirstOrDefault(x => x.Id == previousActiveTourId.Value)
                        : null;

                    RefreshNearbyFromBootstrap();
                    if (previousSelectedPoiId.HasValue)
                    {
                        SelectedPoi = _allPois.FirstOrDefault(x => x.Id == previousSelectedPoiId.Value)
                            ?? NearbyPois.FirstOrDefault()
                            ?? _allPois.FirstOrDefault();
                    }
                    else
                    {
                        SelectedPoi ??= NearbyPois.FirstOrDefault() ?? _allPois.FirstOrDefault();
                    }
                    RefreshMap();
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(VisiblePoisSummary)));
                    Status = $"Da tai {bootstrap.Pois.Count} POI, {bootstrap.Tours.Count} tour va {bootstrap.Languages.Count} ngon ngu. Visitor: {VisitorDisplayName}.";
                    return;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                }
            }

            throw lastError ?? new InvalidOperationException("Khong the ket noi toi API.");
        }
        catch (Exception ex)
        {
            Status = $"Khong tai duoc du lieu bootstrap: {BuildConnectionHelpMessage(ex)}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task ChangeLanguageAsync()
    {
        if (SelectedLanguage != null)
        {
            Preferences.Default.Set(PreferenceVisitorLanguage, SelectedLanguage.Code);
        }

        await BootstrapAsync();
    }

    public async Task ToggleTrackingAsync()
    {
        if (IsTracking)
        {
            await _locationService.StopAsync();
            IsTracking = false;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TrackingActionText)));
            Status = "Da dung tracking.";
            return;
        }

        await BootstrapAsync();
        await _locationService.StartAsync(TimeSpan.FromSeconds(6));
        IsTracking = true;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TrackingActionText)));
        Status = AllowBackgroundTracking
            ? "Dang tracking GPS. Da xin quyen location va san sang geofence/auto narration."
            : "Dang tracking GPS o foreground. Admin hien dang tat background tracking cho visitor nay.";
    }

    public async Task LookupQrAsync(CancellationToken cancellationToken = default)
        => await LookupQrAsync(null, cancellationToken);

    public async Task LookupQrAsync(string? overrideCode, CancellationToken cancellationToken = default)
    {
        var code = string.IsNullOrWhiteSpace(overrideCode) ? QrCodeInput : overrideCode;
        if (string.IsNullOrWhiteSpace(code))
        {
            Status = "Nhap ma QR truoc khi mo noi dung.";
            return;
        }

        try
        {
            _apiClient.BaseUrl = ApiBaseUrl;
            var language = SelectedLanguage?.Code ?? "vi-VN";
            var normalizedCode = code.Trim();
            QrCodeInput = normalizedCode;
            var result = await _apiClient.LookupQrAsync(normalizedCode, language, cancellationToken);
            if (result?.Poi == null)
            {
                Status = $"Khong tim thay QR: {normalizedCode}.";
                return;
            }

            SelectedPoi = result.Poi;
            Status = $"QR {result.Code} da mo noi dung {result.Poi.Title}.";
            await _audioQueueService.EnqueueAsync(result.Poi, cancellationToken);
            RefreshMap();
        }
        catch (Exception ex)
        {
            Status = $"Khong mo duoc QR: {ex.Message}";
        }
    }

    public async Task PlaySelectedAsync(CancellationToken cancellationToken = default)
    {
        if (SelectedPoi == null)
        {
            Status = "Chua co POI de phat.";
            return;
        }

        await _audioQueueService.EnqueueAsync(SelectedPoi, cancellationToken);
    }

    public async Task StartSelectedTourAsync(CancellationToken cancellationToken = default)
    {
        if (SelectedTour == null)
        {
            Status = "Chon 1 tour truoc khi bat dau.";
            return;
        }

        var firstPlayableStopIndex = SelectedTour.Stops?
            .OrderBy(x => x.SortOrder)
            .Select((stop, index) => new { stop, index })
            .FirstOrDefault(x => x.stop.Poi != null)?.index ?? -1;

        if (firstPlayableStopIndex < 0)
        {
            Status = $"Tour {SelectedTour.Name} chua co diem dung hop le.";
            return;
        }

        ActiveTour = SelectedTour;
        _activeTourStopIndex = firstPlayableStopIndex;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ActiveTourStatus)));

        var stop = (ActiveTour.Stops ?? new List<TourStopItem>())
            .OrderBy(x => x.SortOrder)
            .ToList()[_activeTourStopIndex];
        if (stop.Poi != null)
        {
            SelectedPoi = stop.Poi;
            Status = $"Da bat dau tour {ActiveTour.Name}.";
            await _audioQueueService.EnqueueAsync(stop.Poi, cancellationToken);
            RefreshMap();
        }
    }

    public async Task PlayNextTourStopAsync(CancellationToken cancellationToken = default)
    {
        if (ActiveTour == null)
        {
            Status = "Chua co tour dang chay.";
            return;
        }

        var stops = ActiveTour.Stops
            .OrderBy(x => x.SortOrder)
            .Where(x => x.Poi != null)
            .ToList();

        if (stops.Count == 0)
        {
            Status = $"Tour {ActiveTour.Name} chua co diem dung hop le.";
            return;
        }

        if (_activeTourStopIndex + 1 >= stops.Count)
        {
            Status = $"Tour {ActiveTour.Name} da hoan thanh.";
            return;
        }

        _activeTourStopIndex++;
        var nextStop = stops[_activeTourStopIndex];
        if (nextStop.Poi != null)
        {
            SelectedPoi = nextStop.Poi;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ActiveTourStatus)));
            await _audioQueueService.EnqueueAsync(nextStop.Poi, cancellationToken);
            RefreshMap();
        }
    }

    public void OnAppForegroundChanged(bool isForeground)
    {
        _appIsForeground = isForeground;
        _locationService.SetForegroundState(isForeground);
        if (!isForeground)
        {
            _ = _audioQueueService.StopAsync();
            if (!AllowBackgroundTracking && IsTracking)
            {
                _ = MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await _locationService.StopAsync();
                    IsTracking = false;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TrackingActionText)));
                    Status = "Admin da tat background tracking cho visitor nay, nen app dung tracking khi chuyen sang nen.";
                });
            }
        }
    }

    public async Task SyncVisitorSettingsAsync(CancellationToken cancellationToken = default)
    {
        var visitor = await SyncVisitorProfileAsync(
            SelectedLanguage?.Code ?? Preferences.Default.Get(PreferenceVisitorLanguage, "vi-VN"),
            cancellationToken,
            suppressError: false);
        if (visitor != null)
        {
            Status = $"Da dong bo visitor {visitor.DisplayName}. AutoPlay: {(visitor.AllowAutoPlay ? "bat" : "tat")}, Background: {(visitor.AllowBackgroundTracking ? "bat" : "tat")}.";
        }
    }

    public async Task StopPlaybackAsync()
    {
        await _audioQueueService.StopAsync();
    }

    public void ResetApiBaseUrl()
    {
        ApiBaseUrl = DefaultApiUrl;
        _apiClient.BaseUrl = DefaultApiUrl;
        Status = "Da reset API URL ve dia chi emulator mac dinh.";
    }

    public async Task OpenSelectedMapAsync()
    {
        if (SelectedPoi == null || string.IsNullOrWhiteSpace(SelectedPoi.MapUrl))
        {
            Status = "POI nay chua co link ban do.";
            return;
        }

        await Launcher.Default.OpenAsync(SelectedPoi.MapUrl);
    }

    public async Task OpenSelectedPoiDetailsAsync()
    {
        if (SelectedPoi == null)
        {
            Status = "Chua co POI de xem chi tiet.";
            return;
        }

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            if (Shell.Current?.CurrentPage != null)
            {
                await Shell.Current.Navigation.PushAsync(new PoiDetailPage(this));
            }
        });
    }

    public void SelectPoiById(int poiId)
    {
        var poi = VisiblePois.FirstOrDefault(x => x.Id == poiId)
            ?? _allPois.FirstOrDefault(x => x.Id == poiId)
            ?? NearbyPois.FirstOrDefault(x => x.Id == poiId);

        if (poi == null)
        {
            Status = $"Khong tim thay POI co ID {poiId} tren ban do.";
            return;
        }

        SelectedPoi = poi;
        Status = $"Da chon {poi.Title} tu ban do.";
    }

    private async void OnLocationChanged(object? sender, Location location)
    {
        _latestLocation = location;
        CurrentLocation = $"{location.Latitude:F6}, {location.Longitude:F6} | acc {location.Accuracy:F1}m";

        var request = new LocationUpdateRequest
        {
            UserId = _userId,
            DeviceId = _deviceId,
            Language = SelectedLanguage?.Code ?? "vi-VN",
            Latitude = location.Latitude,
            Longitude = location.Longitude,
            Accuracy = location.Accuracy ?? 0,
            SpeedMetersPerSecond = location.Speed,
            Bearing = location.Course,
            IsForeground = _appIsForeground,
            RecordedAt = DateTime.UtcNow
        };

        try
        {
            await _apiClient.TrackAsync(request);

            var nearby = await _apiClient.GetNearbyAsync(request);
            ReplaceCollection(NearbyPois, nearby);
            ApplyNearbyRuntimeState(nearby);
            SelectedPoi ??= NearbyPois.FirstOrDefault();
            if (NearbyPois.FirstOrDefault(x => x.IsNearest) is { } nearest)
            {
                SelectedPoi = nearest;
            }

            var geofence = await _apiClient.CheckGeofenceAsync(request);
            if (geofence.ShouldPlay && geofence.TriggeredPoi != null)
            {
                if (CanAutoPlay(geofence.TriggeredPoi) && ShouldAutoPlayForTriggerMode(geofence.TriggeredPoi))
                {
                    SelectedPoi = geofence.TriggeredPoi;
                    await _audioQueueService.EnqueueAsync(geofence.TriggeredPoi);
                    _recentAutoTriggers[geofence.TriggeredPoi.Id] = DateTime.UtcNow;

                    if (ActiveTour != null)
                    {
                        TryAdvanceActiveTour(geofence.TriggeredPoi.Id);
                    }
                }
            }

            SeedLocalGeofenceStates(nearby, geofence.TriggeredPoi?.Id);

            RefreshMap();
        }
        catch (Exception ex)
        {
            Status = $"Loi ket noi API: {ex.Message}";
        }
    }

    private void RefreshNearbyFromBootstrap()
    {
        ReplaceCollection(NearbyPois, _allPois.OrderByDescending(x => x.Priority).ThenBy(x => x.Title).Take(8).ToList());
        SelectedPoi ??= NearbyPois.FirstOrDefault();
        ApplyPoiFilters();
    }

    private void ApplyNearbyRuntimeState(IEnumerable<PoiItem> nearby)
    {
        var nearbyById = nearby.ToDictionary(x => x.Id);
        foreach (var poi in _allPois)
        {
            if (nearbyById.TryGetValue(poi.Id, out var runtimePoi))
            {
                poi.DistanceMeters = runtimePoi.DistanceMeters;
                poi.IsNearest = runtimePoi.IsNearest;
            }
            else
            {
                poi.DistanceMeters = 0;
                poi.IsNearest = false;
            }
        }

        ApplyPoiFilters();
    }

    private void RefreshMap()
    {
        var centerLat = _latestLocation?.Latitude ?? (_allPois.FirstOrDefault()?.Latitude ?? 10.7618);
        var centerLng = _latestLocation?.Longitude ?? (_allPois.FirstOrDefault()?.Longitude ?? 106.6613);
        var nearestId = SelectedPoi?.Id ?? NearbyPois.FirstOrDefault(x => x.IsNearest)?.Id;
        var poisToRender = VisiblePois.Count > 0 ? VisiblePois.ToList() : _allPois.ToList();

        var html = new StringBuilder();
        html.Append("""
            <html>
            <head>
              <meta name="viewport" content="width=device-width, initial-scale=1.0" />
              <link rel="stylesheet" href="https://unpkg.com/leaflet/dist/leaflet.css" />
              <script src="https://unpkg.com/leaflet/dist/leaflet.js"></script>
              <style>
                html,body,#map{height:100%;margin:0;}
                .leaflet-popup-content{font-family:sans-serif;}
              </style>
            </head>
            <body><div id="map"></div><script>
        """);

        html.Append($"const map = L.map('map').setView([{centerLat.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {centerLng.ToString(System.Globalization.CultureInfo.InvariantCulture)}], 16);");
        html.Append("L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png').addTo(map);");

        if (_latestLocation != null)
        {
            html.Append($"L.circleMarker([{_latestLocation.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {_latestLocation.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}],{{radius:8,color:'#0d6efd',fillColor:'#0d6efd',fillOpacity:0.8}}).addTo(map).bindPopup('Vi tri cua ban');");
        }

        foreach (var poi in poisToRender)
        {
            var color = poi.Id == nearestId ? "#d9480f" : "#17324d";
            var radius = poi.Id == nearestId ? 10 : 7;
            html.Append(
                "(function() {" +
                $"const marker = L.circleMarker([{poi.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {poi.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}],{{radius:{radius},color:'{color}',fillColor:'{color}',fillOpacity:0.85}})" +
                ".addTo(map)" +
                $".bindPopup('<b>{Escape(poi.Title)}</b><br/>{Escape(poi.Summary)}');" +
                $"marker.on('click', function() {{ window.location.href = 'audiotour://poi/{poi.Id}'; }});" +
                "})();");
        }

        html.Append("</script></body></html>");
        MapHtml = html.ToString();
    }

    private static string Escape(string value) => (value ?? string.Empty).Replace("'", "\\'").Replace("\r", " ").Replace("\n", " ");

    private void ResetCategoryFilterOptions()
    {
        var options = new List<string> { "Tat ca" };
        options.AddRange(Categories
            .Select(x => x.Name)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x));
        ReplaceCollection(CategoryFilterOptions, options);

        if (!CategoryFilterOptions.Contains(SelectedCategoryFilter))
        {
            SelectedCategoryFilter = "Tat ca";
        }
    }

    private void ApplyPoiFilters()
    {
        IEnumerable<PoiItem> filtered = _allPois;

        if (!string.IsNullOrWhiteSpace(SelectedCategoryFilter) &&
            !SelectedCategoryFilter.Equals("Tat ca", StringComparison.OrdinalIgnoreCase))
        {
            filtered = filtered.Where(x => x.Category.Equals(SelectedCategoryFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(PoiSearchText))
        {
            var keyword = PoiSearchText.Trim();
            filtered = filtered.Where(x =>
                x.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                x.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                x.Category.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                x.Address.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                x.Summary.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        var ordered = filtered
            .OrderByDescending(x => x.IsNearest)
            .ThenByDescending(x => x.Priority)
            .ThenBy(x => x.Title)
            .ToList();

        ReplaceCollection(AllPois, ordered);
        ReplaceCollection(VisiblePois, ordered);
        if (SelectedPoi == null || ordered.All(x => x.Id != SelectedPoi.Id))
        {
            SelectedPoi = ordered.FirstOrDefault();
        }

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(VisiblePoisSummary)));
        RefreshMap();
    }

    private bool CanAutoPlay(PoiItem poi)
    {
        if (!AllowAutoPlay)
        {
            Status = $"Visitor nay dang bi tat auto-play tu admin, bo qua {poi.Title}.";
            return false;
        }

        if (_recentAutoTriggers.TryGetValue(poi.Id, out var lastTrigger))
        {
            var blockFor = Math.Max(1, Math.Max(poi.DebounceSeconds, poi.CooldownSeconds));
            if ((DateTime.UtcNow - lastTrigger).TotalSeconds < blockFor)
            {
                Status = $"Bo qua auto play {poi.Title} vi van trong thoi gian debounce/cooldown tren may.";
                return false;
            }
        }

        return true;
    }

    private bool ShouldAutoPlayForTriggerMode(PoiItem poi)
    {
        var key = poi.Id;
        var triggerRadius = Math.Max(1, poi.Radius);
        var nearbyRadius = Math.Max(triggerRadius, poi.ApproachRadiusMeters > 0 ? poi.ApproachRadiusMeters : triggerRadius);
        var currentInsideTrigger = poi.DistanceMeters > 0 && poi.DistanceMeters <= triggerRadius;
        var currentInsideNearby = poi.DistanceMeters > 0 && poi.DistanceMeters <= nearbyRadius;

        _poiGeofenceStates.TryGetValue(key, out var previous);

        var mode = (poi.TriggerMode ?? "both").Trim().ToLowerInvariant();
        var shouldPlay = mode switch
        {
            "manual" => false,
            "enter" => !previous.InsideTrigger && currentInsideTrigger,
            "nearby" => !previous.InsideNearby && currentInsideNearby,
            _ => (!previous.InsideTrigger && currentInsideTrigger) || (!previous.InsideNearby && currentInsideNearby)
        };

        _poiGeofenceStates[key] = new PoiGeofenceState(currentInsideTrigger, currentInsideNearby);

        if (!shouldPlay && mode != "manual")
        {
            Status = $"Bo qua {poi.Title} vi chua co chuyen trang thai moi cho trigger mode {poi.TriggerMode}.";
        }

        return shouldPlay;
    }

    private void SeedLocalGeofenceStates(IEnumerable<PoiItem> nearby, int? ignorePoiId)
    {
        foreach (var poi in nearby)
        {
            if (ignorePoiId.HasValue && poi.Id == ignorePoiId.Value)
            {
                continue;
            }

            var triggerRadius = Math.Max(1, poi.Radius);
            var nearbyRadius = Math.Max(triggerRadius, poi.ApproachRadiusMeters > 0 ? poi.ApproachRadiusMeters : triggerRadius);
            var insideTrigger = poi.DistanceMeters > 0 && poi.DistanceMeters <= triggerRadius;
            var insideNearby = poi.DistanceMeters > 0 && poi.DistanceMeters <= nearbyRadius;

            if (_poiGeofenceStates.TryGetValue(poi.Id, out var state))
            {
                _poiGeofenceStates[poi.Id] = state with
                {
                    InsideTrigger = insideTrigger,
                    InsideNearby = insideNearby
                };
            }
            else
            {
                _poiGeofenceStates[poi.Id] = new PoiGeofenceState(insideTrigger, insideNearby);
            }
        }
    }

    private async Task<VisitorProfile?> SyncVisitorProfileAsync(string language, CancellationToken cancellationToken, bool suppressError)
    {
        if (_isSyncingVisitor)
        {
            return null;
        }

        try
        {
            _isSyncingVisitor = true;
            var visitor = new VisitorProfile
            {
                Id = _userId,
                DeviceId = _deviceId,
                DisplayName = string.IsNullOrWhiteSpace(VisitorDisplayName) ? "Khach an danh" : VisitorDisplayName.Trim(),
                Language = string.IsNullOrWhiteSpace(language) ? "vi-VN" : language,
                AllowAutoPlay = AllowAutoPlay,
                AllowBackgroundTracking = AllowBackgroundTracking
            };

            var updated = await _apiClient.UpsertVisitorAsync(visitor, cancellationToken);
            ApplyVisitorProfile(updated);
            return updated;
        }
        catch (Exception ex)
        {
            if (!suppressError)
            {
                Status = $"Khong dong bo duoc visitor voi API: {BuildConnectionHelpMessage(ex)}";
            }

            return null;
        }
        finally
        {
            _isSyncingVisitor = false;
        }
    }

    private void ApplyVisitorProfile(VisitorProfile visitor)
    {
        if (visitor == null)
        {
            return;
        }

        VisitorDisplayName = string.IsNullOrWhiteSpace(visitor.DisplayName) ? "Khach an danh" : visitor.DisplayName;
        AllowAutoPlay = visitor.AllowAutoPlay;
        AllowBackgroundTracking = visitor.AllowBackgroundTracking;

        if (!string.IsNullOrWhiteSpace(visitor.Language))
        {
            Preferences.Default.Set(PreferenceVisitorLanguage, visitor.Language);
            if (Languages.Count > 0)
            {
                SelectedLanguage = Languages.FirstOrDefault(x => x.Code == visitor.Language)
                    ?? SelectedLanguage;
            }
        }
    }

    private static string GetOrCreatePreference(string key, Func<string> factory)
    {
        var existing = Preferences.Default.Get(key, string.Empty);
        if (!string.IsNullOrWhiteSpace(existing))
        {
            return existing;
        }

        var created = factory();
        Preferences.Default.Set(key, created);
        return created;
    }

    private void TryAdvanceActiveTour(int poiId)
    {
        if (ActiveTour == null)
        {
            return;
        }

        var orderedStops = ActiveTour.Stops
            .OrderBy(x => x.SortOrder)
            .Where(x => x.Poi != null)
            .ToList();

        var currentIndex = orderedStops.FindIndex(x => x.PoiId == poiId);
        if (currentIndex >= 0)
        {
            _activeTourStopIndex = currentIndex;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ActiveTourStatus)));
        }
    }

    private IEnumerable<string> GetApiBaseUrlCandidates()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in EnumerateCandidates())
        {
            yield return item;
        }

        IEnumerable<string> EnumerateCandidates()
        {
            if (!string.IsNullOrWhiteSpace(ApiBaseUrl))
            {
                var current = ApiBaseUrl.Trim().TrimEnd('/');
                if (seen.Add(current))
                {
                    yield return current;
                }

                if (current.Contains("localhost", StringComparison.OrdinalIgnoreCase))
                {
                    var emulatorHttp = current.Replace("localhost", "10.0.2.2", StringComparison.OrdinalIgnoreCase);
                    if (seen.Add(emulatorHttp))
                    {
                        yield return emulatorHttp;
                    }
                }

                if (current.Contains("10.0.2.2", StringComparison.OrdinalIgnoreCase) && current.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                {
                    var emulatorHttps = current.Replace("http://", "https://", StringComparison.OrdinalIgnoreCase)
                        .Replace(":5297", ":7114", StringComparison.OrdinalIgnoreCase);
                    if (seen.Add(emulatorHttps))
                    {
                        yield return emulatorHttps;
                    }
                }
            }

            if (seen.Add("http://10.0.2.2:5297"))
            {
                yield return DefaultApiUrl;
            }

            if (seen.Add("https://10.0.2.2:7114"))
            {
                yield return "https://10.0.2.2:7114";
            }

            if (seen.Add("http://192.168.1.10:5297"))
            {
                yield return "http://192.168.1.10:5297";
            }
        }
    }

    private static string BuildConnectionHelpMessage(Exception ex)
    {
        var message = ex.Message;

        if (message.Contains("Connection failure", StringComparison.OrdinalIgnoreCase) ||
            ex is HttpRequestException ||
            ex is TaskCanceledException)
        {
            return "Khong ket noi duoc toi API. Hay mo AudioGuideAPI truoc. Neu dang chay tren emulator, URL dung la http://10.0.2.2:5297; neu chay tren may that thi dung IP LAN cua may tinh.";
        }

        return message;
    }

    private static void ReplaceCollection<T>(ObservableCollection<T> collection, IEnumerable<T> items)
    {
        collection.Clear();
        foreach (var item in items)
        {
            collection.Add(item);
        }
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }

    private readonly record struct PoiGeofenceState(bool InsideTrigger, bool InsideNearby);
}
