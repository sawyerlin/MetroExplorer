using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using MetroExplorer.Model.MapModel;

namespace MetroExplorer.Components.Maps
{
    public sealed class LinkPanel : Control
    {
        public static readonly DependencyProperty MapPinModelProperty = DependencyProperty.Register(
            "MapPinModel", typeof(MapPinModel), typeof(LinkPanel), new PropertyMetadata(default(MapPinModel)));

        public MapPinModel MapPinModel
        {
            get { return (MapPinModel)GetValue(MapPinModelProperty); }
            set { SetValue(MapPinModelProperty, value); }
        }

        public LinkPanel()
        {
            DefaultStyleKey = typeof(LinkPanel);
        }
    }
}