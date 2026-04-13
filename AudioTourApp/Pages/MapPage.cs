using AudioTourApp.Models;
using AudioTourApp.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Maps;
using Map = Microsoft.Maui.Controls.Maps.Map;

namespace AudioTourApp.Pages;

public class MapPage : ContentPage
{
    private readonly MainViewModel _viewModel;
    private readonly Map _nativeMap;
    private bool _hasInitializedRegion;
    private int? _lastFocusedPoiId;
    private bool _isRefreshingMap;

    public MapPage(MainViewModel viewModel)
    {
        BindingContext = _viewModel = viewModel;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        Title = "Bản đồ";
        BackgroundColor = Color.FromArgb("#F3F6FA");
        _nativeMap = CreateNativeMap();
        Content = BuildContent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RefreshNativeMap(forceRegion: !_hasInitializedRegion);
    }

    private async void OnToggleTrackingClicked(object? sender, EventArgs e)
    {
        await _viewModel.ToggleTrackingAsync();
    }

    private async void OnPlaySelectedClicked(object? sender, EventArgs e)
    {
        await _viewModel.PlaySelectedAsync();
    }

    private async void OnOpenMapClicked(object? sender, EventArgs e)
    {
        await _viewModel.OpenSelectedMapAsync();
    }

    private async void OnOpenPoiDetailsClicked(object? sender, EventArgs e)
    {
        await _viewModel.OpenSelectedPoiDetailsAsync();
    }

    private async void OnOpenNarrationClicked(object? sender, EventArgs e)
    {
        await _viewModel.OpenSelectedNarrationAsync();
    }

    private async void OnLanguageChanged(object? sender, EventArgs e)
    {
        await _viewModel.ChangeLanguageAsync();
    }

    private void OnNearestPoiClicked(object? sender, EventArgs e)
    {
        _viewModel.SelectNearestPoi();
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.MapRefreshVersion))
        {
            MainThread.BeginInvokeOnMainThread(() => RefreshNativeMap());
        }
    }

    private View BuildContent()
    {
        var root = new VerticalStackLayout
        {
            Padding = new Thickness(18, 18, 18, 28),
            Spacing = 18
        };

        root.Add(new Border
        {
            StrokeThickness = 0,
            Padding = 18,
            StrokeShape = new RoundRectangle { CornerRadius = 26 },
            Background = new LinearGradientBrush(
                new GradientStopCollection
                {
                    new(Color.FromArgb("#17324D"), 0f),
                    new(Color.FromArgb("#2E6C7B"), 1f)
                },
                new Point(0, 0),
                new Point(1, 1)),
            Content = new VerticalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    new Label
                    {
                        Text = "Google Maps và geofence",
                        FontSize = 26,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Colors.White
                    },
                    new Label
                    {
                        Text = "Hiển thị vị trí của bạn, tất cả POI, điểm gần nhất và chạm marker để xem chi tiết nhanh.",
                        TextColor = Color.FromArgb("#E4EEF7")
                    }
                }
            }
        });

        var mapCard = new Border
        {
            Stroke = Color.FromArgb("#D9E3EE"),
            BackgroundColor = Colors.White,
            Padding = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 24 }
        };

        var mapHeader = new Grid
        {
            Padding = new Thickness(16, 16, 16, 0),
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        mapHeader.Add(new VerticalStackLayout
        {
            Spacing = 2,
            Children =
            {
                new Label { Text = "Google Maps hiện tại", FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#17324D") },
                new Label { TextColor = Color.FromArgb("#667C92") }.Bind(Label.TextProperty, nameof(MainViewModel.CurrentLocation)),
                new Label { FontSize = 12, TextColor = Color.FromArgb("#8AA0B6") }.Bind(Label.TextProperty, nameof(MainViewModel.MapPoisSummary))
            }
        });

        var headerActions = new VerticalStackLayout
        {
            Spacing = 8,
            HorizontalOptions = LayoutOptions.End,
            Children =
            {
                CreateLanguagePicker(),
                CreateBoundActionButton(nameof(MainViewModel.TrackingActionText), OnToggleTrackingClicked, "#E4B43C", "#17324D")
            }
        };
        Grid.SetColumn(headerActions, 1);
        mapHeader.Add(headerActions);

        var statusStrip = new Grid
        {
            Padding = new Thickness(16, 12, 16, 12),
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 10
        };
        statusStrip.Add(CreateInfoChip("Định vị", nameof(MainViewModel.TrackingStatusText), "#EEF5FB", "#17324D"));
        var nearestChip = CreateInfoChip("POI gần nhất", nameof(MainViewModel.NearestPoiSummaryText), "#FFF7E2", "#8B5E00");
        Grid.SetColumn(nearestChip, 1);
        statusStrip.Add(nearestChip);

        var mapLegend = new HorizontalStackLayout
        {
            Spacing = 10,
            Padding = new Thickness(16, 0, 16, 14)
        };
        mapLegend.Add(CreateLegendPill("Bạn", "#0D6EFD"));
        mapLegend.Add(CreateLegendPill("Tất cả POI", "#17324D"));
        mapLegend.Add(CreateLegendPill("Gần nhất", "#D9480F"));
        mapLegend.Add(CreateLegendPill("Đang chọn", "#F0B429"));

        mapCard.Content = new VerticalStackLayout
        {
            Spacing = 0,
            Children = { mapHeader, statusStrip, mapLegend, _nativeMap }
        };
        root.Add(mapCard);

        var selectedCard = new Border
        {
            Stroke = Color.FromArgb("#D9E3EE"),
            BackgroundColor = Colors.White,
            Padding = 16,
            StrokeShape = new RoundRectangle { CornerRadius = 24 }
        };
        var selectedLayout = new VerticalStackLayout { Spacing = 12 };
        selectedLayout.Add(new Label
        {
            Text = "POI đang được chọn trên bản đồ",
            FontSize = 20,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#17324D")
        });
        selectedLayout.Add(new Image
        {
            HeightRequest = 200,
            Aspect = Aspect.AspectFill,
            BackgroundColor = Color.FromArgb("#E8EDF3")
        }.Bind(Image.SourceProperty, "SelectedPoi.ImageUrl", converter: AppImageSourceConverter.Instance));
        selectedLayout.Add(new Label
        {
            FontSize = 22,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#17324D")
        }.Bind(Label.TextProperty, "SelectedPoi.Title"));
        selectedLayout.Add(new Label { TextColor = Color.FromArgb("#667C92") }.Bind(Label.TextProperty, nameof(MainViewModel.SelectedPoiMetaText)));
        selectedLayout.Add(new Label { TextColor = Color.FromArgb("#8AA0B6"), FontSize = 12 }.Bind(Label.TextProperty, nameof(MainViewModel.SelectedPoiCoordinateText)));
        selectedLayout.Add(new Label
        {
            Text = "Bản thuyết minh",
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#17324D")
        });
        selectedLayout.Add(new Label
        {
            TextColor = Color.FromArgb("#31485F"),
            MaxLines = 5,
            LineBreakMode = LineBreakMode.TailTruncation
        }.Bind(Label.TextProperty, nameof(MainViewModel.SelectedPoiNarrationText)));
        selectedLayout.Add(new Label { TextColor = Color.FromArgb("#5F7488") }.Bind(Label.TextProperty, nameof(MainViewModel.SelectedPoiAudioText)));

        var selectedActions = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 10
        };
        selectedActions.Add(CreateActionButton("Nghe ngay", OnPlaySelectedClicked, "#17324D", "White"));
        var selectedMapButton = CreateActionButton("Mở bản đồ", OnOpenMapClicked, "#EEF3F8", "#17324D");
        Grid.SetColumn(selectedMapButton, 1);
        selectedActions.Add(selectedMapButton);
        var selectedDetailButton = CreateActionButton("Chi tiết", OnOpenPoiDetailsClicked, "#E4B43C", "#17324D");
        Grid.SetColumn(selectedDetailButton, 2);
        selectedActions.Add(selectedDetailButton);
        selectedLayout.Add(selectedActions);

        var bottomSelectedActions = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 10
        };
        bottomSelectedActions.Add(CreateActionButton("Mở bản thuyết minh", OnOpenNarrationClicked, "#EEF5FB", "#17324D"));
        var nearestButton = CreateActionButton("POI gần nhất", OnNearestPoiClicked, "#F3F7FB", "#17324D");
        Grid.SetColumn(nearestButton, 1);
        bottomSelectedActions.Add(nearestButton);
        selectedLayout.Add(bottomSelectedActions);
        selectedLayout.Add(new Label
        {
            TextColor = Color.FromArgb("#35526B"),
            FontAttributes = FontAttributes.Bold
        }.Bind(Label.TextProperty, nameof(MainViewModel.PlaybackStatusText)));
        selectedCard.Content = selectedLayout;
        root.Add(selectedCard);

        var poisCard = new Border
        {
            Stroke = Color.FromArgb("#D9E3EE"),
            BackgroundColor = Colors.White,
            Padding = 16,
            StrokeShape = new RoundRectangle { CornerRadius = 24 }
        };
        var poisLayout = new VerticalStackLayout { Spacing = 12 };
        poisLayout.Add(new Label
        {
            Text = "Tất cả điểm thuyết minh",
            FontSize = 20,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#17324D")
        });
        poisLayout.Add(new Entry
        {
            Placeholder = "Tìm theo tên, tóm tắt, địa chỉ...",
            BackgroundColor = Color.FromArgb("#F8FAFD"),
            TextColor = Color.FromArgb("#17324D")
        }.Bind(Entry.TextProperty, nameof(MainViewModel.PoiSearchText), BindingMode.TwoWay));

        var categoryPicker = new Picker
        {
            Title = "Lọc theo danh mục",
            BackgroundColor = Color.FromArgb("#F8FAFD"),
            TextColor = Color.FromArgb("#17324D")
        };
        categoryPicker.SetBinding(Picker.ItemsSourceProperty, nameof(MainViewModel.CategoryFilterOptions));
        categoryPicker.SetBinding(Picker.SelectedItemProperty, nameof(MainViewModel.SelectedCategoryFilter), BindingMode.TwoWay);
        poisLayout.Add(categoryPicker);
        poisLayout.Add(new Label
        {
            TextColor = Color.FromArgb("#667C92"),
            FontSize = 12
        }.Bind(Label.TextProperty, nameof(MainViewModel.VisiblePoisSummary)));

        var collection = new CollectionView { SelectionMode = SelectionMode.Single };
        collection.SetBinding(ItemsView.ItemsSourceProperty, nameof(MainViewModel.VisiblePois));
        collection.SetBinding(SelectableItemsView.SelectedItemProperty, nameof(MainViewModel.SelectedPoi), BindingMode.TwoWay);
        collection.ItemTemplate = new DataTemplate(() =>
        {
            var card = new Border
            {
                Stroke = Color.FromArgb("#E3EAF2"),
                BackgroundColor = Color.FromArgb("#FBFCFE"),
                StrokeShape = new RoundRectangle { CornerRadius = 20 },
                Padding = 12,
                Margin = new Thickness(0, 0, 0, 10)
            };

            var grid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(100),
                    new ColumnDefinition(GridLength.Star)
                },
                ColumnSpacing = 12
            };

            grid.Add(new Image
            {
                HeightRequest = 90,
                WidthRequest = 100,
                Aspect = Aspect.AspectFill,
                BackgroundColor = Color.FromArgb("#E8EDF3")
            }.Bind(Image.SourceProperty, nameof(PoiItem.ImageUrl), converter: AppImageSourceConverter.Instance));

            var details = new VerticalStackLayout { Spacing = 4 };
            details.Add(new Label
            {
                FontAttributes = FontAttributes.Bold,
                FontSize = 17,
                TextColor = Color.FromArgb("#17324D")
            }.Bind(Label.TextProperty, nameof(PoiItem.Title)));
            details.Add(new Label
            {
                TextColor = Color.FromArgb("#5D7287"),
                MaxLines = 2,
                LineBreakMode = LineBreakMode.TailTruncation
            }.Bind(Label.TextProperty, nameof(PoiItem.Summary)));
            details.Add(new Label
            {
                TextColor = Color.FromArgb("#17324D")
            }.Bind(Label.TextProperty, nameof(PoiItem.DistanceLabel)));
            details.Add(new Label
            {
                TextColor = Color.FromArgb("#73869A")
            }.Bind(Label.TextProperty, nameof(PoiItem.TriggerMode), stringFormat: "Kích hoạt: {0}"));
            details.Add(new Label
            {
                TextColor = Color.FromArgb("#8A9BAA"),
                FontSize = 12
            }.Bind(Label.TextProperty, nameof(PoiItem.Category), stringFormat: "Danh mục: {0}"));

            Grid.SetColumn(details, 1);
            grid.Add(details);
            card.Content = grid;
            return card;
        });
        poisLayout.Add(collection);

        var actions = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 10
        };
        actions.Add(CreateActionButton("Nghe POI đã chọn", OnPlaySelectedClicked, "#17324D", "White"));
        var openMapButton = CreateActionButton("Mở Google Maps ngoài", OnOpenMapClicked, "#EEF3F8", "#17324D");
        Grid.SetColumn(openMapButton, 1);
        actions.Add(openMapButton);
        poisLayout.Add(actions);
        poisLayout.Add(CreateActionButton("Xem chi tiết POI", OnOpenPoiDetailsClicked, "#E4B43C", "#17324D"));
        poisCard.Content = poisLayout;
        root.Add(poisCard);

        return new ScrollView { Content = root };
    }

    private Map CreateNativeMap()
    {
        return new Map
        {
            HeightRequest = 420,
            IsShowingUser = true,
            IsScrollEnabled = true,
            IsZoomEnabled = true,
            IsTrafficEnabled = false,
            MapType = MapType.Street
        };
    }

    private void RefreshNativeMap(bool forceRegion = false)
    {
        if (_isRefreshingMap)
        {
            return;
        }

        try
        {
            _isRefreshingMap = true;

            var pois = _viewModel.MapPois
                .Where(poi => poi.Latitude != 0 && poi.Longitude != 0)
                .OrderByDescending(poi => poi.IsNearest)
                .ThenByDescending(poi => poi.Priority)
                .ThenBy(poi => poi.Title)
                .ToList();

            _nativeMap.Pins.Clear();
            _nativeMap.MapElements.Clear();
            _nativeMap.IsShowingUser = _viewModel.LatestLocation != null;

            AddUserLocationElements(_viewModel.LatestLocation);

            var selectedPoi = _viewModel.SelectedPoi;
            var nearestPoi = _viewModel.CurrentNearestPoi;

            foreach (var poi in pois)
            {
                var pin = new PoiMapPin
                {
                    PoiId = poi.Id,
                    Label = poi.Title,
                    Address = BuildPinAddress(poi),
                    Location = new Location(poi.Latitude, poi.Longitude),
                    Type = poi.Id == selectedPoi?.Id
                        ? PinType.Place
                        : poi.Id == nearestPoi?.Id
                            ? PinType.SearchResult
                            : PinType.SavedPin
                };
                pin.MarkerClicked += OnPoiMarkerClicked;
                pin.InfoWindowClicked += OnPoiInfoWindowClicked;
                _nativeMap.Pins.Add(pin);
            }

            AddHighlightElements(selectedPoi, nearestPoi);

            var shouldFocusSelected = selectedPoi?.Id != _lastFocusedPoiId;
            if (selectedPoi != null)
            {
                _lastFocusedPoiId = selectedPoi.Id;
            }

            if (selectedPoi != null && (forceRegion || shouldFocusSelected))
            {
                var selectedRadius = Math.Max(selectedPoi.ApproachRadiusMeters, selectedPoi.Radius);
                MoveToPoi(selectedPoi, Math.Max(selectedRadius, 140));
                _hasInitializedRegion = true;
                return;
            }

            if (!_hasInitializedRegion || forceRegion)
            {
                MoveToFitAll(pois, _viewModel.LatestLocation);
                _hasInitializedRegion = true;
            }
        }
        finally
        {
            _isRefreshingMap = false;
        }
    }

    private void AddHighlightElements(PoiItem? selectedPoi, PoiItem? nearestPoi)
    {
        if (selectedPoi != null && selectedPoi.Latitude != 0 && selectedPoi.Longitude != 0)
        {
            _nativeMap.MapElements.Add(new Circle
            {
                Center = new Location(selectedPoi.Latitude, selectedPoi.Longitude),
                Radius = Distance.FromMeters(Math.Max(selectedPoi.Radius, 40)),
                StrokeColor = Color.FromArgb("#F0B429"),
                StrokeWidth = 4,
                FillColor = Color.FromArgb("#22F0B429")
            });
        }

        var shouldShowNearestHighlight = selectedPoi == null;

        if (shouldShowNearestHighlight &&
            nearestPoi != null &&
            nearestPoi.Id != selectedPoi?.Id &&
            nearestPoi.Latitude != 0 &&
            nearestPoi.Longitude != 0)
        {
            _nativeMap.MapElements.Add(new Circle
            {
                Center = new Location(nearestPoi.Latitude, nearestPoi.Longitude),
                Radius = Distance.FromMeters(Math.Max(Math.Max(nearestPoi.ApproachRadiusMeters, nearestPoi.Radius), 55)),
                StrokeColor = Color.FromArgb("#D9480F"),
                StrokeWidth = 3,
                FillColor = Color.FromArgb("#18D9480F")
            });
        }

        var activeStops = (_viewModel.ActiveTour?.Stops ?? new List<TourStopItem>())
            .Where(stop => stop.Poi != null && stop.Poi.Latitude != 0 && stop.Poi.Longitude != 0)
            .OrderBy(stop => stop.SortOrder)
            .ToList();

        if (activeStops.Count > 1)
        {
            var route = new Microsoft.Maui.Controls.Maps.Polyline
            {
                StrokeColor = Color.FromArgb("#0F766E"),
                StrokeWidth = 6
            };

            foreach (var stop in activeStops)
            {
                route.Geopath.Add(new Location(stop.Poi!.Latitude, stop.Poi.Longitude));
            }

            _nativeMap.MapElements.Add(route);
        }
    }

    private void AddUserLocationElements(Location? userLocation)
    {
        if (userLocation == null)
        {
            return;
        }

        var accuracy = userLocation.Accuracy.GetValueOrDefault();
        var safeAccuracy = Math.Max(1d, accuracy);

        _nativeMap.Pins.Add(new Pin
        {
            Label = "Vị trí của bạn",
            Address = $"Độ chính xác khoảng {safeAccuracy:F0}m",
            Location = new Location(userLocation.Latitude, userLocation.Longitude),
            Type = PinType.Generic
        });

        _nativeMap.MapElements.Add(new Circle
        {
            Center = new Location(userLocation.Latitude, userLocation.Longitude),
            Radius = Distance.FromMeters(Math.Max(8d, safeAccuracy)),
            StrokeColor = Color.FromArgb("#0D6EFD"),
            StrokeWidth = 3,
            FillColor = Color.FromArgb("#1A0D6EFD")
        });
    }

    private void MoveToPoi(PoiItem poi, double radiusMeters)
    {
        _nativeMap.MoveToRegion(MapSpan.FromCenterAndRadius(
            new Location(poi.Latitude, poi.Longitude),
            Distance.FromMeters(radiusMeters)));
    }

    private void MoveToFitAll(IReadOnlyCollection<PoiItem> pois, Location? userLocation)
    {
        var locations = new List<Location>();
        locations.AddRange(pois.Select(poi => new Location(poi.Latitude, poi.Longitude)));
        if (userLocation != null)
        {
            locations.Add(new Location(userLocation.Latitude, userLocation.Longitude));
        }

        if (locations.Count == 0)
        {
            _nativeMap.MoveToRegion(new MapSpan(new Location(10.7618, 106.7041), 0.02, 0.02));
            return;
        }

        var minLat = locations.Min(location => location.Latitude);
        var maxLat = locations.Max(location => location.Latitude);
        var minLng = locations.Min(location => location.Longitude);
        var maxLng = locations.Max(location => location.Longitude);

        var center = new Location((minLat + maxLat) / 2d, (minLng + maxLng) / 2d);
        var latitudeDegrees = Math.Max(0.01, (maxLat - minLat) * 1.6);
        var longitudeDegrees = Math.Max(0.01, (maxLng - minLng) * 1.6);

        _nativeMap.MoveToRegion(new MapSpan(center, latitudeDegrees, longitudeDegrees));
    }

    private void OnPoiMarkerClicked(object? sender, PinClickedEventArgs e)
    {
        if (sender is PoiMapPin pin)
        {
            _viewModel.SelectPoiById(pin.PoiId);
        }
    }

    private async void OnPoiInfoWindowClicked(object? sender, PinClickedEventArgs e)
    {
        if (sender is not PoiMapPin pin)
        {
            return;
        }

        _viewModel.SelectPoiById(pin.PoiId);
        await _viewModel.OpenSelectedPoiDetailsAsync();
    }

    private static string BuildPinAddress(PoiItem poi)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(poi.Category))
        {
            parts.Add(poi.Category);
        }

        if (poi.HasKnownDistance)
        {
            parts.Add(poi.DistanceDisplay);
        }

        if (!string.IsNullOrWhiteSpace(poi.Summary))
        {
            parts.Add(poi.Summary);
        }

        return string.Join(" | ", parts.Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    private static Button CreateActionButton(string text, EventHandler handler, string backgroundColor, string textColor)
    {
        var button = new Button
        {
            Text = text,
            BackgroundColor = Color.FromArgb(backgroundColor),
            TextColor = textColor.Equals("White", StringComparison.OrdinalIgnoreCase) ? Colors.White : Color.FromArgb(textColor),
            CornerRadius = 18
        };
        button.Clicked += handler;
        return button;
    }

    private static Button CreateBoundActionButton(string propertyName, EventHandler handler, string backgroundColor, string textColor)
    {
        var button = new Button
        {
            BackgroundColor = Color.FromArgb(backgroundColor),
            TextColor = textColor.Equals("White", StringComparison.OrdinalIgnoreCase) ? Colors.White : Color.FromArgb(textColor),
            CornerRadius = 18
        };
        button.SetBinding(Button.TextProperty, propertyName);
        button.Clicked += handler;
        return button;
    }

    private Picker CreateLanguagePicker()
    {
        var picker = new Picker
        {
            Title = "Ngôn ngữ",
            ItemDisplayBinding = new Binding("NativeName"),
            WidthRequest = 170,
            BackgroundColor = Color.FromArgb("#F8FAFD"),
            TextColor = Color.FromArgb("#17324D")
        };
        picker.SetBinding(Picker.ItemsSourceProperty, nameof(MainViewModel.Languages));
        picker.SetBinding(Picker.SelectedItemProperty, nameof(MainViewModel.SelectedLanguage), BindingMode.TwoWay);
        picker.SelectedIndexChanged += OnLanguageChanged;
        return picker;
    }

    private static Border CreateInfoChip(string title, string bindingPath, string backgroundColor, string accentColor)
    {
        var stack = new VerticalStackLayout
        {
            Spacing = 2,
            Children =
            {
                new Label
                {
                    Text = title,
                    FontSize = 12,
                    TextColor = Color.FromArgb(accentColor)
                },
                new Label
                {
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#17324D"),
                    FontSize = 13,
                    LineBreakMode = LineBreakMode.TailTruncation
                }.Bind(Label.TextProperty, bindingPath)
            }
        };

        return new Border
        {
            StrokeThickness = 0,
            BackgroundColor = Color.FromArgb(backgroundColor),
            Padding = new Thickness(12, 10),
            StrokeShape = new RoundRectangle { CornerRadius = 18 },
            Content = stack
        };
    }

    private static Border CreateLegendPill(string text, string dotColor)
    {
        return new Border
        {
            StrokeThickness = 0,
            BackgroundColor = Color.FromArgb("#F5F8FB"),
            Padding = new Thickness(10, 6),
            StrokeShape = new RoundRectangle { CornerRadius = 16 },
            Content = new HorizontalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    new BoxView
                    {
                        WidthRequest = 10,
                        HeightRequest = 10,
                        CornerRadius = 5,
                        BackgroundColor = Color.FromArgb(dotColor),
                        VerticalOptions = LayoutOptions.Center
                    },
                    new Label
                    {
                        Text = text,
                        FontSize = 12,
                        TextColor = Color.FromArgb("#17324D"),
                        VerticalTextAlignment = TextAlignment.Center
                    }
                }
            }
        };
    }
}

internal sealed class PoiMapPin : Pin
{
    public int PoiId { get; init; }
}
