﻿namespace MetroExplorer.Pages.MapPage
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
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
        private readonly MapPin _lastFocusedMapPin;

        private bool _isDragging;

        #endregion

        #region Constructors

        public PageMap()
        {
            _lastFocusedMapPin = null;
            _mapLocationFolders = new ObservableCollection<MapLocationFolderModel>();
            InitializeComponent();

            _searchPane = SearchPane.GetForCurrentView();
            _searchPane.PlaceholderText = "Please enter your address";
            _searchPane.ShowOnKeyboardInput = true;
            _searchPane.SearchHistoryEnabled = false;

            _mapDataAccess = new DataAccess<MapModel>();
            _mapLocationAccess = new DataAccess<MapLocationModel>();


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

            DefaultViewModel["Focused"] = false;
            DefaultViewModel["Linkable"] = false;
            DefaultViewModel["Markable"] = false;
            DefaultViewModel["UnMarkable"] = false;
            DefaultViewModel["FolderSelected"] = false;
            DefaultViewModel["MapLocationFolders"] = _mapLocationFolders;

            SetLocations();

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

        private void MapViewViewChangeEnded(object sender,
            ViewChangeEndedEventArgs e)
        {
            if (_lastFocusedMapPin != null && _lastFocusedMapPin.Focused)
                _lastFocusedMapPin.UnFocus();

            if (_focusedMapPin != null && !_focusedMapPin.Focused)
                _focusedMapPin.Focus();
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

        private async void Dragging(PointerRoutedEventArgs pointerRoutedEventArgs)
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

                if (reponse == null) return;

                foreach (GeocodeLocation geoLocation in reponse.LocationData)
                {
                    string address = geoLocation.Address.FormattedAddress;
                    _focusedMapPin.DataContext = address;
                }
            }
        }
    }
}