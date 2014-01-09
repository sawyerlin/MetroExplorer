using Windows.Foundation;

namespace MetroExplorer.Pages.MapPage
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using Windows.ApplicationModel.Search;
    using Windows.Storage;
    using Windows.Storage.AccessCache;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Input;
    using Bing.Maps.Search;
    using Bing.Maps;
    using Core;
    using Common;
    using Components.Maps;
    using Components.Maps.Objects;
    using DataSource;
    using DataSource.DataConfigurations;
    using DataSource.DataModels;
    using ExplorerPage;
    using MainPage;
    using Windows.Storage.Pickers;

    public sealed partial class PageMap : LayoutAwarePage
    {
        #region Fields

        // Search
        private readonly SearchPane _searchPane;
        private SearchManager _searchManager;
        private LocationDataResponse _searchResponse;

        // Datas
        private MapModel _map;
        private ObservableCollection<MapLocationModel> _mapLocations;
        private ObservableCollection<MapLocationFolderModel> _mapLocationFolders;

        // Accessors
        private readonly DataAccess<MapModel> _mapDataAccess;
        private readonly DataAccess<MapLocationModel> _mapLocationAccess;
        private readonly DataAccess<MapLocationFolderModel> _mapLocationFolderAccess;

        // MapPins
        private readonly ObservableCollection<MapPin> _mapPins;
        private MapPin _focusedMapPin, _lastFocusedMapPin;

        private bool _isDragging;

        #endregion

        #region Constructors

        public PageMap()
        {
            InitializeComponent();

            _searchPane = SearchPane.GetForCurrentView();
            _searchPane.PlaceholderText = "Please enter your address";
            _searchPane.ShowOnKeyboardInput = true;
            _searchPane.SearchHistoryEnabled = false;

            _mapDataAccess = new DataAccess<MapModel>();
            _mapLocationAccess = new DataAccess<MapLocationModel>();
            _mapLocationFolderAccess = new DataAccess<MapLocationFolderModel>();

            _mapPins = new ObservableCollection<MapPin>();


            MapView.AllowDrop = true;
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
            _mapLocations = await _mapLocationAccess.GetSources(DataSourceType.Sqlite);

            DefaultViewModel["Focused"] = false;
            DefaultViewModel["Linkable"] = false;
            DefaultViewModel["Markable"] = false;
            DefaultViewModel["UnMarkable"] = false;
            DefaultViewModel["FolderSelected"] = false;
            DefaultViewModel["MapLocationFolders"] = _mapLocationFolders;

            SetLocations(_mapLocations);

            MapView.ViewChangeEnded += MapViewViewChangeEnded;

            _searchPane.QuerySubmitted += SearchPaneQuerySubmitted;
            _searchPane.SuggestionsRequested += SearchPaneSuggestionsRequested;
        }

        protected override void SaveState(
            Dictionary<String, Object> pageState)
        {
            MapView.ViewChangeEnded -= MapViewViewChangeEnded;
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
            GeocodeLocation geoCodeLocation = _searchResponse.LocationData
                .FirstOrDefault(locationData => locationData.Address.FormattedAddress == args.QueryText) ??
                _searchResponse.LocationData.FirstOrDefault();

            _lastFocusedMapPin = _focusedMapPin;

            if (geoCodeLocation != null)
            {
                MapPin existedMapPin = _mapPins.FirstOrDefault(mapPin =>
                    mapPin.Latitude == geoCodeLocation.Location.Latitude.ToString()
                    && mapPin.Longitude == geoCodeLocation.Location.Longitude.ToString());
                if (existedMapPin == null)
                {
                    MapPin mapPinElement = new MapPin(string.Empty, string.Empty,
                        geoCodeLocation.Location.Latitude.ToString(),
                        geoCodeLocation.Location.Longitude.ToString());

                    mapPinElement.MapPinTapped += MapPinElementMapPinTapped;
                    MapView.Children.Add(mapPinElement);
                    MapLayer.SetPosition(mapPinElement, geoCodeLocation.Location);
                    MapView.SetView(geoCodeLocation.Location, 15.0f);
                    _focusedMapPin = mapPinElement;
                }
                else
                {
                    Location location = new Location(double.Parse(existedMapPin.Latitude), double.Parse(existedMapPin.Longitude));
                    MapView.SetView(location, 15.0f);
                    existedMapPin.Focus();
                    _focusedMapPin = existedMapPin;
                }
                DefaultViewModel["Focused"] = true;
            }
            DefaultViewModel["Linkable"] = (bool)DefaultViewModel["Focused"] && _focusedMapPin.Marked;
            DefaultViewModel["Markable"] = (bool)DefaultViewModel["Focused"] && !(bool)DefaultViewModel["Linkable"];
            DefaultViewModel["UnMarkable"] = (bool)DefaultViewModel["Linkable"];
        }

        private void MapViewViewChangeEnded(object sender,
            ViewChangeEndedEventArgs e)
        {
            if (_lastFocusedMapPin != null && _lastFocusedMapPin.Focused)
                _lastFocusedMapPin.UnFocus();

            if (_focusedMapPin != null && !_focusedMapPin.Focused)
                _focusedMapPin.Focus();
        }

        private async void MapPinElementMapPinTapped(object sender,
            MapPinTappedEventArgs e)
        {
            MapPin mapPinElement = (MapPin)sender;

            if (mapPinElement.Focused)
            {
                if (e.Marked)
                {
                    await _mapLocationAccess.Add(
                        DataSourceType.Sqlite,
                        new MapLocationModel
                    {
                        ID = Guid.NewGuid(),
                        Name = mapPinElement.Name,
                        Description = mapPinElement.Description,
                        Latitude = mapPinElement.Latitude,
                        Longitude = mapPinElement.Longitude,
                        MapId = _map.ID
                    });

                    MapLocationModel addedLocation = _mapLocations.FirstOrDefault(location =>
                        location.Latitude == mapPinElement.Latitude.ToString());
                    if (addedLocation != null)
                        mapPinElement.Id = addedLocation.ID;

                    _mapPins.Add(mapPinElement);
                }
                else
                {
                    MapLocationModel deleteLocation = _mapLocations.FirstOrDefault(location =>
                        location.ID.Equals(mapPinElement.Id));
                    if (deleteLocation != null)
                        await _mapLocationAccess.Remove(DataSourceType.Sqlite, deleteLocation);

                    _mapPins.Remove(mapPinElement);
                }
            }

            _lastFocusedMapPin = _focusedMapPin;
            _focusedMapPin = mapPinElement;

            if (_lastFocusedMapPin != null)
                _lastFocusedMapPin.UnFocus();

            if (!_focusedMapPin.Focused)
                _focusedMapPin.Focus();
        }

        private void SetLocations(IEnumerable<MapLocationModel> locations)
        {

            foreach (MapLocationModel mapLocation in locations)
            {
                MapPin mapPinElement = new MapPin(mapLocation.Name,
                    mapLocation.Description,
                    mapLocation.Latitude,
                    mapLocation.Longitude) { Id = mapLocation.ID };

                mapPinElement.MapPinTapped += MapPinElementMapPinTapped;

                MapView.Children.Add(mapPinElement);
                Location location = new Location(double.Parse(mapLocation.Latitude), double.Parse(mapLocation.Longitude));
                MapLayer.SetPosition(mapPinElement, location);
                mapPinElement.Mark();
                _mapPins.Add(mapPinElement);
            }

            MapView.ViewChanged += MapViewViewChanged;

        }

        private void MapViewViewChanged(object sender,
            ViewChangedEventArgs e)
        {
            foreach (MapPin mapPin in _mapPins)
            {
                if (DataSource.FocusedLocationId != null && DataSource.FocusedLocationId.Value.Equals(mapPin.Id))
                {
                    _focusedMapPin = mapPin;
                    UpdateMapFolderList(DataSource.FocusedLocationId.Value);
                    //DataSource.SelectedStorageFolders = new List<StorageFolder>();

                    mapPin.Focus();
                    DefaultViewModel["Focused"] = true;
                    DataSource.FocusedLocationId = null;
                }
                mapPin.Mark();
            }

            MapView.ViewChanged -= MapViewViewChanged;
        }

        private async void UpdateMapFolderList(Guid mapLocationId)
        {
            _mapLocationFolderAccess.MapLocationId = mapLocationId;
            _mapLocationFolders = await _mapLocationFolderAccess.GetSources(DataSourceType.Sqlite);
            DefaultViewModel["MapLocationFolders"] = _mapLocationFolders;

            List<MapLocationFolderModel> removedFoders = new List<MapLocationFolderModel>();
            foreach (MapLocationFolderModel removedFolder in _mapLocationFolders)
            {
                StorageFolder folder = await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(removedFolder.Token, AccessCacheOptions.FastLocationsOnly);
                if (folder == null || !DataSource.SelectedStorageFolders.Any(storageFolder => storageFolder.EqualTo(folder)))
                    removedFoders.Add(removedFolder);
            }

            await _mapLocationFolderAccess.RemoveMany(DataSourceType.Sqlite, removedFoders);

            foreach (StorageFolder folder in DataSource.SelectedStorageFolders)
            {
                string token = StorageApplicationPermissions.FutureAccessList.Add(folder, "link");
                if (!_mapLocationFolders.Any(locationFolder => locationFolder.Token.Equals(token)))
                {
                    MapLocationFolderModel locationFolder = new MapLocationFolderModel
                    {
                        ID = Guid.NewGuid(),
                        Name = folder.Name,
                        Description = folder.DateCreated.ToString(),
                        MapLocationId = mapLocationId,
                        Token = token
                    };
                    await _mapLocationFolderAccess.Add(DataSourceType.Sqlite, locationFolder);
                }
            }
        }

        private async void ButtonLinkExplorerClick(object sender, RoutedEventArgs e)
        {
            DataSource.SelectedStorageFolders = new List<StorageFolder>();

            // ToDo: DataSource.SelectedStorageFolders
            foreach (MapLocationFolderModel folder in _mapLocationFolders)
            {
                StorageFolder storageFolder = await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(folder.Token, AccessCacheOptions.FastLocationsOnly);
                if (storageFolder != null)
                    DataSource.SelectedStorageFolders.Add(storageFolder);
            }

            DataSource.FocusedLocationId = _focusedMapPin.Id;
            Frame.Navigate(typeof(PageMain));
        }

        private async void ButtonLinkClick(object sender, RoutedEventArgs e)
        {
            FolderPicker folderPicker = new FolderPicker
            {
                ViewMode = PickerViewMode.List,
                SuggestedStartLocation = PickerLocationId.Desktop
            };
            folderPicker.FileTypeFilter.Add("*");
            StorageFolder storageFolder = await folderPicker.PickSingleFolderAsync();

            if (storageFolder == null) return;

            string token = StorageApplicationPermissions.FutureAccessList.Add(storageFolder, "link");

            if (!_mapLocationFolders.Any(folder => folder.Token.Equals(token)))
            {
                await _mapLocationFolderAccess.Add(DataSourceType.Sqlite, new MapLocationFolderModel
                    {
                        ID = Guid.NewGuid(),
                        Name = storageFolder.Name,
                        Description = storageFolder.DateCreated.ToString(),
                        MapLocationId = _focusedMapPin.Id,
                        Token = token
                    });

                _mapLocationFolders = await _mapLocationFolderAccess.GetSources(DataSourceType.Sqlite);
                DefaultViewModel["MapLocationFolders"] = _mapLocationFolders;
                DefaultViewModel["Linkable"] = (bool)DefaultViewModel["Focused"] && _focusedMapPin.Marked;
                DefaultViewModel["Markable"] = (bool)DefaultViewModel["Focused"] && !(bool)DefaultViewModel["Linkable"];
                DefaultViewModel["UnMarkable"] = (bool)DefaultViewModel["Linkable"];
            }
        }

        private void ButtonShowClick(object sender, RoutedEventArgs e)
        {
            //StorageFolder folder = await StorageApplicationPermissions.FutureAccessList
            //    .GetFolderAsync(MapFolderListView.SelectedItem.Token);
            //DataSource.NavigatorStorageFolders.Clear();
            //DataSource.NavigatorStorageFolders.Add(folder);

            //if (folder != null)
            Frame.Navigate(typeof(PageExplorer));
        }

        private void ButtonDeleteClick(object sender, RoutedEventArgs e)
        {
            //if (MapFolderListView.SelectedItem != null)
            //    await _mapLocationFolderAccess.RemoveMany(
            //        DataSourceType.Sqlite, new List<MapLocationFolderModel> { MapFolderListView.SelectedItem });
        }

        private async void ButtonMarkClick(object sender, RoutedEventArgs e)
        {
            await _mapLocationAccess.Add(
                        DataSourceType.Sqlite,
                        new MapLocationModel
                        {
                            ID = Guid.NewGuid(),
                            Name = _focusedMapPin.Name,
                            Description = _focusedMapPin.Description,
                            Latitude = _focusedMapPin.Latitude,
                            Longitude = _focusedMapPin.Longitude,
                            MapId = _map.ID
                        });

            MapLocationModel addedLocation = _mapLocations.FirstOrDefault(location =>
                location.Latitude == _focusedMapPin.Latitude.ToString());
            if (addedLocation != null)
                _focusedMapPin.Id = addedLocation.ID;

            _mapPins.Add(_focusedMapPin);
            _focusedMapPin.Mark();
            if (addedLocation != null) _mapLocationFolderAccess.MapLocationId = addedLocation.ID;
            _mapLocationFolders = await _mapLocationFolderAccess.GetSources(DataSourceType.Sqlite);
            DefaultViewModel["MapLocationFolders"] = _mapLocationFolders;
            DefaultViewModel["Linkable"] = (bool)DefaultViewModel["Focused"] && _focusedMapPin.Marked;
            DefaultViewModel["Markable"] = (bool)DefaultViewModel["Focused"] && !(bool)DefaultViewModel["Linkable"];
            DefaultViewModel["UnMarkable"] = (bool)DefaultViewModel["Linkable"];
        }

        private async void ButtonUnMarkClick(object sender, RoutedEventArgs e)
        {
            MapLocationModel deleteLocation = _mapLocations.FirstOrDefault(location =>
                        location.ID.Equals(_focusedMapPin.Id));
            if (deleteLocation != null)
                await _mapLocationAccess.Remove(DataSourceType.Sqlite, deleteLocation);

            _mapPins.Remove(_focusedMapPin);
            _focusedMapPin.UnMark();
            DefaultViewModel["Linkable"] = (bool)DefaultViewModel["Focused"] && _focusedMapPin.Marked;
            DefaultViewModel["Markable"] = (bool)DefaultViewModel["Focused"] && !(bool)DefaultViewModel["Linkable"];
            DefaultViewModel["UnMarkable"] = (bool)DefaultViewModel["Linkable"];
        }

        #endregion

        private void MapViewPointerMovedOverride(object sender, PointerRoutedEventArgs e)
        {
            if (_isDragging && MapView != null)
            {
                Point currentPoint = e.GetCurrentPoint(MapView).Position;
                Point transferedPoint = new Point(currentPoint.X - _focusedMapPin.Width / 2,
                    currentPoint.Y - _focusedMapPin.Height / 2);
                Location location;

                if (MapView.TryPixelToLocation(transferedPoint, out location))
                {
                    MapLayer.SetPosition(_focusedMapPin, location);
                }
            }
        }

        private void MapView_PointerReleasedOverride(object sender, PointerRoutedEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                _focusedMapPin = null;
            }

        }

        private void Path_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (BottomAppBar != null) BottomAppBar.IsOpen = false;

            MapView.CapturePointer(e.Pointer);
            MapPin pin = new MapPin(MapView);

            Point currentPoint = e.GetCurrentPoint(MapView).Position;

            Location currentLocation;
            MapView.TryPixelToLocation(currentPoint, out currentLocation);
            MapLayer.SetPosition(pin, currentLocation);
            MapView.Children.Add(pin);
            Point transferedPoint = new Point(currentPoint.X - pin.Width / 2,
                currentPoint.Y - pin.Height / 2);
            MapView.TryPixelToLocation(transferedPoint, out currentLocation);
            MapLayer.SetPosition(pin, currentLocation);

            pin.Dragging += Dragging;

            _focusedMapPin = pin;
            _isDragging = true;
        }

        private void Dragging(object sender, PointerRoutedEventArgs pointerRoutedEventArgs)
        {
            MapPin pin = (MapPin)sender;
            Point currentPoint = pointerRoutedEventArgs.GetCurrentPoint(MapView).Position;
            Point transferedPoint = new Point(currentPoint.X - pin.Width / 2,
                currentPoint.Y - pin.Height / 2);
            Location location;

            if (MapView.TryPixelToLocation(transferedPoint, out location))
            {
                _searchManager = MapView.SearchManager;
                var reponse =
                    _searchManager.ReverseGeocodeAsync(new ReverseGeocodeRequestOptions(location)).GetResults();
                if (reponse == null) return;

                foreach (GeocodeLocation geoLocation in reponse.LocationData)
                {
                    string address = geoLocation.Address.FormattedAddress;
                    pin.DataContext = address;
                }
            }
        }
    }
}