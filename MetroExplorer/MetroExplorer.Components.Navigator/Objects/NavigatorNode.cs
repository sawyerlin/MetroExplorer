﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.UI.Xaml.Media;

namespace MetroExplorer.Components.Navigator.Objects
{
    public sealed class NavigatorNode
    {
        public string NodeName { get; set; }
        public ICommand NodeAction { get; private set; }
        public int NodeIndex { get; private set; }
        public IEnumerable<string> ItemList { get; private set; }
        public Brush Background { get; private set; }

        public NavigatorNode(
            int index,
            string name,
            ICommand action,
            Brush background)
        {
            NodeIndex = index;
            NodeName = name;
            NodeAction = action;
            Background = background;
        }

        public NavigatorNode(
            int index,
            string name,
            ICommand action,
            Brush background,
            IEnumerable<string> itemList)
            : this(index, name, action, background)
        {
            ItemList = itemList;
        }
    }
}
