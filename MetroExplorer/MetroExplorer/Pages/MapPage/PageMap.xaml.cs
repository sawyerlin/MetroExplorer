using Windows.Devices.Geolocation;

namespace MetroExplorer.Pages.MapPage
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;
    using Windows.UI.Core;
    using Windows.ApplicationModel.Search;
    using Windows.UI.Xaml.Input;
    using Bing.Maps.Search;
    using Bing.Maps;
    using Components.Maps;
    using DataSource;
    using DataSource.DataConfigurations;
    using DataSource.DataModels;
    using Windows.Foundation;

    public sealed partial class PageMap
    {
        #region Fields

        // Search
        private readonly SearchPane _searchPane;
        private SearchManager _searchManager;
        private LocationDataResponse _searchResponse;

        // Datas
        private MapModel _map;
        private readonly ObservableCollection<MapLocationFolderModel> _mapLocationFolders;

        // Accessors
        private readonly DataAccess<MapModel> _mapDataAccess;
        private readonly DataAccess<MapLocationModel> _mapLocationAccess;

        // MapPins
        private MapPin _focusedMapPin;
        private bool _isDragging;
        private Point _lastPoint;
        private Point _currentPoint;

        #endregion

        #region Constructors

        public PageMap()
        {
            _mapLocationFolders = new ObservableCollection<MapLocationFolderModel>();
            InitializeComponent();

            _searchPane = SearchPane.GetForCurrentView();
            _searchPane.PlaceholderText = "Please enter your address";
            _searchPane.ShowOnKeyboardInput = true;
            _searchPane.SearchHistoryEnabled = false;

            _mapDataAccess = new DataAccess<MapModel>();
            _mapLocationAccess = new DataAccess<MapLocationModel>();

            MapView.AllowDrop = true;
            ButtonPosition.AddHandler(PointerPressedEvent, new PointerEventHandler(ButtonPositionPointerPressed), true);
        }

        #endregion

        #region Events

        protected override async void LoadState(
            Object navigationParameter,
            Dictionary<String, Object> pageState)
        {
            ObservableCollection<MapModel> maps = await _mapDataAccess.GetSources(DataSourceType.Sqlite);
            _map = maps.First();
            _mapLocationAccess.MapId = _map.ID;

            DefaultViewModel["Focused"] = false;
            DefaultViewModel["Linkable"] = false;
            DefaultViewModel["Markable"] = false;
            DefaultViewModel["UnMarkable"] = false;
            DefaultViewModel["FolderSelected"] = false;
            DefaultViewModel["MapLocationFolders"] = _mapLocationFolders;

            SetLocations();

            _searchPane.QuerySubmitted += SearchPaneQuerySubmitted;
            _searchPane.SuggestionsRequested += SearchPaneSuggestionsRequested;
        }

        protected override void SaveState(
            Dictionary<String, Object> pageState)
        {
            _searchPane.QuerySubmitted -= SearchPaneQuerySubmitted;
            _searchPane.SuggestionsRequested -= SearchPaneSuggestionsRequested;
        }

        private async void SearchPaneSuggestionsRequested(
            SearchPane sender,
            SearchPaneSuggestionsRequestedEventArgs args)
        {
            SearchPaneSuggestionsRequestDeferral deferral = args.Request.GetDeferral();

            GeocodeRequestOptions requests = new GeocodeRequestOptions(args.QueryText);
            _searchManager = MapView.SearchManager;
            _searchResponse = await _searchManager.GeocodeAsync(requests);
            foreach (GeocodeLocation locationData in _searchResponse.LocationData)
                args.Request.SearchSuggestionCollection.AppendQuerySuggestion(locationData.Address.FormattedAddress);

            deferral.Complete();
        }

        private void SearchPaneQuerySubmitted(
            SearchPane sender,
            SearchPaneQuerySubmittedEventArgs args)
        {
            //GeocodeLocation geoCodeLocation = _searchResponse.LocationData
            //    .FirstOrDefault(locationData => locationData.Address.FormattedAddress == args.QueryText) ??
            //    _searchResponse.LocationData.FirstOrDefault();

            //_lastFocusedMapPin = _focusedMapPin;

            //if (geoCodeLocation != null)
            //{
            //    MapPin existedMapPin = _mapPins.FirstOrDefault(mapPin =>
            //        mapPin.Latitude == geoCodeLocation.Location.Latitude.ToString()
            //        && mapPin.Longitude == geoCodeLocation.Location.Longitude.ToString());
            //    if (existedMapPin == null)
            //    {
            //        MapPin mapPinElement = new MapPin(string.Empty, string.Empty,
            //            geoCodeLocation.Location.Latitude.ToString(),
            //            geoCodeLocation.Location.Longitude.ToString());

            //        mapPinElement.MapPinTapped += MapPinElementMapPinTapped;
            //        MapView.Children.Add(mapPinElement);
            //        MapLayer.SetPosition(mapPinElement, geoCodeLocation.Location);
            //        MapView.SetView(geoCodeLocation.Location, 15.0f);
            //        _focusedMapPin = mapPinElement;
            //    }
            //    else
            //    {
            //        Location location = new Location(double.Parse(existedMapPin.Latitude), double.Parse(existedMapPin.Longitude));
            //        MapView.SetView(location, 15.0f);
            //        existedMapPin.Focus();
            //        _focusedMapPin = existedMapPin;
            //    }
            //    DefaultViewModel["Focused"] = true;
            //}
            //DefaultViewModel["Linkable"] = (bool)DefaultViewModel["Focused"] && _focusedMapPin.Marked;
            //DefaultViewModel["Markable"] = (bool)DefaultViewModel["Focused"] && !(bool)DefaultViewModel["Linkable"];
            //DefaultViewModel["UnMarkable"] = (bool)DefaultViewModel["Linkable"];
        }

        private void SetLocations()
        {

            //foreach (MapLocationModel mapLocation in locations)
            //{
            //    MapPin mapPinElement = new MapPin(mapLocation.Name,
            //        mapLocation.Description,
            //        mapLocation.Latitude,
            //        mapLocation.Longitude) { Id = mapLocation.Id };

            //    mapPinElement.MapPinTapped += MapPinElementMapPinTapped;

            //    MapView.Children.Add(mapPinElement);
            //    Location location = new Location(double.Parse(mapLocation.Latitude), double.Parse(mapLocation.Longitude));
            //    MapLayer.SetPosition(mapPinElement, location);
            //    mapPinElement.Mark();
            //    _mapPins.Add(mapPinElement);
            //}

            //MapView.ViewChanged += MapViewViewChanged;

        }

        private async void ButtonHomeClick(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            Geolocator currentGeolocator = new Geolocator();
            Geoposition position = await currentGeolocator.GetGeopositionAsync();
            Location location = new Location(position.Coordinate.Latitude, position.Coordinate.Longitude);
            MapView.SetView(location, 15.0f);
        }

        #endregion

        private void ButtonPositionPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (BottomAppBar != null) BottomAppBar.IsOpen = false;
            if (TopAppBar != null) TopAppBar.IsOpen = false;

            MapView.CapturePointer(e.Pointer);
            MapPin pin = new MapPin(MapView);

            Point currentPoint = e.GetCurrentPoint(MapView).Position;
            _currentPoint = currentPoint;
            Location currentLocation;
            MapView.TryPixelToLocation(currentPoint, out currentLocation);
            MapLayer.SetPosition(pin, currentLocation);
            MapView.Children.Add(pin);
            Point transferedPoint = new Point(currentPoint.X - pin.Width / 2,
                currentPoint.Y - pin.Height / 2);
            MapView.TryPixelToLocation(transferedPoint, out currentLocation);
            MapLayer.SetPosition(pin, currentLocation);

            pin.DragStarted += DragStarted;
            pin.Dragging += Dragging;
            pin.DragCompleted += DragCompleted;

            _focusedMapPin = pin;
            _isDragging = true;
        }

        private void DragCompleted(PointerRoutedEventArgs pointerRoutedEventArgs)
        {
            _focusedMapPin.HidePanel();
        }

        private async void MapViewPointerMovedOverride(object sender, PointerRoutedEventArgs e)
        {
            if (_isDragging && MapView != null)
            {
                _focusedMapPin.HidePanel();
                _lastPoint = _currentPoint;
                Point currentPoint = e.GetCurrentPoint(MapView).Position;
                _currentPoint = currentPoint;

                Point transferedPoint = new Point(currentPoint.X - _focusedMapPin.Width / 2,
                    currentPoint.Y - _focusedMapPin.Height / 2);
                Location location;

                if (MapView.TryPixelToLocation(transferedPoint, out location))
                    MapLayer.SetPosition(_focusedMapPin, location);

                await ShowAddressPanel(e);
            }
        }

        private void MapViewPointerReleasedOverride(object sender, PointerRoutedEventArgs e)
        {
            if (_isDragging)
            {
                _focusedMapPin.HidePanel();
                _isDragging = false;
                _focusedMapPin = null;
            }
        }

        private void DragStarted(MapPin pin)
        {
            _currentPoint = pin.CurrentLocation;
            _focusedMapPin = pin;
        }

        private async void Dragging(PointerRoutedEventArgs pointerRoutedEventArgs)
        {
            if (_focusedMapPin != null)
                _focusedMapPin.HidePanel();
            _lastPoint = _currentPoint;
            Point currentPoint = pointerRoutedEventArgs.GetCurrentPoint(MapView).Position;
            _currentPoint = currentPoint;
            await ShowAddressPanel(pointerRoutedEventArgs);
        }

        private async Task ShowAddressPanel(PointerRoutedEventArgs pointerRoutedEventArgs)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                await Task.Delay(TimeSpan.FromMilliseconds(200));
                if (_focusedMapPin != null)
                {
                    if (_lastPoint == _currentPoint)
                        _focusedMapPin.ShowPanel();

                    await ShowAddress(pointerRoutedEventArgs);
                }
            });
        }

        private async Task ShowAddress(PointerRoutedEventArgs pointerRoutedEventArgs)
        {
            try
            {
                Point currentPoint = pointerRoutedEventArgs.GetCurrentPoint(MapView).Position;
                Point transferedPoint = new Point(currentPoint.X - _focusedMapPin.Width / 2,
                    currentPoint.Y - _focusedMapPin.Height / 2);
                Location location;

                if (MapView.TryPixelToLocation(transferedPoint, out location))
                {
                    _searchManager = MapView.SearchManager;
                    var reponse =
                        await _searchManager.ReverseGeocodeAsync(new ReverseGeocodeRequestOptions(location));

                    if (reponse == null || reponse.LocationData == null) return;

                    foreach (GeocodeLocation geoLocation in reponse.LocationData)
                    {
                        if (geoLocation.Address != null && geoLocation.Address.FormattedAddress != null)
                        {
                            string address = geoLocation.Address.FormattedAddress;
                            _focusedMapPin.Address = address;
                        }
                    }
                }
            }
            catch (Exception)
            {

                // ToDo: Find why The SyetemNullPointerException is throwed
            }
        }

    }
}
