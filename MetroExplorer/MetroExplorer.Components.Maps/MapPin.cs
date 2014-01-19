namespace MetroExplorer.Components.Maps
{
    using System;
    using Windows.Foundation;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Input;
    using Bing.Maps;
    using DataSource.DataModels;
    using Objects;

    public sealed class MapPin : Control
    {
        #region Fields

        private readonly Map _map;
        private Location _mapCenter;

        #endregion

        #region Properties

        public MapLocationModel LocationModel { get; set; }

        public bool Marked { get; private set; }

        public bool Focused { get; private set; }

        public bool IsDragging { get; private set; }

        #endregion

        #region EventHandler

        public Action<MapPin> DragStarted;

        public Action<PointerRoutedEventArgs> Dragging;

        public Action<PointerRoutedEventArgs> DragCompleted;

        public event EventHandler GetFocused;

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
                    {
                        MapLayer.SetPosition(this, location);
                    }
                }

                if (Dragging != null)
                {
                    Dragging(e);
                }
            }
        }

        private void MapViewChangeStarted(object sender, ViewChangeStartedEventArgs e)
        {
            if (_mapCenter != null)
            {
                _map.Center = _mapCenter;
            }
        }

        private void MapPointerReleasedOverride(object sender, PointerRoutedEventArgs e)
        {
            if (_map != null)
            {
                _map.PointerMovedOverride -= MapPointerMovedOverride;
                _map.PointerReleasedOverride -= MapPointerReleasedOverride;
            }

            IsDragging = false;

            if (DragCompleted != null)
            {
                DragCompleted(e);
            }
        }

        public void Focus()
        {
            if (!Focused)
            {
                Focused = true;
                VisualStateManager.GoToState(this, "Focused", true);
                if (GetFocused != null)
                    GetFocused(this, new EventArgs());
            }
        }

        public void UnFocus()
        {
            if (Focused)
            {
                Focused = false;
                VisualStateManager.GoToState(this, "UnFocused", true);
            }
        }

        public void Mark()
        {
            Marked = true;
            VisualStateManager.GoToState(this, "Marked", true);
        }

        public void UnMark()
        {
            Marked = false;
            VisualStateManager.GoToState(this, "UnMarked", true);
        }
    }
}
