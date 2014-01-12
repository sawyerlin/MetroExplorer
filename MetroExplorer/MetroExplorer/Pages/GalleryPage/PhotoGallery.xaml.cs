namespace MetroExplorer.Pages.GalleryPage
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using Windows.Storage;
    using Windows.Storage.FileProperties;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Data;
    using Windows.UI.Xaml.Input;
    using Windows.UI.Xaml.Media;
    using Windows.UI.Xaml.Media.Imaging;
    using Common;
    using Core;
    using Core.Objects;
    using Core.Utils;
    using ExplorerPage;

    /// <summary>
    /// Page affichant une collection groupée d'éléments.
    /// </summary>
    public sealed partial class PhotoGallery : LayoutAwarePage, INotifyPropertyChanged
    {
        public static double ActualScreenHeight = 0;

        ObservableCollection<ExplorerItem> _galleryItems = new ObservableCollection<ExplorerItem>();
        public ObservableCollection<ExplorerItem> GalleryItems
        {
            get
            {
                return _galleryItems;
            }
            set
            {
                _galleryItems = value;
                NotifyPropertyChanged("GalleryItems");
            }
        }

        DispatcherTimer _sliderDispatcher = new DispatcherTimer();

        MediaElement _currentPlayMedia;
        Slider _currentVideoTimerSlider;

        public PhotoGallery(bool sliderpressed)
        {
            _sliderpressed = sliderpressed;
            InitializeComponent();
            DataContext = this;
            Loaded += PhotoGallery_Loaded;
            Unloaded += PhotoGallery_Unloaded;
        }

        void PhotoGallery_Unloaded(object sender, RoutedEventArgs e)
        {
        }

        void PhotoGallery_Loaded(object sender, RoutedEventArgs e)
        {
            LoadingProgressBar.Visibility = Visibility.Visible;
        }

        protected async override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
            EventLogger.OnActionEvent(EventLogger.FolderOpened);
            LoadingProgressBar.Visibility = Visibility.Visible;
            var expoloreItems = navigationParameter as List<ExplorerItem>;
            int photoCount = 0;
            if (expoloreItems != null)
                foreach (ExplorerItem item in expoloreItems)
                {
                    if (photoCount > 50)
                        break;
                    if (await PhotoThumbnail(item))
                    {
                        GalleryItems.Add(item);
                        photoCount++;
                    }
                }
            MyVariableGridView.ItemsSource = GalleryItems;
            ImageFlipVIew.ItemsSource = GalleryItems;
            ImageFlipVIew.SelectedIndex = -1;
            LoadingProgressBar.Visibility = Visibility.Collapsed;
            LoadingProgressBar.Opacity = 0;
        }

        protected override void SaveState(Dictionary<String, Object> pageState)
        {
            foreach (var item in GalleryItems)
            {
                item.Image = null;
            }
            GalleryItems.Clear();
            GalleryItems = null;
            MyVariableGridView.ItemsSource = null;
            ImageFlipVIew.ItemsSource = null;
            if (_sliderDispatcher != null)
            {
                _sliderDispatcher.Stop();
                _sliderDispatcher = null;
            }
            GC.Collect();
        }

        private async System.Threading.Tasks.Task<bool> PhotoThumbnail(ExplorerItem photo)
        {
            if (photo.StorageFile == null || (!photo.StorageFile.IsImageFile() && !photo.StorageFile.IsVideoFile())) return false;
            try
            {
                StorageItemThumbnail fileThumbnail = await photo.StorageFile.GetThumbnailAsync(ThumbnailMode.SingleItem, (uint)ActualScreenHeight, ThumbnailOptions.UseCurrentScale);
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.SetSource(fileThumbnail);
                photo.Image = bitmapImage;
                photo.Width = (bitmapImage.PixelHeight / bitmapImage.PixelWidth == 1) ? 1 : 2;
                photo.Height = (bitmapImage.PixelWidth / bitmapImage.PixelHeight == 1) ? 1 : 2;
                if (photo.Width == 1 && photo.Height == 1 && bitmapImage.PixelWidth > 600 && bitmapImage.PixelHeight > 600)
                {
                    photo.Width = 2;
                    photo.Height = 2;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void GoBack2(object sender, RoutedEventArgs e)
        {
            if (MyVariableGridView.Visibility == Visibility.Collapsed)
            {
                MyVariableGridView.Visibility = Visibility.Visible;
                ImageFlipVIew.Visibility = Visibility.Collapsed;
                SliderModeButton.Visibility = Visibility.Visible;
                UnSliderModeButton.Visibility = Visibility.Collapsed;
                if (_currentPlayMedia != null && (_currentPlayMedia.CurrentState != MediaElementState.Closed &&
                 _currentPlayMedia.CurrentState != MediaElementState.Stopped))
                    CloseAndUnloadLastMedia();
                ImageFlipVIew.SelectedIndex = -1;
            }
            else
            {
                Frame.Navigate(typeof(PageExplorer));
            }
        }

        private void SliderModeButton_Click(object sender, RoutedEventArgs e)
        {
            if (GalleryItems.Count == 0) return;
            SliderModeButton.Visibility = Visibility.Collapsed;
            UnSliderModeButton.Visibility = Visibility.Visible;

            MyVariableGridView.Visibility = Visibility.Collapsed;
            ImageFlipVIew.Visibility = Visibility.Visible;

            if (ImageFlipVIew.Items != null && ImageFlipVIew.SelectedItem == null && ImageFlipVIew.Items.Count > 0)
                ImageFlipVIew.SelectedIndex = 0;

            if (_sliderDispatcher == null)
                _sliderDispatcher = new DispatcherTimer();
            _sliderDispatcher.Tick += SliderDispatcher_Tick;
            _sliderDispatcher.Interval = new TimeSpan(0, 0, 0, 3);
            _sliderDispatcher.Start();
            if (BottomAppBar != null) BottomAppBar.IsOpen = false;
        }

        void SliderDispatcher_Tick(object sender, object e)
        {
            if (ImageFlipVIew != null && ImageFlipVIew.Items != null && ImageFlipVIew.Items.Count > 0)
            {
                if (ImageFlipVIew.Items.Count - 1 == ImageFlipVIew.SelectedIndex)
                    ImageFlipVIew.SelectedIndex = 0;
                else
                    ImageFlipVIew.SelectedIndex++;
            }
        }

        private void UnSliderModeButton_Click(object sender, RoutedEventArgs e)
        {
            UnSliderModeButton.Visibility = Visibility.Collapsed;
            SliderModeButton.Visibility = Visibility.Visible;
            if (_sliderDispatcher != null)
            {
                _sliderDispatcher.Stop();
                _sliderDispatcher = null;
            }
        }

        private void StartFlipView(ExplorerItem item)
        {
            if (ImageFlipVIew.ItemsSource == null)
                ImageFlipVIew.ItemsSource = GalleryItems;
            if (item != null && GalleryItems.Contains(item))
            {
                ImageFlipVIew.SelectedIndex = GalleryItems.IndexOf(item);
            }
            else if (ImageFlipVIew.Items != null && ImageFlipVIew.Items.Count > 0)
                ImageFlipVIew.SelectedIndex = 0;
        }

        private async void ImageFlipVIew_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_currentPlayMedia != null && (_currentPlayMedia.CurrentState != MediaElementState.Closed &&
                 _currentPlayMedia.CurrentState != MediaElementState.Stopped))
                CloseAndUnloadLastMedia();

            if (ImageFlipVIew.SelectedItem == null) return;
            var item = (ImageFlipVIew.SelectedItem as ExplorerItem);
            if (item != null && item.StorageFile.IsVideoFile())
            {
                var container = ImageFlipVIew.ItemContainerGenerator.ContainerFromItem(item);
                if (container == null)
                    return;
                var media = GetMediaElement(container);
                if (media == null) return;
                _currentVideoTimerSlider = GetSlider(container);
                media.SetSource(await item.StorageFile.OpenAsync(FileAccessMode.Read), item.StorageFile.FileType);
                if (_sliderDispatcher != null)
                    _sliderDispatcher.Stop();
                media.MediaFailed += media_MediaFailed;
                media.MediaEnded += media_MediaEnded;
                media.MediaOpened += media_MediaOpened;
                _currentPlayMedia = media;
            }

            GC.Collect();
        }

        private void CloseAndUnloadLastMedia()
        {
            if (_currentPlayMedia != null)
            {
                _currentPlayMedia.MediaFailed -= media_MediaFailed;
                _currentPlayMedia.MediaEnded -= media_MediaEnded;
                _currentPlayMedia.Stop();
                _currentPlayMedia.Source = null;
                _currentPlayMedia = null;
            }
            if (_currentVideoTimerSlider != null)
            {
                _currentVideoTimerSlider.ValueChanged -= _currentVideoTimerSlider_ValueChanged;
                _currentVideoTimerSlider.StepFrequency = 0;
                _currentVideoTimerSlider.Visibility = Visibility.Collapsed;
                _currentVideoTimerSlider = null;
            }
            StopTimerForVideoSlider();
        }

        void media_MediaEnded(object sender, RoutedEventArgs e)
        {
            if (_sliderDispatcher != null && UnSliderModeButton.Visibility == Visibility.Visible)
                _sliderDispatcher.Start();
            CloseAndUnloadLastMedia();
        }

        void media_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            if (_sliderDispatcher != null && UnSliderModeButton.Visibility == Visibility.Visible)
                _sliderDispatcher.Start();
            CloseAndUnloadLastMedia();
        }

        void media_MediaOpened(object sender, RoutedEventArgs e)
        {
            if (_sliderDispatcher != null && UnSliderModeButton.Visibility == Visibility.Visible)
                _sliderDispatcher.Stop();
            if (_currentPlayMedia != null && _currentVideoTimerSlider != null)
            {
                double absvalue = (int)Math.Round(
                    _currentPlayMedia.NaturalDuration.TimeSpan.TotalSeconds,
                    MidpointRounding.AwayFromZero);
                _currentVideoTimerSlider.Visibility = Visibility.Collapsed;
                _currentVideoTimerSlider.Maximum = absvalue;
                _currentVideoTimerSlider.ValueChanged += _currentVideoTimerSlider_ValueChanged;
                _currentVideoTimerSlider.StepFrequency = SliderFrequency(_currentPlayMedia.NaturalDuration.TimeSpan);
                SetupTimerForVideoSlider();

            }
        }

        void _currentVideoTimerSlider_ValueChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (_currentPlayMedia != null)
            {
                _currentPlayMedia.Position = TimeSpan.FromSeconds(e.NewValue);
            }
        }

        private double SliderFrequency(TimeSpan timevalue)
        {
            double stepfrequency;

            double absvalue = (int)Math.Round(
                timevalue.TotalSeconds, MidpointRounding.AwayFromZero);

            stepfrequency = (int)(Math.Round(absvalue / 100));

            if (timevalue.TotalMinutes >= 10 && timevalue.TotalMinutes < 30)
            {
                stepfrequency = 10;
            }
            else if (timevalue.TotalMinutes >= 30 && timevalue.TotalMinutes < 60)
            {
                stepfrequency = 30;
            }
            else if (timevalue.TotalHours >= 1)
            {
                stepfrequency = 60;
            }
            if (stepfrequency == 0) stepfrequency += 1;

            if (stepfrequency == 1)
            {
                stepfrequency = absvalue / 100;
            }
            return stepfrequency;
        }

        public MediaElement GetMediaElement(DependencyObject parent)
        {
            if (parent == null)
                return null;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(VisualTreeHelper.GetChild(VisualTreeHelper.GetChild(VisualTreeHelper.GetChild(parent, i), i), i), 1);
                if (child is MediaElement)
                    return child as MediaElement;
            }
            return null;
        }

        public Slider GetSlider(DependencyObject parent)
        {
            if (parent == null)
                return null;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(VisualTreeHelper.GetChild(VisualTreeHelper.GetChild(VisualTreeHelper.GetChild(parent, i), i), i), 2);
                if (child is Slider)
                    return child as Slider;
            }
            return null;
        }

        private void MyVariableGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            MyVariableGridView.Visibility = Visibility.Collapsed;
            ImageFlipVIew.Visibility = Visibility.Visible;
            if (e.ClickedItem != null)
                StartFlipView(e.ClickedItem as ExplorerItem);
        }

        private void MediaElement_PointerPressed(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (_currentPlayMedia != null)
            {
                if (_currentPlayMedia.CurrentState == MediaElementState.Playing)
                {
                    _currentPlayMedia.Pause();
                    _currentVideoTimerSlider.Visibility = Visibility.Visible;
                }
                else if (_currentPlayMedia.CurrentState == MediaElementState.Paused)
                {
                    _currentPlayMedia.Play();
                    _currentVideoTimerSlider.Visibility = Visibility.Collapsed;
                }
            }
        }

        private DispatcherTimer _timerForVideoSlider;

        private void SetupTimerForVideoSlider()
        {
            _timerForVideoSlider = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(_currentVideoTimerSlider.StepFrequency)
            };
            StartTimerForVideoSlider();
        }

        private void __timerForVideoSlider_Tick(object sender, object e)
        {
            if (!_sliderpressed && _currentVideoTimerSlider != null && _currentPlayMedia != null)
            {
                _currentVideoTimerSlider.Value = _currentPlayMedia.Position.TotalSeconds;
            }
        }

        private void StartTimerForVideoSlider()
        {
            _timerForVideoSlider.Tick += __timerForVideoSlider_Tick;
            _timerForVideoSlider.Start();
        }

        private void StopTimerForVideoSlider()
        {
            if (_timerForVideoSlider != null)
            {
                _timerForVideoSlider.Stop();
                _timerForVideoSlider.Tick -= __timerForVideoSlider_Tick;
            }
        }

        private readonly bool _sliderpressed;
    }

    public sealed partial class PhotoGallery
    {
        #region propertychanged
        private void NotifyPropertyChanged(String changedPropertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(changedPropertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        #endregion
    }

    public sealed class FileTypeVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value != null)
            {
                var storageFile = (value as StorageFile);
                return storageFile.IsVideoFile() ? "Visible" : "Collapsed";
            }
            return "Collapsed";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return !(value is bool && (bool)value);
        }
    }
}
