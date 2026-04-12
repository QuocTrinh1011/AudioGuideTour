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
    private int _mapRefreshVersion;
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

    public int MapRefreshVersion
    {
        get => _mapRefreshVersion;
        private set => SetField(ref _mapRefreshVersion, value);
    }

    public Location? LatestLocation => _latestLocation;

    public IEnumerable<PoiItem> MapPois => _allPois.Count > 0 ? _allPois : VisiblePois;

    public PoiItem? CurrentNearestPoi => VisiblePois.FirstOrDefault(x => x.IsNearest)
        ?? _allPois.Where(x => x.DistanceMeters > 0).OrderBy(x => x.DistanceMeters).FirstOrDefault()
        ?? NearbyPois.FirstOrDefault();

    public string MapPoisSummary
    {
        get
        {
            var count = _allPois.Count > 0 ? _allPois.Count : VisiblePois.Count;
            return count == 0
                ? "Chưa có POI để hiển thị trên Google Maps."
                : $"Google Maps đang hiển thị {count} POI.";
        }
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
        : $"Đang phát: {_audioQueueService.CurrentItem.Title}";
    public string QueueSummaryText => _audioQueueService.PendingItems.Count == 0
        ? "Hàng đợi trống"
        : $"Hàng đợi: {string.Join(", ", _audioQueueService.PendingItems.Select(x => x.Title))}";

    public string SelectedPoiSubtitle => SelectedPoi == null
        ? "Chọn 1 POI gần đây hoặc quét QR để xem thông tin chi tiết."
        : $"{SelectedPoi.Language} | {SelectedPoi.DistanceMeters:F0}m | Ưu tiên {SelectedPoi.Priority}";
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
        : $"{ResolveCategoryDisplayName(SelectedPoi.Category)} | Kích hoạt {SelectedPoi.TriggerMode} | Bán kính {SelectedPoi.Radius}m | Tiếp cận {SelectedPoi.ApproachRadiusMeters}m";
    public string SelectedPoiCoordinateText => SelectedPoi == null
        ? "Chưa có tọa độ POI."
        : $"{SelectedPoi.Latitude:F6}, {SelectedPoi.Longitude:F6}";
    public string SelectedPoiVoiceText => SelectedPoi == null
        ? "Chưa có giọng đọc."
        : string.IsNullOrWhiteSpace(SelectedPoi.VoiceName)
            ? $"Ngôn ngữ đọc: {SelectedPoi.Language}"
            : $"Voice ưu tiên: {SelectedPoi.VoiceName} | Ngôn ngữ: {SelectedPoi.Language}";
    public string SelectedPoiAudioText => SelectedPoi == null
        ? "Chưa có thông tin audio."
        : string.IsNullOrWhiteSpace(SelectedPoi.AudioUrl)
            ? $"Chế độ: {SelectedPoi.AudioMode}. Đang ưu tiên TTS."
            : $"Chế độ: {SelectedPoi.AudioMode}. Có file audio hỗ trợ.";
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
                : $"POI {index + 1}/{source.Count} trong danh sách hiện tại.";
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
                : $"Tour {ActiveTour.Name}: điểm {_activeTourStopIndex + 1}/{totalStops} - {currentStop.Poi.Title}";
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
            Status = "Nhập mã QR trước khi mở nội dung.";
            return;
        }

        try
        {
            _apiClient.BaseUrl = ApiBaseUrl;
            var language = SelectedLanguage?.Code ?? "vi-VN";
            var normalizedCode = NormalizeQrCode(code);
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
            Status = $"QR {result.Code} đã mở nội dung {result.Poi.Title}.";
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
            Status = "Chọn 1 tour trước khi bắt đầu.";
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
            Status = stop.AutoPlay
                ? $"Đã bắt đầu tour {ActiveTour.Name}."
                : $"Đã bắt đầu tour {ActiveTour.Name}. Điểm dừng này đang tắt tự phát.";
            if (stop.AutoPlay)
            {
                await _audioQueueService.EnqueueAsync(CreatePlaybackRequest(stop.Poi, "tour", false), cancellationToken);
            }
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
            Status = $"Tour {ActiveTour.Name} đã hoàn thành.";
            return;
        }

        _activeTourStopIndex++;
        var nextStop = stops[_activeTourStopIndex];
        if (nextStop.Poi != null)
        {
            SelectedPoi = nextStop.Poi;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ActiveTourStatus)));
            Status = nextStop.AutoPlay
                ? $"Đã chuyển sang điểm dừng {_activeTourStopIndex + 1} của tour {ActiveTour.Name}."
                : $"Đã chuyển sang điểm dừng {_activeTourStopIndex + 1} của tour {ActiveTour.Name}. Điểm này đang tắt tự phát.";
            if (nextStop.AutoPlay)
            {
                await _audioQueueService.EnqueueAsync(CreatePlaybackRequest(nextStop.Poi, "tour", false), cancellationToken);
            }
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
                await StopTrackingAsync("Quyền chạy nền đang tắt, nên app sẽ dừng tracking khi chuyển sang nền.", clearPreference: true);
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
            var nearest = NearbyPois.FirstOrDefault(x => x.IsNearest);
            if (SelectedPoi == null)
            {
                SelectedPoi = nearest ?? NearbyPois.FirstOrDefault();
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
                    var activeTourStop = FindActiveTourStop(geofence.TriggeredPoi.Id);
                    if (activeTourStop != null && !activeTourStop.AutoPlay)
                    {
                        Status = $"Đã tới {geofence.TriggeredPoi.Title}, nhưng điểm dừng tour này đang tắt tự phát.";
                        _recentAutoTriggers[geofence.TriggeredPoi.Id] = DateTime.UtcNow;
                    }
                    else
                    {
                        await _audioQueueService.EnqueueAsync(
                            CreatePlaybackRequest(
                                geofence.TriggeredPoi,
                                string.IsNullOrWhiteSpace(geofence.Reason) ? "geofence" : geofence.Reason,
                                true));
                        _recentAutoTriggers[geofence.TriggeredPoi.Id] = DateTime.UtcNow;
                    }

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
        SelectedPoi ??= NearbyPois.FirstOrDefault(x => x.IsNearest) ?? NearbyPois.FirstOrDefault();
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
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MapPoisSummary)));
        MapRefreshVersion++;
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
                Status = $"Bỏ qua tự phát cho {poi.Title} vì vẫn đang trong thời gian debounce/cooldown trên máy.";
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
            Status = $"Bỏ qua {poi.Title} vì chưa có chuyển trạng thái mới cho kiểu kích hoạt {poi.TriggerMode}.";
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

    private TourStopItem? FindActiveTourStop(int poiId)
    {
        if (ActiveTour == null)
        {
            return null;
        }

        return (ActiveTour.Stops ?? new List<TourStopItem>())
            .OrderBy(x => x.SortOrder)
            .FirstOrDefault(x => x.PoiId == poiId);
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

    private static string NormalizeQrCode(string raw)
    {
        var value = raw?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        if (Uri.TryCreate(value, UriKind.Absolute, out var absolute))
        {
            var fromQuery = TryReadQueryValue(absolute, "code");
            if (!string.IsNullOrWhiteSpace(fromQuery))
            {
                return fromQuery.Trim().ToUpperInvariant();
            }

            var lastSegment = absolute.Segments
                .LastOrDefault()?
                .Trim('/')
                .Trim();
            if (!string.IsNullOrWhiteSpace(lastSegment))
            {
                return lastSegment.ToUpperInvariant();
            }
        }

        return value.ToUpperInvariant();
    }

    private static string? TryReadQueryValue(Uri uri, string key)
    {
        if (string.IsNullOrWhiteSpace(uri.Query))
        {
            return null;
        }

        foreach (var segment in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var pair = segment.Split('=', 2);
            if (pair.Length == 2 && string.Equals(pair[0], key, StringComparison.OrdinalIgnoreCase))
            {
                return Uri.UnescapeDataString(pair[1]);
            }
        }

        return null;
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
