namespace MetroExplorer.Components.Maps.Objects
{
    using Windows.UI.Xaml;

    public class MapPinTappedEventArgs : RoutedEventArgs
    {
        public bool Marked { get; private set; }

        public MapPinTappedEventArgs(bool marked)
        {
            Marked = marked;
        }
    }
}
