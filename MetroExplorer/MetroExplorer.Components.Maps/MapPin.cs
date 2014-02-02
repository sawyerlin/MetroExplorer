namespace MetroExplorer.Components.Maps
{
    using System;
    using Windows.Foundation;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Input;
    using Windows.UI.Xaml.Media;
    using Bing.Maps;
    using DataSource.DataModels;
    using Model.MapModel;

    public sealed class MapPin : Control
    {
        #region Fields

        private readonly Map _map;
        private Location _mapCenter;

        #endregion

        #region Dependency Properties

        public static readonly DependencyProperty AddressProperty = DependencyProperty.Register(
            "Address", typeof(string), typeof(MapPin), new PropertyMetadata("Loading ..."));

        public string Address
        {
            get { return (string)GetValue(AddressProperty); }
            set { SetValue(AddressProperty, value); }
        }

        public static readonly DependencyProperty MapPinModelProperty = DependencyProperty.Register(
            "MapPinModel", typeof(MapPinModel), typeof(MapPin), new PropertyMetadata(new MapPinModel
            {
                Address = "Test",
                Image = new ImageBrush()
            }));

        public MapPinModel MapPinModel
        {
            get { return (MapPinModel)GetValue(MapPinModelProperty); }
            set { SetValue(MapPinModelProperty, value); }
        }

        #endregion

        #region Properties

        public MapLocationModel LocationModel { get; set; }

        public Point CurrentLocation { get; set; }

        public bool IsDragging { get; private set; }

        #endregion

        #region EventHandler

        public Action<MapPin> DragStarted;

        public Action<PointerRoutedEventArgs> Dragging;

        public Action<PointerRoutedEventArgs> DragCompleted;

        #endregion

        #region Constructors

        public MapPin()
        {
            DefaultStyleKey = typeof(MapPin);
        }

        public MapPin(Map map)
            : this()
        {
            _map = map;
            PointerPressed += MapPinPointerPressed;
        }

        public MapPin(
            MapLocationModel locationModel)
            : this()
        {
            LocationModel = locationModel;
        }

        #endregion

        #region Events

        protected override void OnTapped(TappedRoutedEventArgs e)
        {
            base.OnTapped(e);
            // ToDo: Change MapPinModel
            MapPinModel.Address = Address;
            MapPinModel.Image = new ImageBrush();
        }

        private void MapPinPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (_map != null)
            {
                _mapCenter = _map.Center;
                _map.ViewChangeStarted += MapViewChangeStarted;
                _map.PointerMovedOverride += MapPointerMovedOverride;
                _map.PointerReleasedOverride += MapPointerReleasedOverride;
            }

            IsDragging = true;
            CurrentLocation = e.GetCurrentPoint(_map).Position;
            if (DragStarted != null)
                DragStarted(this);
        }

        private void MapPointerMovedOverride(object sender, PointerRoutedEventArgs e)
        {
            if (IsDragging && _map != null)
            {
                if (IsDragging && _map != null)
                {
                    Point currentPoint = e.GetCurrentPoint(_map).Position;
                    Point transferedPoint = new Point(currentPoint.X - Width / 2,
                        currentPoint.Y - Height / 2);
                    Location location;

                    if (_map.TryPixelToLocation(transferedPoint, out location))
                        MapLayer.SetPosition(this, location);
                }

                if (Dragging != null)
                    Dragging(e);
            }
        }

        private void MapViewChangeStarted(object sender, ViewChangeStartedEventArgs e)
        {
            if (_mapCenter != null)
                _map.Center = _mapCenter;
        }

        private void MapPointerReleasedOverride(object sender, PointerRoutedEventArgs e)
        {
            if (_map != null)
            {
                _map.ViewChangeStarted -= MapViewChangeStarted;
                _map.PointerMovedOverride -= MapPointerMovedOverride;
                _map.PointerReleasedOverride -= MapPointerReleasedOverride;
            }

            IsDragging = false;

            if (DragCompleted != null)
            {
                DragCompleted(e);
            }
        }

        #endregion

        #region Public Methods

        public void ShowPanel()
        {
            VisualStateManager.GoToState(this, "Holding", true);
        }

        public void HidePanel()
        {
            VisualStateManager.GoToState(this, "Moving", true);
        }

        #endregion
    }
}