namespace MetroExplorer.Components.Maps
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Data;
    using Windows.UI.Xaml.Documents;
    using Windows.UI.Xaml.Input;
    using Windows.UI.Xaml.Media;
    using Bing.Maps;
    using Objects;

    public sealed class MapPin : Control
    {
        #region Fields

        private bool _isDragging = false;
        private Map _map;
        private Location _mapCenter;

        #endregion

        #region Properties

        public Guid ID { get; set; }

        public string PinName { get; private set; }

        public string Description { get; private set; }

        public string Latitude { get; private set; }

        public string Longitude { get; private set; }

        public bool Marked { get; private set; }

        public bool Focused { get; private set; }

        #endregion

        #region EventHandler

        public Action<PointerRoutedEventArgs> DragStarted;

        public Action<PointerRoutedEventArgs> Dragging;

        public Action<PointerRoutedEventArgs> DragCompleted;

        public event EventHandler<MapPinTappedEventArgs> MapPinTapped;

        public event EventHandler GetFocused;

        #endregion

        #region Constructors

        public MapPin()
        {
            this.DefaultStyleKey = typeof(MapPin);
        }

        public MapPin(Map map)
            : this()
        {
            _map = map;
        }

        public MapPin(
            string pinName,
            string description,
            string latitude,
            string longitude)
            : this()
        {
            PinName = pinName;
            Description = description;
            Latitude = latitude;
            Longitude = longitude;
        }

        #endregion

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            Tapped += OnTapped;
        }

        private void OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (Focused)
            {
                VisualStateManager.GoToState(this, Marked ? "UnMarked" : "Marked", true);
                Marked = !Marked;
            }
            if (MapPinTapped != null)
                MapPinTapped(this, new MapPinTappedEventArgs(Marked));
        }

        protected override void OnPointerPressed(PointerRoutedEventArgs e)
        {
            if (_map != null)
            {
                _mapCenter = _map.Center;
                _map.ViewChangeStarted += _map_ViewChangeStarted;
                _map.PointerMovedOverride += _map_PointerMovedOverride;
                _map.PointerReleasedOverride += _map_PointerReleasedOverride;
            }

            _isDragging = true;

            if (DragStarted != null)
                DragStarted(e);

            base.OnPointerPressed(e);
        }

        void _map_PointerMovedOverride(object sender, PointerRoutedEventArgs e)
        {
            if (_isDragging && _map != null)
            {
                var point = e.GetCurrentPoint(_map);
                Location location;

                if (_map.TryPixelToLocation(point.Position, out location))
                {
                    MapLayer.SetPosition(this, location);
                }

                if (Dragging != null)
                {
                    Dragging(e);
                }
            }
        }

        void _map_ViewChangeStarted(object sender, ViewChangeStartedEventArgs e)
        {
            if (_mapCenter != null)
            {
                _map.Center = _mapCenter;
            }
        }

        void _map_PointerReleasedOverride(object sender, PointerRoutedEventArgs e)
        {
            if (_map != null)
            {
                _mapCenter = null;
                _map.ViewChangeStarted -= _map_ViewChangeStarted;
                _map.PointerMovedOverride -= _map_PointerMovedOverride;
                _map.PointerReleasedOverride -= _map_PointerReleasedOverride;
            }

            _isDragging = false;

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
