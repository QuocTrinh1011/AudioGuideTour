using AudioTourApp.Models;
using AudioTourApp.Pages;
using AudioTourApp.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

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
    private const string PreferenceTrackingEnabled = "audio-tour-tracking-enabled";
    private static readonly TimeSpan TrackingInterval = TimeSpan.FromSeconds(6);

    private readonly ApiClient _apiClient;
    private readonly LocationTrackingService _locationService;
    private readonly AudioQueueService _audioQueueService;
    private readonly AppPermissionService _permissionService;
    private readonly NarrationService _narrationService;
    private readonly Dictionary<int, DateTime> _recentAutoTriggers = new();
    private readonly Dictionary<int, PoiGeofenceState> _poiGeofenceStates = new();

    private string _status = "Sẵn sàng";
    private string _currentLocation = "Chưa có vị trí";
    private string _mapHtml = "<html><body style='font-family:sans-serif;padding:20px'>Bấm Bootstrap để tải dữ liệu bản đồ.</body></html>";
    private string _poiSearchText = "";
    private string _selectedCategoryFilter = "Tất cả";
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
    private bool _isRestoringTracking;
    private bool _canUseCamera;
    private string _locationPermissionText = "Chưa kiểm tra";
    private string _backgroundPermissionText = "Chưa kiểm tra";
    private string _cameraPermissionText = "Chưa kiểm tra";
    private string _notificationPermissionText = "Chưa kiểm tra";

    public MainViewModel(ApiClient apiClient, LocationTrackingService locationService, AudioQueueService audioQueueService, AppPermissionService permissionService, NarrationService narrationService)
    {
        _apiClient = apiClient;
        _locationService = locationService;
        _audioQueueService = audioQueueService;
        _permissionService = permissionService;
        _narrationService = narrationService;
        _apiBaseUrl = Preferences.Default.Get(PreferenceApiBaseUrl, apiClient.BaseUrl);
        _userId = GetOrCreatePreference(PreferenceUserId, () => Guid.NewGuid().ToString("N"));
        _deviceId = GetOrCreatePreference(PreferenceDeviceId, () => $"{DeviceInfo.Current.Platform}-{Guid.NewGuid().ToString("N")[..8]}");
        _visitorDisplayName = Preferences.Default.Get(PreferenceVisitorName, "Khách ẩn danh");
        _allowAutoPlay = Preferences.Default.Get(PreferenceAllowAutoPlay, true);
        _allowBackgroundTracking = Preferences.Default.Get(PreferenceAllowBackground, true);
        var savedLanguage = Preferences.Default.Get(PreferenceVisitorLanguage, "vi-VN");
        _selectedLanguage = new LanguageItem { Code = savedLanguage, NativeName = savedLanguage, Name = savedLanguage, Locale = savedLanguage };
        _isTracking = _locationService.IsRunning;

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
    public ObservableCollection<QrLookupHistoryItem> RecentQrLookups { get; } = new();
    public ObservableCollection<string> AudioDiagnostics { get; } = new();

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
        set
        {
            if (SetField(ref _status, value))
            {
                PushAudioDiagnostic(value);
            }
        }
    }

    public string CurrentLocation
    {
        get => _currentLocation;
        set => SetField(ref _currentLocation, value);
    }

    public string LocationPermissionText
    {
        get => _locationPermissionText;
        set => SetField(ref _locationPermissionText, value);
    }

    public string BackgroundPermissionText
    {
        get => _backgroundPermissionText;
        set => SetField(ref _backgroundPermissionText, value);
    }

    public string CameraPermissionText
    {
        get => _cameraPermissionText;
        set => SetField(ref _cameraPermissionText, value);
    }

    public bool CanUseCamera
    {
        get => _canUseCamera;
        set => SetField(ref _canUseCamera, value);
    }

    public string NotificationPermissionText
    {
        get => _notificationPermissionText;
        set => SetField(ref _notificationPermissionText, value);
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
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedLanguageDisplayText)));
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
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedPoiPositionText)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NearestPoiSummaryText)));
                RefreshMap();
            }
        }
    }

    public bool HasSelectedPoi => SelectedPoi != null;
    public bool HasSelectedTour => SelectedTour != null;
    public bool HasActiveTour => ActiveTour != null;
    public string SelectedLanguageDisplayText => SelectedLanguage == null
        ? "Chưa chọn ngôn ngữ"
        : $"{SelectedLanguage.NativeName} ({SelectedLanguage.Code})";
    public string TrackingStatusText => IsTracking ? "Đang bật" : "Đang tắt";
    public string TrackingActionText => IsTracking ? "Tắt tracking" : "Bật tracking";
    public string PlaybackStatusText => _audioQueueService.CurrentItem == null
        ? "Chưa có audio đang phát"
        : $"Đang phat: {_audioQueueService.CurrentItem.Title}";
    public string QueueSummaryText => _audioQueueService.PendingItems.Count == 0
        ? "Hang doi rong"
        : $"Hang doi: {string.Join(", ", _audioQueueService.PendingItems.Select(x => x.Title))}";

    public string SelectedPoiSubtitle => SelectedPoi == null
        ? "Chọn 1 POI gần đây hoặc quét QR để xem thông tin chi tiết."
        : $"{SelectedPoi.Language} | {SelectedPoi.DistanceMeters:F0}m | Priority {SelectedPoi.Priority}";
    public string SelectedPoiNarrationText => SelectedPoi == null
        ? "Chưa có POI nào được chọn."
        : !string.IsNullOrWhiteSpace(SelectedPoi.TtsScript)
            ? SelectedPoi.TtsScript
            : !string.IsNullOrWhiteSpace(SelectedPoi.Description)
                ? SelectedPoi.Description
                : (string.IsNullOrWhiteSpace(SelectedPoi.Summary) ? "POI này chưa có nội dung thuyết minh." : SelectedPoi.Summary);
    public string SelectedPoiNarrationSourceText => SelectedPoi == null
        ? "Chưa có nguồn thuyết minh."
        : !string.IsNullOrWhiteSpace(SelectedPoi.TtsScript)
            ? "Nguồn đọc: TTS Script từ admin"
            : !string.IsNullOrWhiteSpace(SelectedPoi.Description)
                ? "Nguồn đọc: Mô tả POI"
                : !string.IsNullOrWhiteSpace(SelectedPoi.Summary)
                    ? "Nguồn đọc: Tóm tắt POI"
                    : "POI này chưa có script đọc.";
    public string SelectedPoiMetaText => SelectedPoi == null
        ? "Chọn 1 POI để xem thông tin kích hoạt."
        : $"{ResolveCategoryDisplayName(SelectedPoi.Category)} | Trigger {SelectedPoi.TriggerMode} | Radius {SelectedPoi.Radius}m | Nearby {SelectedPoi.ApproachRadiusMeters}m";
    public string SelectedPoiCoordinateText => SelectedPoi == null
        ? "Chưa có tọa độ POI."
        : $"{SelectedPoi.Latitude:F6}, {SelectedPoi.Longitude:F6}";
    public string SelectedPoiVoiceText => SelectedPoi == null
        ? "Chưa có voice."
        : string.IsNullOrWhiteSpace(SelectedPoi.VoiceName)
            ? $"Ngôn ngữ đọc: {SelectedPoi.Language}"
            : $"Voice ưu tiên: {SelectedPoi.VoiceName} | Ngôn ngữ: {SelectedPoi.Language}";
    public string SelectedPoiAudioText => SelectedPoi == null
        ? "Chưa có thông tin audio."
        : string.IsNullOrWhiteSpace(SelectedPoi.AudioUrl)
            ? $"Chế độ: {SelectedPoi.AudioMode}. Đang ưu tiên TTS."
            : $"Chế độ: {SelectedPoi.AudioMode}. Có audio fallback.";
    public string SelectedPoiPositionText
    {
        get
        {
            var source = GetPoiNavigationSource();
            if (SelectedPoi == null || source.Count == 0)
            {
                return "Chưa có vị trí POI trong danh sách.";
            }

            var index = source.FindIndex(x => x.Id == SelectedPoi.Id);
            return index < 0
                ? "POI này chưa nằm trong danh sách hiện tại."
                : $"POI {index + 1}/{source.Count} trong danh sach hien tai.";
        }
    }
    public string NearestPoiSummaryText
    {
        get
        {
            var nearest = VisiblePois.FirstOrDefault(x => x.IsNearest)
                ?? _allPois.Where(x => x.DistanceMeters > 0).OrderBy(x => x.DistanceMeters).FirstOrDefault()
                ?? NearbyPois.FirstOrDefault();

            return nearest == null
                ? "Chưa xác định được POI gần nhất."
                : $"Gần nhất: {nearest.Title} ({nearest.DistanceMeters:F0}m)";
        }
    }
    public string VisiblePoisSummary => VisiblePois.Count == 0
        ? "Không có POI nào khớp bộ lọc hiện tại."
        : $"Đang hiện {VisiblePois.Count} điểm thuyết minh.";
    public string RecentQrSummary => RecentQrLookups.Count == 0
        ? "Chưa có QR nào được mở trong phiên này."
        : $"Đã mở {RecentQrLookups.Count} QR gần đây.";
    public string AudioDiagnosticsSummary => AudioDiagnostics.Count == 0
        ? "Chưa có log chẩn đoán audio."
        : string.Join(Environment.NewLine, AudioDiagnostics.Take(6));
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
                return "Chưa bắt đầu tour. Chọn 1 tour để phát theo điểm dừng.";
            }

            var stops = ActiveTour.Stops ?? new List<TourStopItem>();
            var totalStops = stops.Count;
            var currentStop = totalStops == 0 || _activeTourStopIndex < 0 || _activeTourStopIndex >= totalStops
                ? null
                : stops[_activeTourStopIndex];

            return currentStop?.Poi == null
                ? $"Tour {ActiveTour.Name} chưa có điểm dừng hợp lệ."
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
                    NormalizePoiCategories(bootstrap.Pois);
                    NormalizeTourCategories(bootstrap.Tours);
                    ReplaceCollection(Tours, bootstrap.Tours);
                    ResetCategoryFilterOptions();

                    _allPois.Clear();
                    _allPois.AddRange(bootstrap.Pois);
                    ApplyPoiFilters();

                    SelectedLanguage = Languages.FirstOrDefault(x => x.Code == bootstrap.RequestedLanguage)
                        ?? Languages.FirstOrDefault(x => x.Code == effectiveLanguage)
                        ?? Languages.FirstOrDefault(x => x.Code == requestedLanguage)
                        ?? Languages.FirstOrDefault();
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedLanguageDisplayText)));
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
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NearestPoiSummaryText)));
                    Status = $"Đã tải {bootstrap.Pois.Count} POI, {bootstrap.Tours.Count} tour và {bootstrap.Languages.Count} ngôn ngữ. Visitor: {VisitorDisplayName}.";
                    await RefreshPermissionStatusAsync();
                    await TryRestoreTrackingAsync(cancellationToken);
                    return;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                }
            }

            throw lastError ?? new InvalidOperationException("Không thể kết nối tới API.");
        }
        catch (Exception ex)
        {
            Status = $"Không tải được dữ liệu bootstrap: {BuildConnectionHelpMessage(ex)}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task RefreshPermissionStatusAsync()
    {
        var snapshot = await _permissionService.GetStatusAsync();
        ApplyPermissionSnapshot(snapshot);
    }

    public async Task RequestTrackingPermissionsAsync()
    {
        var snapshot = await _permissionService.RequestTrackingPermissionsAsync();
        ApplyPermissionSnapshot(snapshot);
        Status = "Đã cập nhật quyền GPS/background/notification trên app.";
    }

    public async Task RequestCameraPermissionAsync()
    {
        var snapshot = await _permissionService.RequestCameraPermissionAsync();
        ApplyPermissionSnapshot(snapshot);
        Status = "Đã cập nhật quyền camera trên app.";
    }

    public async Task OpenSystemSettingsAsync()
    {
        await _permissionService.OpenSystemSettingsAsync();
        Status = "Đã mở cài đặt hệ thống để bạn cấp quyền thủ công nếu cần.";
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
            await StopTrackingAsync("Đã dừng tracking.", clearPreference: true);
            return;
        }

        await BootstrapAsync();
        await StartTrackingInternalAsync(isRestore: false);
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
                Status = $"Không tìm thấy QR: {normalizedCode}.";
                return;
            }

            NormalizePoiCategory(result.Poi);
            SelectedPoi = result.Poi;
            PushRecentQr(result);
            Status = $"QR {result.Code} da mo noi dung {result.Poi.Title}.";
            await _audioQueueService.EnqueueAsync(CreatePlaybackRequest(result.Poi, "qr", false), cancellationToken);
            RefreshMap();
        }
        catch (Exception ex)
        {
            Status = $"Không mở được QR: {ex.Message}";
        }
    }

    public async Task PlaySelectedAsync(CancellationToken cancellationToken = default)
    {
        if (SelectedPoi == null)
        {
            Status = "Chưa có POI để phát.";
            return;
        }

        PushAudioDiagnostic($"Yêu cầu phát: {SelectedPoi.Title} | Lang {SelectedPoi.Language} | Mode {SelectedPoi.AudioMode} | AudioUrl {(string.IsNullOrWhiteSpace(SelectedPoi.AudioUrl) ? "không có" : SelectedPoi.AudioUrl)}");
        await _audioQueueService.EnqueueAsync(CreatePlaybackRequest(SelectedPoi, "manual", false), cancellationToken);
    }

    public async Task RunSelectedPoiAudioDiagnosticsAsync(CancellationToken cancellationToken = default)
    {
        if (SelectedPoi == null)
        {
            Status = "Chưa có POI để chẩn đoán audio.";
            return;
        }

        try
        {
            _apiClient.BaseUrl = ApiBaseUrl;
            PushAudioDiagnostic($"Chan doan POI: {SelectedPoi.Title}");
            PushAudioDiagnostic($"Ngôn ngữ: {SelectedPoi.Language} | Voice: {(string.IsNullOrWhiteSpace(SelectedPoi.VoiceName) ? "mac dinh" : SelectedPoi.VoiceName)} | AudioMode: {SelectedPoi.AudioMode}");

            if (!string.IsNullOrWhiteSpace(SelectedPoi.AudioUrl))
            {
                var probe = await _apiClient.ProbeUrlAsync(SelectedPoi.AudioUrl, cancellationToken);
                PushAudioDiagnostic($"Audio URL OK? HTTP {probe.StatusCode} | {probe.ContentType} | {FormatLength(probe.ContentLength)}");
            }
            else
            {
                PushAudioDiagnostic("Audio URL: không có file audio, app sẽ dùng TTS.");
            }

            var hasNarrationText = !string.IsNullOrWhiteSpace(SelectedPoi.TtsScript) ||
                                   !string.IsNullOrWhiteSpace(SelectedPoi.Description) ||
                                   !string.IsNullOrWhiteSpace(SelectedPoi.Summary);
            PushAudioDiagnostic(hasNarrationText
                ? "Nội dung TTS: có script/mô tả/tóm tắt để đọc."
                : "Nội dung TTS: không có nội dung để đọc.");

            var ttsDiagnostic = await _narrationService.GetDiagnosticsAsync(SelectedPoi.Language, SelectedPoi.VoiceName, cancellationToken);
            PushAudioDiagnostic(ttsDiagnostic);
            Status = $"Đã chạy chẩn đoán audio cho {SelectedPoi.Title}.";
        }
        catch (Exception ex)
        {
            Status = $"Chan doan audio that bai: {BuildConnectionHelpMessage(ex)}";
        }
    }

    public async Task StartSelectedTourAsync(CancellationToken cancellationToken = default)
    {
        if (SelectedTour == null)
        {
            Status = "Chon 1 tour truoc khi bật dau.";
            return;
        }

        var firstPlayableStopIndex = SelectedTour.Stops?
            .OrderBy(x => x.SortOrder)
            .Select((stop, index) => new { stop, index })
            .FirstOrDefault(x => x.stop.Poi != null)?.index ?? -1;

        if (firstPlayableStopIndex < 0)
        {
            Status = $"Tour {SelectedTour.Name} chưa có điểm dừng hợp lệ.";
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
            Status = $"Đã bắt đầu tour {ActiveTour.Name}.";
            await _audioQueueService.EnqueueAsync(CreatePlaybackRequest(stop.Poi, "tour", false), cancellationToken);
            RefreshMap();
        }
    }

    public async Task PlayNextTourStopAsync(CancellationToken cancellationToken = default)
    {
        if (ActiveTour == null)
        {
            Status = "Chưa có tour đang chạy.";
            return;
        }

        var stops = ActiveTour.Stops
            .OrderBy(x => x.SortOrder)
            .Where(x => x.Poi != null)
            .ToList();

        if (stops.Count == 0)
        {
            Status = $"Tour {ActiveTour.Name} chưa có điểm dừng hợp lệ.";
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
            await _audioQueueService.EnqueueAsync(CreatePlaybackRequest(nextStop.Poi, "tour", false), cancellationToken);
            RefreshMap();
        }
    }

    public void OnAppForegroundChanged(bool isForeground)
    {
        _appIsForeground = isForeground;
        _locationService.SetForegroundState(isForeground);
        if (isForeground)
        {
            if (Preferences.Default.Get(PreferenceTrackingEnabled, false) && !IsTracking)
            {
                _ = MainThread.InvokeOnMainThreadAsync(async () => await TryRestoreTrackingAsync());
            }

            return;
        }

        if (!AllowBackgroundTracking && IsTracking)
        {
            _ = MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await StopTrackingAsync("Admin da tắt background tracking cho visitor nay, nen app dung tracking khi chuyen sang nen.", clearPreference: true);
            });
        }
    }

    public async Task TryRestoreTrackingAsync(CancellationToken cancellationToken = default)
    {
        if (_isRestoringTracking)
        {
            return;
        }

        if (_locationService.IsRunning)
        {
            IsTracking = true;
            return;
        }

        if (IsTracking)
        {
            IsTracking = false;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TrackingActionText)));
        }

        if (!Preferences.Default.Get(PreferenceTrackingEnabled, false))
        {
            return;
        }

        try
        {
            _isRestoringTracking = true;
            await RefreshPermissionStatusAsync();
            await StartTrackingInternalAsync(isRestore: true, cancellationToken);
        }
        catch (Exception ex)
        {
            IsTracking = false;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TrackingActionText)));
            Status = $"Không thể khôi phục tracking tự động: {BuildConnectionHelpMessage(ex)}";
        }
        finally
        {
            _isRestoringTracking = false;
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
            Status = $"Đã đồng bộ visitor {visitor.DisplayName}. AutoPlay: {(visitor.AllowAutoPlay ? "bật" : "tắt")}, Background: {(visitor.AllowBackgroundTracking ? "bật" : "tắt")}.";
        }
    }

    public async Task StopPlaybackAsync()
    {
        await _audioQueueService.StopAsync();
    }

    public async Task PasteQrFromClipboardAsync()
    {
        try
        {
            var text = await Clipboard.Default.GetTextAsync();
            if (string.IsNullOrWhiteSpace(text))
            {
                Status = "Clipboard hiện không có mã QR để dán.";
                return;
            }

            QrCodeInput = text.Trim();
            Status = $"Đã dán QR: {QrCodeInput}.";
        }
        catch (Exception ex)
        {
            Status = $"Không đọc được clipboard: {ex.Message}";
        }
    }

    public void OpenRecentQr(QrLookupHistoryItem? item)
    {
        if (item == null)
        {
            Status = "Chưa có QR lịch sử để mở lại.";
            return;
        }

        QrCodeInput = item.Code;
        var poi = _allPois.FirstOrDefault(x => x.Title.Equals(item.PoiTitle, StringComparison.OrdinalIgnoreCase))
            ?? VisiblePois.FirstOrDefault(x => x.Title.Equals(item.PoiTitle, StringComparison.OrdinalIgnoreCase))
            ?? NearbyPois.FirstOrDefault(x => x.Title.Equals(item.PoiTitle, StringComparison.OrdinalIgnoreCase));

        if (poi != null)
        {
            SelectedPoi = poi;
            Status = $"Đã mở lại QR {item.Code} cho {poi.Title}.";
        }
        else
        {
            Status = $"Đã chọn lại QR {item.Code}. Bấm Mở QR để tải nội dung mới nhất.";
        }
    }

    public void ResetApiBaseUrl()
    {
        ApiBaseUrl = DefaultApiUrl;
        _apiClient.BaseUrl = DefaultApiUrl;
        Status = "Đã reset API URL về địa chỉ emulator mặc định.";
    }

    public async Task OpenSelectedMapAsync()
    {
        if (SelectedPoi == null)
        {
            Status = "Chưa có POI để mở bản đồ.";
            return;
        }

        var mapUrl = SelectedPoi.MapUrl;
        if (string.IsNullOrWhiteSpace(mapUrl) && SelectedPoi.Latitude != 0 && SelectedPoi.Longitude != 0)
        {
            mapUrl = $"https://www.google.com/maps/search/?api=1&query={SelectedPoi.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)},{SelectedPoi.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
        }

        if (string.IsNullOrWhiteSpace(mapUrl))
        {
            Status = "POI này chưa có link bản đồ.";
            return;
        }

        await Launcher.Default.OpenAsync(mapUrl);
    }

    public async Task OpenSelectedPoiDetailsAsync()
    {
        if (SelectedPoi == null)
        {
            Status = "Chưa có POI để xem chi tiết.";
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

    public async Task OpenSelectedNarrationAsync()
    {
        if (SelectedPoi == null)
        {
            Status = "Chưa có POI để xem bản thuyết minh.";
            return;
        }

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            if (Shell.Current?.CurrentPage != null)
            {
                await Shell.Current.Navigation.PushAsync(new NarrationPage(this));
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
            Status = $"Không tìm thấy POI có ID {poiId} trên bản đồ.";
            return;
        }

        SelectedPoi = poi;
        Status = $"Đã chọn {poi.Title} từ bản đồ.";
    }

    public void SelectNearestPoi()
    {
        var nearest = VisiblePois.FirstOrDefault(x => x.IsNearest)
            ?? _allPois.Where(x => x.DistanceMeters > 0).OrderBy(x => x.DistanceMeters).FirstOrDefault()
            ?? NearbyPois.FirstOrDefault();

        if (nearest == null)
        {
            Status = "Chưa có POI gần nhất để chọn.";
            return;
        }

        SelectedPoi = nearest;
        Status = $"Đã chọn POI gần nhất: {nearest.Title}.";
    }

    public void ResetPoiFilters()
    {
        PoiSearchText = string.Empty;
        SelectedCategoryFilter = "Tất cả";
        ApplyPoiFilters();
        SelectNearestPoi();
    }

    public void SelectNextPoi()
    {
        var source = GetPoiNavigationSource();
        if (source.Count == 0)
        {
            Status = "Không có POI nào để chuyển tiếp.";
            return;
        }

        if (SelectedPoi == null)
        {
            SelectedPoi = source.FirstOrDefault();
            return;
        }

        var currentIndex = source.FindIndex(x => x.Id == SelectedPoi.Id);
        var nextIndex = currentIndex < 0 ? 0 : (currentIndex + 1) % source.Count;
        SelectedPoi = source[nextIndex];
        Status = $"Đã chuyển sang POI tiếp theo: {SelectedPoi?.Title}.";
    }

    public void SelectPreviousPoi()
    {
        var source = GetPoiNavigationSource();
        if (source.Count == 0)
        {
            Status = "Không có POI nào để quay lại.";
            return;
        }

        if (SelectedPoi == null)
        {
            SelectedPoi = source.FirstOrDefault();
            return;
        }

        var currentIndex = source.FindIndex(x => x.Id == SelectedPoi.Id);
        var previousIndex = currentIndex <= 0 ? source.Count - 1 : currentIndex - 1;
        SelectedPoi = source[previousIndex];
        Status = $"Đã quay về POI trước: {SelectedPoi?.Title}.";
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
            NormalizePoiCategories(nearby);
            ReplaceCollection(NearbyPois, nearby);
            ApplyNearbyRuntimeState(nearby);
            SelectedPoi ??= NearbyPois.FirstOrDefault();
            if (NearbyPois.FirstOrDefault(x => x.IsNearest) is { } nearest)
            {
                SelectedPoi = nearest;
            }

            var geofence = await _apiClient.CheckGeofenceAsync(request);
            NormalizePoiCategories(geofence.NearbyPois);
            if (geofence.TriggeredPoi != null)
            {
                NormalizePoiCategory(geofence.TriggeredPoi);
            }
            if (geofence.ShouldPlay && geofence.TriggeredPoi != null)
            {
                if (CanAutoPlay(geofence.TriggeredPoi) && ShouldAutoPlayForTriggerMode(geofence.TriggeredPoi))
                {
                    SelectedPoi = geofence.TriggeredPoi;
                    await _audioQueueService.EnqueueAsync(
                        CreatePlaybackRequest(
                            geofence.TriggeredPoi,
                            string.IsNullOrWhiteSpace(geofence.Reason) ? "geofence" : geofence.Reason,
                            true));
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
            Status = $"Lỗi kết nối API: {ex.Message}";
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
        var nearestId = _allPois.Where(x => x.DistanceMeters > 0).OrderBy(x => x.DistanceMeters).FirstOrDefault()?.Id
            ?? NearbyPois.FirstOrDefault(x => x.IsNearest)?.Id;
        var selectedId = SelectedPoi?.Id;
        var visiblePoiIds = VisiblePois.Select(x => x.Id).ToHashSet();
        var poisToRender = _allPois.Count > 0 ? _allPois.ToList() : VisiblePois.ToList();
        var selectedTitle = SelectedPoi?.Title ?? "Chưa chọn POI";
        var nearestTitle = _allPois.Where(x => x.Id == nearestId).Select(x => x.Title).FirstOrDefault()
            ?? NearbyPois.FirstOrDefault(x => x.Id == nearestId)?.Title
            ?? "Chưa xác định";
        var activeTourStops = (ActiveTour?.Stops ?? new List<TourStopItem>())
            .Where(x => x.Poi != null)
            .OrderBy(x => x.SortOrder)
            .Select((stop, index) => new
            {
                stop.PoiId,
                stop.SortOrder,
                Title = stop.Poi!.Title,
                Latitude = stop.Poi.Latitude,
                Longitude = stop.Poi.Longitude,
                IsCurrent = index == _activeTourStopIndex
            })
            .ToList();

        var poisJson = JsonSerializer.Serialize(poisToRender.Select(poi => new
        {
            poi.Id,
            poi.Title,
            poi.Summary,
            poi.Category,
            poi.TriggerMode,
            poi.Radius,
            poi.ApproachRadiusMeters,
            poi.DistanceMeters,
            poi.Latitude,
            poi.Longitude,
            IsSelected = selectedId.HasValue && poi.Id == selectedId.Value,
            IsNearest = nearestId.HasValue && poi.Id == nearestId.Value,
            IsVisible = visiblePoiIds.Count == 0 || visiblePoiIds.Contains(poi.Id)
        }));
        var activeTourJson = JsonSerializer.Serialize(new
        {
            Name = ActiveTour?.Name ?? string.Empty,
            Status = ActiveTourStatus,
            Stops = activeTourStops
        });
        var userLocationJson = _latestLocation == null
            ? "null"
            : JsonSerializer.Serialize(new
            {
                Latitude = _latestLocation.Latitude,
                Longitude = _latestLocation.Longitude,
                Accuracy = Math.Max(_latestLocation.Accuracy ?? 0, 8d)
            });

        MapHtml = $$"""
            <html>
            <head>
              <meta name="viewport" content="width=device-width, initial-scale=1.0" />
              <link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css" />
              <script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js"></script>
              <style>
                html, body { height: 100%; margin: 0; }
                body {
                  position: relative;
                  background:
                    radial-gradient(circle at top left, rgba(228, 180, 60, 0.16), transparent 34%),
                    linear-gradient(180deg, #eef4fa 0%, #e6edf5 100%);
                  font-family: "Segoe UI", sans-serif;
                }
                #map { height: 100%; width: 100%; }
                .leaflet-control-attribution { font-size: 10px; }
                .map-panel,
                .map-legend,
                .map-tour-panel {
                  position: absolute;
                  z-index: 999;
                  border-radius: 18px;
                  box-shadow: 0 14px 34px rgba(15, 31, 49, 0.16);
                  backdrop-filter: blur(10px);
                }
                .map-panel {
                  top: 12px;
                  left: 12px;
                  padding: 14px 16px;
                  max-width: 260px;
                  background: rgba(12, 28, 42, 0.88);
                  color: #fff;
                }
                .map-panel strong,
                .map-tour-panel strong {
                  display: block;
                  font-size: 14px;
                  margin-bottom: 6px;
                }
                .map-panel span,
                .map-tour-panel span {
                  display: block;
                  font-size: 12px;
                  line-height: 1.45;
                  opacity: 0.92;
                }
                .map-tour-panel {
                  top: 12px;
                  right: 12px;
                  padding: 12px 14px;
                  max-width: 220px;
                  background: rgba(255, 255, 255, 0.92);
                  color: #17324d;
                }
                .map-legend {
                  left: 12px;
                  right: 12px;
                  bottom: 12px;
                  display: flex;
                  flex-wrap: wrap;
                  gap: 8px;
                  padding: 10px 12px;
                  background: rgba(255, 255, 255, 0.92);
                  color: #17324d;
                  align-items: center;
                }
                .map-legend-item {
                  display: inline-flex;
                  align-items: center;
                  gap: 6px;
                  font-size: 11px;
                  padding: 4px 8px;
                  border-radius: 999px;
                  background: #f5f8fb;
                }
                .map-dot {
                  width: 10px;
                  height: 10px;
                  border-radius: 50%;
                  display: inline-block;
                }
                .map-user-icon {
                  width: 18px;
                  height: 18px;
                  border-radius: 50%;
                  border: 3px solid #ffffff;
                  background: #2563eb;
                  box-shadow: 0 0 0 6px rgba(37, 99, 235, 0.18);
                }
                .tour-stop-icon {
                  width: 28px;
                  height: 28px;
                  border-radius: 50%;
                  background: #0f766e;
                  color: white;
                  font-size: 12px;
                  font-weight: 700;
                  display: flex;
                  align-items: center;
                  justify-content: center;
                  border: 2px solid rgba(255,255,255,0.92);
                  box-shadow: 0 8px 18px rgba(15, 118, 110, 0.24);
                }
                .tour-stop-icon.is-current {
                  background: #e4b43c;
                  color: #17324d;
                }
                .poi-popup {
                  min-width: 190px;
                  color: #17324d;
                }
                .poi-popup h4 {
                  margin: 0 0 6px;
                  font-size: 15px;
                }
                .poi-popup p {
                  margin: 0 0 8px;
                  font-size: 12px;
                  line-height: 1.45;
                  color: #506579;
                }
                .poi-popup .meta {
                  display: flex;
                  flex-wrap: wrap;
                  gap: 6px;
                }
                .poi-popup .tag {
                  font-size: 11px;
                  padding: 3px 8px;
                  border-radius: 999px;
                  background: #eef4fa;
                  color: #17324d;
                }
              </style>
            </head>
            <body>
              <div id="map"></div>
              <div class="map-panel">
                <strong id="selected-label"></strong>
                <span id="nearest-label"></span>
                <span id="count-label"></span>
                <span id="status-label"></span>
              </div>
              <div class="map-tour-panel" id="tour-panel" style="display:none;">
                <strong id="tour-name"></strong>
                <span id="tour-status"></span>
                <span id="tour-count"></span>
              </div>
              <div class="map-legend">
                <div class="map-legend-item"><span class="map-dot" style="background:#2563eb;"></span> Vị trí của bạn</div>
                <div class="map-legend-item"><span class="map-dot" style="background:#17324d;"></span> Tất cả POI</div>
                <div class="map-legend-item"><span class="map-dot" style="background:#dc2626;"></span> POI gần nhất</div>
                <div class="map-legend-item"><span class="map-dot" style="background:#f59e0b;"></span> POI đang chọn</div>
                <div class="map-legend-item"><span class="map-dot" style="background:#0f766e;"></span> Tuyen tour</div>
              </div>
              <script>
                const center = [{{FormatInvariant(centerLat)}}, {{FormatInvariant(centerLng)}}];
                const selectedTitle = {{SerializeForJavaScript(selectedTitle)}};
                const nearestTitle = {{SerializeForJavaScript(nearestTitle)}};
                const visibleSummary = {{SerializeForJavaScript($"Đang hiện {VisiblePois.Count}/{poisToRender.Count} POI trên bản đồ.")}};
                const trackingSummary = {{SerializeForJavaScript(_latestLocation == null ? "Chưa có vị trí GPS hiện tại." : $"Vị trí cập nhật mới nhất: {_latestLocation.Latitude:F6}, {_latestLocation.Longitude:F6}")}};
                const pois = {{poisJson}};
                const userLocation = {{userLocationJson}};
                const activeTour = {{activeTourJson}};

                const map = L.map('map', { zoomControl: false, preferCanvas: true }).setView(center, 16);
                L.control.zoom({ position: 'bottomright' }).addTo(map);
                L.tileLayer('https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}{r}.png', {
                  attribution: '&copy; OpenStreetMap contributors &copy; CARTO',
                  maxZoom: 20
                }).addTo(map);

                const bounds = [];
                let selectedMarker = null;

                const escapeHtml = (value) => (value ?? '')
                  .toString()
                  .replace(/&/g, '&amp;')
                  .replace(/</g, '&lt;')
                  .replace(/>/g, '&gt;')
                  .replace(/"/g, '&quot;')
                  .replace(/'/g, '&#39;');

                document.getElementById('selected-label').textContent = `POI đang chọn: ${selectedTitle}`;
                document.getElementById('nearest-label').textContent = `POI gần nhất: ${nearestTitle}`;
                document.getElementById('count-label').textContent = visibleSummary;
                document.getElementById('status-label').textContent = trackingSummary;

                if (activeTour && activeTour.Stops && activeTour.Stops.length > 0) {
                  document.getElementById('tour-panel').style.display = 'block';
                  document.getElementById('tour-name').textContent = activeTour.Name || 'Tour đang hoạt động';
                  document.getElementById('tour-status').textContent = activeTour.Status || '';
                  document.getElementById('tour-count').textContent = `${activeTour.Stops.length} điểm dừng trên bản đồ`;
                }

                if (userLocation) {
                  const userLatLng = [userLocation.Latitude, userLocation.Longitude];
                  L.circle(userLatLng, {
                    radius: Math.max(userLocation.Accuracy || 8, 8),
                    color: '#60a5fa',
                    weight: 1,
                    fillColor: '#93c5fd',
                    fillOpacity: 0.18
                  }).addTo(map);

                  const userIcon = L.divIcon({
                    className: '',
                    html: '<div class="map-user-icon"></div>',
                    iconSize: [18, 18],
                    iconAnchor: [9, 9]
                  });

                  L.marker(userLatLng, { icon: userIcon })
                    .addTo(map)
                    .bindPopup('<div class="poi-popup"><h4>Vị trí của bạn</h4><p>App đang theo dõi GPS để kích hoạt POI và geofence.</p></div>');

                  bounds.push(userLatLng);
                }

                pois.forEach((poi) => {
                  const latLng = [poi.Latitude, poi.Longitude];
                  const markerColor = poi.IsSelected ? '#f59e0b' : (poi.IsNearest ? '#dc2626' : '#17324d');
                  const markerRadius = poi.IsSelected ? 12 : (poi.IsNearest ? 10 : 8);
                  const fillOpacity = poi.IsVisible ? 0.9 : 0.4;
                  const marker = L.circleMarker(latLng, {
                    radius: markerRadius,
                    color: markerColor,
                    fillColor: markerColor,
                    fillOpacity: fillOpacity,
                    weight: poi.IsSelected ? 3 : (poi.IsNearest ? 2.4 : 1.6)
                  }).addTo(map);

                  if (poi.IsSelected) {
                    L.circle(latLng, {
                      radius: Math.max(poi.Radius || 0, 28),
                      color: '#f59e0b',
                      weight: 2,
                      fillColor: '#fbbf24',
                      fillOpacity: 0.08
                    }).addTo(map);
                  }

                  if (poi.IsNearest) {
                    L.circle(latLng, {
                      radius: Math.max(poi.ApproachRadiusMeters || 0, poi.Radius || 0, 48),
                      color: '#dc2626',
                      weight: 1.8,
                      dashArray: '6 6',
                      fillColor: '#fca5a5',
                      fillOpacity: 0.04
                    }).addTo(map);
                  }

                  const distanceText = poi.DistanceMeters > 0
                    ? `${Math.round(poi.DistanceMeters)}m`
                    : 'Đang tính';

                  marker.bindPopup(
                    `<div class="poi-popup">
                      <h4>${escapeHtml(poi.Title)}</h4>
                      <p>${escapeHtml(poi.Summary || 'Không có mô tả ngắn.')}</p>
                      <div class="meta">
                        <span class="tag">${escapeHtml(poi.Category || 'POI')}</span>
                        <span class="tag">Trigger ${escapeHtml(poi.TriggerMode || 'both')}</span>
                        <span class="tag">Khoang cach ${distanceText}</span>
                      </div>
                    </div>`
                  );

                  marker.on('click', () => {
                    window.location.href = `audiotour://poi/${poi.Id}`;
                  });

                  if (poi.IsSelected) {
                    selectedMarker = marker;
                  }

                  bounds.push(latLng);
                });

                if (activeTour && activeTour.Stops && activeTour.Stops.length > 0) {
                  const routePoints = activeTour.Stops.map((stop) => [stop.Latitude, stop.Longitude]);
                  if (routePoints.length > 1) {
                    L.polyline(routePoints, {
                      color: '#0f766e',
                      weight: 4,
                      opacity: 0.88,
                      dashArray: '10 8'
                    }).addTo(map);
                  }

                  activeTour.Stops.forEach((stop, index) => {
                    const icon = L.divIcon({
                      className: '',
                      html: `<div class="tour-stop-icon${stop.IsCurrent ? ' is-current' : ''}">${index + 1}</div>`,
                      iconSize: [28, 28],
                      iconAnchor: [14, 14]
                    });

                    L.marker([stop.Latitude, stop.Longitude], { icon })
                      .addTo(map)
                      .bindPopup(`<div class="poi-popup"><h4>Stop ${index + 1}: ${escapeHtml(stop.Title)}</h4><p>${stop.IsCurrent ? 'Điểm đang phát trong tour.' : 'Điểm dừng nằm trong tuyến tour hiện tại.'}</p></div>`)
                      .on('click', () => {
                        window.location.href = `audiotour://poi/${stop.PoiId}`;
                      });

                    bounds.push([stop.Latitude, stop.Longitude]);
                  });
                }

                if (bounds.length > 1) {
                  map.fitBounds(bounds, { padding: [36, 36], maxZoom: 17 });
                } else {
                  map.setView(center, 16);
                }

                if (selectedMarker) {
                  window.setTimeout(() => selectedMarker.openPopup(), 180);
                }

                window.setTimeout(() => map.invalidateSize(), 220);
              </script>
            </body>
            </html>
            """;
    }

    private static string SerializeForJavaScript<T>(T value) => JsonSerializer.Serialize(value);

    private static string FormatInvariant(double value) => value.ToString(System.Globalization.CultureInfo.InvariantCulture);

    private void NormalizePoiCategories(IEnumerable<PoiItem> pois)
    {
        foreach (var poi in pois)
        {
            NormalizePoiCategory(poi);
        }
    }

    private void NormalizeTourCategories(IEnumerable<TourItem> tours)
    {
        foreach (var stop in tours.SelectMany(x => x.Stops ?? new List<TourStopItem>()))
        {
            if (stop.Poi != null)
            {
                NormalizePoiCategory(stop.Poi);
            }
        }
    }

    private void NormalizePoiCategory(PoiItem poi)
    {
        poi.Category = ResolveCategoryDisplayName(poi.Category);
    }

    private string ResolveCategoryDisplayName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var match = Categories.FirstOrDefault(x =>
            x.Slug.Equals(value, StringComparison.OrdinalIgnoreCase) ||
            x.Name.Equals(value, StringComparison.OrdinalIgnoreCase));

        return match?.Name ?? value;
    }

    private AudioPlaybackRequest CreatePlaybackRequest(PoiItem poi, string triggerType, bool wasAutoPlayed)
    {
        return new AudioPlaybackRequest
        {
            Poi = poi,
            UserId = _userId,
            Language = SelectedLanguage?.Code ?? poi.Language ?? "vi-VN",
            TriggerType = triggerType,
            WasAutoPlayed = wasAutoPlayed
        };
    }

    private void ResetCategoryFilterOptions()
    {
        var options = new List<string> { "Tất cả" };
        options.AddRange(Categories
            .Select(x => x.Name)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x));
        ReplaceCollection(CategoryFilterOptions, options);

        if (!CategoryFilterOptions.Contains(SelectedCategoryFilter))
        {
            SelectedCategoryFilter = "Tất cả";
        }
    }

    private void ApplyPoiFilters()
    {
        IEnumerable<PoiItem> filtered = _allPois;

        if (!string.IsNullOrWhiteSpace(SelectedCategoryFilter) &&
            !SelectedCategoryFilter.Equals("Tất cả", StringComparison.OrdinalIgnoreCase))
        {
            filtered = filtered.Where(x => ResolveCategoryDisplayName(x.Category).Equals(SelectedCategoryFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(PoiSearchText))
        {
            var keyword = PoiSearchText.Trim();
            filtered = filtered.Where(x =>
                x.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                x.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                ResolveCategoryDisplayName(x.Category).Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
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
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedPoiPositionText)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NearestPoiSummaryText)));
        RefreshMap();
    }

    private List<PoiItem> GetPoiNavigationSource()
    {
        if (VisiblePois.Count > 0)
        {
            return VisiblePois.ToList();
        }

        if (_allPois.Count > 0)
        {
            return _allPois.ToList();
        }

        return NearbyPois.ToList();
    }

    private bool CanAutoPlay(PoiItem poi)
    {
        if (!AllowAutoPlay)
        {
            Status = $"Visitor này đang bị tắt auto-play từ admin, bỏ qua {poi.Title}.";
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
            Status = $"Bỏ qua {poi.Title} vì chưa có chuyển trạng thái mới cho trigger mode {poi.TriggerMode}.";
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
                DisplayName = string.IsNullOrWhiteSpace(VisitorDisplayName) ? "Khách ẩn danh" : VisitorDisplayName.Trim(),
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
                Status = $"Không đồng bộ được visitor với API: {BuildConnectionHelpMessage(ex)}";
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

        VisitorDisplayName = string.IsNullOrWhiteSpace(visitor.DisplayName) ? "Khách ẩn danh" : visitor.DisplayName;
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

    private async Task StartTrackingInternalAsync(bool isRestore, CancellationToken cancellationToken = default)
    {
        if (_locationService.IsRunning)
        {
            IsTracking = true;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TrackingActionText)));
            return;
        }

        await _locationService.StartAsync(TrackingInterval);
        Preferences.Default.Set(PreferenceTrackingEnabled, true);
        IsTracking = true;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TrackingActionText)));
        Status = isRestore
            ? AllowBackgroundTracking
                ? "Đã khôi phục tracking GPS tự động. Geofence và auto narration tiếp tục sẵn sàng."
                : "Đã khôi phục tracking GPS ở foreground theo trạng thái trước đó."
            : AllowBackgroundTracking
                ? "Đang tracking GPS. Đã xin quyền location và sẵn sàng geofence/auto narration."
                : "Đang tracking GPS ở foreground. Admin hiện đang tắt background tracking cho visitor này.";
    }

    private async Task StopTrackingAsync(string statusMessage, bool clearPreference)
    {
        await _locationService.StopAsync();
        if (clearPreference)
        {
            Preferences.Default.Set(PreferenceTrackingEnabled, false);
        }

        IsTracking = false;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TrackingActionText)));
        Status = statusMessage;
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
            return "Không kết nối được tới API. Hãy mở AudioGuideAPI trước. Nếu đang chạy trên emulator, URL đúng là http://10.0.2.2:5297; nếu chạy trên máy thật thì dùng IP LAN của máy tính.";
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

    private void PushRecentQr(QrLookupResponse result)
    {
        if (result.Poi == null)
        {
            return;
        }

        var existing = RecentQrLookups.FirstOrDefault(x => x.Code.Equals(result.Code, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            RecentQrLookups.Remove(existing);
        }

        RecentQrLookups.Insert(0, new QrLookupHistoryItem
        {
            Code = result.Code,
            PoiTitle = result.Poi.Title,
            PoiSummary = result.Poi.Summary,
            Language = result.Poi.Language,
            ImageUrl = result.Poi.ImageUrl,
            OpenedAt = DateTime.Now
        });

        while (RecentQrLookups.Count > 8)
        {
            RecentQrLookups.RemoveAt(RecentQrLookups.Count - 1);
        }

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RecentQrSummary)));
    }

    private void PushAudioDiagnostic(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        var entry = $"[{DateTime.Now:HH:mm:ss}] {message.Trim()}";
        if (AudioDiagnostics.Count > 0 && string.Equals(AudioDiagnostics[0], entry, StringComparison.Ordinal))
        {
            return;
        }

        AudioDiagnostics.Insert(0, entry);
        while (AudioDiagnostics.Count > 8)
        {
            AudioDiagnostics.RemoveAt(AudioDiagnostics.Count - 1);
        }

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AudioDiagnosticsSummary)));
    }

    private static string FormatLength(long? value)
    {
        if (!value.HasValue || value.Value <= 0)
        {
            return "không rõ kích thước";
        }

        return $"{value.Value / 1024d:F1} KB";
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

    private void ApplyPermissionSnapshot(AppPermissionSnapshot snapshot)
    {
        LocationPermissionText = $"GPS khi đang dùng app: {FormatPermission(snapshot.LocationWhenInUse)}";
        BackgroundPermissionText = snapshot.LocationAlways.HasValue
            ? $"GPS nen: {FormatPermission(snapshot.LocationAlways.Value)}"
            : "GPS nền: thiết bị không hỗ trợ đọc trạng thái.";
        CameraPermissionText = snapshot.Camera.HasValue
            ? $"Camera/QR: {FormatPermission(snapshot.Camera.Value)}"
            : "Camera/QR: chưa hỗ trợ trên nền này.";
        CanUseCamera = snapshot.Camera == PermissionStatus.Granted;
        NotificationPermissionText = snapshot.Notifications.HasValue
            ? $"Thong bao tracking: {FormatPermission(snapshot.Notifications.Value)}"
            : "Thông báo tracking: không cần hoặc không hỗ trợ.";
    }

    private static string FormatPermission(PermissionStatus status) => status switch
    {
        PermissionStatus.Granted => "Đã cấp",
        PermissionStatus.Denied => "Bi tu choi",
        PermissionStatus.Restricted => "Bi gioi han",
        PermissionStatus.Disabled => "Bị tắt",
        PermissionStatus.Unknown => "Chưa xác định",
        _ => status.ToString()
    };

    private readonly record struct PoiGeofenceState(bool InsideTrigger, bool InsideNearby);
}
