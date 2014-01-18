namespace MetroExplorer.Core.Objects
{
    using System;
    using System.ComponentModel;
    using System.Xml.Serialization;
    using Windows.Storage;
    using Windows.UI.Xaml.Media.Imaging;

    public class HomeItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _name;
        private string _path;
        [XmlIgnore]
        private StorageFolder _storageFolder;
        [XmlIgnore]
        private BitmapImage _image;
        [XmlIgnore]
        private string _imageStretch = "None";

        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                NotifyPropertyChanged("Name");
            }
        }

        public string Path
        {
            get { return _path; }
            set
            {
                _path = value;
                NotifyPropertyChanged("ImagePath");
            }
        }

        [XmlIgnore]
        public StorageFolder StorageFolder
        {
            get { return _storageFolder; }
            set
            {
                _storageFolder = value;
                NotifyPropertyChanged("StorageFolder");
            }
        }

        [XmlIgnore]
        public BitmapImage Image
        {
            get { return _image; }
            set
            {
                _image = value;
                NotifyPropertyChanged("Image");
            }
        }

        [XmlIgnore]
        public string ImageStretch
        {
            get { return _imageStretch; }
            set
            {
                _imageStretch = value;
                NotifyPropertyChanged("ImageStretch");
            }
        }

        public string SubImageName = "";

        public void NotifyPropertyChanged(String changedPropertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(changedPropertyName));
            }
        }

        private string _ifImageChanged = "Visible";
        public string IfImageChanged
        {
            get { return _ifImageChanged; }
            set
            {
                _ifImageChanged = value;
                NotifyPropertyChanged("IfImageChanged");
            }
        }

        private BitmapImage _defautImage;
        public BitmapImage DefautImage
        {
            get { return _defautImage; }
            set
            {
                _defautImage = value;
                NotifyPropertyChanged("DefautImage");
            }
        }
    }
}
