namespace MetroExplorer.Model.MapModel
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using Windows.Devices.Geolocation;
    using Windows.UI.Xaml.Media;

    public class MapPinModel : INotifyPropertyChanged
    {
        private Geoposition _position;
        private ImageBrush _image;
        private string _address;
        private List<string> _folders;

        public Geoposition Position
        {
            get { return _position; }
            set
            {
                _position = value;
                NotifyPropertyChanged("Position");
            }
        }

        public ImageBrush Image
        {
            get { return _image; }
            set
            {
                _image = value;
                NotifyPropertyChanged("Image");
            }
        }

        public string Address
        {
            get { return _address; }
            set
            {
                _address = value;
                NotifyPropertyChanged("Image");
            }
        }

        public List<string> Folders
        {
            get { return _folders; }
            set
            {
                _folders = value;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this,
                    new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
