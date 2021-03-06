﻿namespace MetroExplorer.Components.Navigator
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Controls.Primitives;
    using Windows.UI.Xaml.Media;
    using Windows.UI.Xaml.Media.Animation;
    using Objects;

    public sealed class Navigator : ItemsControl
    {
        #region Constants

        private const string LayoutRootElement = "LayoutRoot";

        private const string PopupListElement = "PopupList";

        private const string ListBoxDropDownElement = "ListBoxDropDown";

        private const string ShowListStoryboard = "StoryboardShowlist";

        private const string HideListStoryboard = "StoryboardHidelist";

        #endregion

        #region Fields

        private IEnumerable<NavigatorNode> _path;
        private int _currentIndex;
        private NavigatorNodeCommandType _commandType;

        private Grid _layoutRoot;
        private Popup _popupList;
        private ListBox _listBoxDropDown;

        private Storyboard _showListStoryboard;
        private Storyboard _hideListStoryboard;

        private NavigatorButton _droppedButton;

        #endregion

        #region EventHandlers

        public event EventHandler<NavigatorNodeCommandArgument> NPathChanged;

        #endregion

        #region DependencyProperties

        public static readonly DependencyProperty PathProperty =
            DependencyProperty.Register("Path", typeof(string), typeof(Navigator),
            new PropertyMetadata(string.Empty, PathChanged));

        private static void PathChanged(
            DependencyObject obj,
            DependencyPropertyChangedEventArgs e
            )
        {
            Navigator navigator = (Navigator)obj;
            if (navigator != null)
                navigator.NavigatorPathChanged(e);
        }

        public void NavigatorPathChanged(
            DependencyPropertyChangedEventArgs e)
        {
            NavigatorNodeCommandArgument argument =
                new NavigatorNodeCommandArgument(
                    _currentIndex,
                    (string)e.NewValue,
                    _commandType);

            int index = 0,
                size = ItemListArray.Count();
            List<NavigatorNode> nodes = new List<NavigatorNode>();
            foreach (string value in argument.Path.Split('\\').Where(value => !string.IsNullOrWhiteSpace(value)))
            {
                NavigatorNodeCommand command = new NavigatorNodeCommand();
                command.Command += (sender, args) =>
                {
                    _currentIndex = args.Index;

                    _commandType = args.CommandType;
                    switch (_commandType)
                    {
                        case NavigatorNodeCommandType.Reduce:
                            string newPath = Path.Substring(0, Path.IndexOf(args.Path, StringComparison.Ordinal) + args.Path.Length);
                            Path = newPath;
                            break;
                        case NavigatorNodeCommandType.ShowList:
                            double positionX = args.PointerPositionX;
                            if (_popupList != null && ItemListArray[_currentIndex].Count > 0)
                            {
                                _listBoxDropDown.ItemsSource = ItemListArray[_currentIndex];
                                _popupList.Margin = new Thickness(positionX - _popupList.Width, ActualHeight, 0, -342.0);
                                _popupList.IsOpen = true;
                                _droppedButton = args.Button;
                                _droppedButton.BeginShowAnimation();
                            }
                            break;
                    }

                };
                if (index < size)
                    nodes.Add(new NavigatorNode(index, value, command, Background, index == size - 1, ItemListArray[index]));
                index++;
            }

            _path = nodes;
            ItemsSource = _path;
            if (NPathChanged != null)
                NPathChanged(this, argument);
        }

        public string Path
        {
            get { return (string)GetValue(PathProperty); }
            set { SetValue(PathProperty, value); }
        }


        public static readonly DependencyProperty DropBackgroundProperty =
            DependencyProperty.Register("DropBackground", typeof(Brush), typeof(NavigatorItem),
                                        new PropertyMetadata(new SolidColorBrush()));

        public Brush DropBackground
        {
            get { return (Brush)GetValue(DropBackgroundProperty); }
            set { SetValue(DropBackgroundProperty, value); }
        }

        #endregion

        #region Properties

        public List<string>[] ItemListArray { get; set; }

        #endregion

        #region Constructors

        public Navigator()
        {
            DefaultStyleKey = typeof(Navigator);
            _commandType = NavigatorNodeCommandType.None;
        }

        #endregion

        #region Override Methods

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _layoutRoot = (Grid)GetTemplateChild(LayoutRootElement);
            if (_layoutRoot != null)
            {
                _showListStoryboard = (Storyboard)_layoutRoot.Resources[ShowListStoryboard];
                _hideListStoryboard = (Storyboard)_layoutRoot.Resources[HideListStoryboard];
            }
            _popupList = (Popup)GetTemplateChild(PopupListElement);
            if (_popupList != null)
            {
                _popupList.Opened += PopupListOpened;
                _popupList.Closed += PopupListClosed;
            }
            _listBoxDropDown = (ListBox)GetTemplateChild(ListBoxDropDownElement);
            if (_listBoxDropDown == null) return;
            _listBoxDropDown.SelectionChanged += ListBoxDropDownSelectionChanged;
        }

        #endregion

        #region Events

        private void ListBoxDropDownSelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            _commandType = NavigatorNodeCommandType.Change;

            IEnumerable<string> splitedPath = Path.Split('\\').Take(_currentIndex + 1);
            string newPath = splitedPath.Aggregate(string.Empty, (current, next) => next + "\\")
                + (string.IsNullOrWhiteSpace(((string)e.AddedItems.FirstOrDefault())) ?
                string.Empty : (string)(e.AddedItems.FirstOrDefault()));
            NavigatorNodeCommandArgument argument =
                new NavigatorNodeCommandArgument(_currentIndex, newPath, _commandType);
            if (NPathChanged != null)
                NPathChanged(this, argument);
        }

        private void PopupListOpened(object sender, object e)
        {
            if (_showListStoryboard != null)
            {
                _popupList.Opened -= PopupListOpened;
                _showListStoryboard.Begin();
                _showListStoryboard.Completed += ShowListStoryboardCompleted;
            }
        }

        private void PopupListClosed(object sender, object e)
        {
            if (_droppedButton != null)
            {
                _droppedButton.BeginHideAnimation();
                _droppedButton = null;
            }
        }

        private void ShowListStoryboardCompleted(object sender, object e)
        {
            _popupList.Opened += PopupListOpened;
            _showListStoryboard.Completed -= ShowListStoryboardCompleted;
        }

        #endregion
    }
}
