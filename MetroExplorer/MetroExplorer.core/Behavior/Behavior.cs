namespace MetroExplorer.Core.Behavior
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Data;
    using Windows.UI.Xaml.Media;

    /// http://winrtbehaviors.codeplex.com/SourceControl/changeset/view/19567#261274
    public abstract class Behavior : FrameworkElement
    {
        private FrameworkElement _associatedObject;

        /// <summary>
        /// The associated object
        /// </summary>
        public FrameworkElement AssociatedObject
        {
            get
            {
                return _associatedObject;
            }
            set
            {
                if (_associatedObject != null)
                {
                    OnDetaching();
                }
                DataContext = null;

                _associatedObject = value;
                if (_associatedObject != null)
                {
                    // FIX LocalJoost 17-08-2012 moved ConfigureDataContext to OnLoaded
                    // to prevent the app hanging on a behavior attached to an element#
                    // that's not directly loaded (like a FlipViewItem)
                    OnAttached();
                }
            }
        }

        protected virtual void OnAttached()
        {
            AssociatedObject.Unloaded += AssociatedObjectUnloaded;
            AssociatedObject.Loaded += AssociatedObjectLoaded;
        }

        protected virtual void OnDetaching()
        {
            AssociatedObject.Unloaded -= AssociatedObjectUnloaded;
            AssociatedObject.Loaded -= AssociatedObjectLoaded;
        }

        private void AssociatedObjectLoaded(object sender, RoutedEventArgs e)
        {
            ConfigureDataContext();
        }

        private void AssociatedObjectUnloaded(object sender, RoutedEventArgs e)
        {
            OnDetaching();
        }

        /// <summary>
        /// Configures data context. 
        /// Courtesy of Filip Skakun
        /// http://twitter.com/xyzzer
        /// </summary>
        private async void ConfigureDataContext()
        {
            while (_associatedObject != null)
            {
                if (AssociatedObjectIsInVisualTree)
                {
                    Debug.WriteLine(_associatedObject.Name + " found in visual tree");
                    SetBinding(
                        DataContextProperty,
                        new Binding
                        {
                            Path = new PropertyPath("DataContext"),
                            Source = _associatedObject
                        });

                    return;
                }
                Debug.WriteLine(_associatedObject.Name + " Not in visual tree");
                await WaitForLayoutUpdateAsync();
            }
        }

        /// <summary>
        /// Checks if object is in visual tree
        /// Courtesy of Filip Skakun
        /// http://twitter.com/xyzzer
        /// </summary>
        private bool AssociatedObjectIsInVisualTree
        {
            get
            {
                if (_associatedObject != null)
                {
                    return Window.Current.Content != null && Ancestors.Contains(Window.Current.Content);
                }
                return false;
            }
        }

        /// <summary>
        /// Finds the object's associatedobject's ancestors
        /// Courtesy of Filip Skakun
        /// http://twitter.com/xyzzer
        /// </summary>
        private IEnumerable<DependencyObject> Ancestors
        {
            get
            {
                if (_associatedObject != null)
                {
                    var parent = VisualTreeHelper.GetParent(_associatedObject);

                    while (parent != null)
                    {
                        yield return parent;
                        parent = VisualTreeHelper.GetParent(parent);
                    }
                }
            }
        }

        /// <summary>
        /// Creates a task that waits for a layout update to complete
        /// Courtesy of Filip Skakun
        /// http://twitter.com/xyzzer
        /// </summary>
        /// <returns></returns>
        private async Task WaitForLayoutUpdateAsync()
        {
            await EventAsync.FromEvent<object>(
                eh => _associatedObject.LayoutUpdated += eh,
                eh => _associatedObject.LayoutUpdated -= eh);
        }
    }

    /// <summary>
    /// Base class for behaviors
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class Behavior<T> : Behavior where T : FrameworkElement
    {
        public new T AssociatedObject
        {
            get
            {
                return (T)base.AssociatedObject;
            }
            set
            {
                base.AssociatedObject = value;
            }
        }
    }

    /// <summary>
    ///  Based on: http://social.msdn.microsoft.com/Forums/sk/async/thread/30f3339c-5e04-4aa8-9a09-9be72d9d9a1b
    /// Courtesy of Filip Skakun
    /// http://twitter.com/xyzzer
    /// </summary>
    public static class EventAsync
    {
        /// <summary>
        /// Creates a <see cref="System.Threading.Tasks.Task"/>
        /// that waits for an event to occur.
        /// </summary>
        /// <example>
        /// <![CDATA[
        /// await EventAsync.FromEvent(
        ///     eh => storyboard.Completed += eh,
        ///     eh => storyboard.Completed -= eh,
        ///     storyboard.Begin);
        /// ]]>
        /// </example>
        /// <param name="addEventHandler">
        /// The action that subscribes to the event.
        /// </param>
        /// <param name="removeEventHandler">
        /// The action that unsubscribes from the event when it first occurs.
        /// </param>
        /// <param name="beginAction">
        /// The action to call after subscribing to the event.
        /// </param>
        /// <returns>
        /// The <see cref="System.Threading.Tasks.Task"/> that
        /// completes when the event registered in
        /// <paramref name="addEventHandler"/> occurs.
        /// </returns>
        public static Task<object> FromEvent<T>(
            Action<EventHandler<T>> addEventHandler,
            Action<EventHandler<T>> removeEventHandler,
            Action beginAction = null)
        {
            return new EventHandlerTaskSource<T>(
                addEventHandler,
                removeEventHandler,
                beginAction).Task;
        }

        /// <summary>
        /// Creates a <see cref="System.Threading.Tasks.Task"/>
        /// that waits for an event to occur.
        /// </summary>
        /// <example>
        /// <![CDATA[
        /// await EventAsync.FromEvent(
        ///     eh => button.Click += eh,
        ///     eh => button.Click -= eh);
        /// ]]>
        /// </example>
        /// <param name="addEventHandler">
        /// The action that subscribes to the event.
        /// </param>
        /// <param name="removeEventHandler">
        /// The action that unsubscribes from the event when it first occurs.
        /// </param>
        /// <param name="beginAction">
        /// The action to call after subscribing to the event.
        /// </param>
        /// <returns>
        /// The <see cref="System.Threading.Tasks.Task"/> that
        /// completes when the event registered in
        /// <paramref name="addEventHandler"/> occurs.
        /// </returns>
        public static Task<RoutedEventArgs> FromRoutedEvent(
            Action<RoutedEventHandler> addEventHandler,
            Action<RoutedEventHandler> removeEventHandler,
            Action beginAction = null)
        {
            return new RoutedEventHandlerTaskSource(
                addEventHandler,
                removeEventHandler,
                beginAction).Task;
        }

        private sealed class EventHandlerTaskSource<TEventArgs>
        {
            private readonly TaskCompletionSource<object> _tcs;
            private readonly Action<EventHandler<TEventArgs>> _removeEventHandler;

            public EventHandlerTaskSource(
                Action<EventHandler<TEventArgs>> addEventHandler,
                Action<EventHandler<TEventArgs>> removeEventHandler,
                Action beginAction = null)
            {
                if (addEventHandler == null)
                {
                    throw new ArgumentNullException("addEventHandler");
                }

                if (removeEventHandler == null)
                {
                    throw new ArgumentNullException("removeEventHandler");
                }

                _tcs = new TaskCompletionSource<object>();
                _removeEventHandler = removeEventHandler;
                addEventHandler.Invoke(EventCompleted);

                if (beginAction != null)
                {
                    beginAction.Invoke();
                }
            }

            /// <summary>
            /// Returns a task that waits for the event to occur.
            /// </summary>
            public Task<object> Task
            {
                get { return _tcs.Task; }
            }

            private void EventCompleted(object sender, TEventArgs args)
            {
                _removeEventHandler.Invoke(EventCompleted);
                _tcs.SetResult(args);
            }
        }

        private sealed class RoutedEventHandlerTaskSource
        {
            private readonly TaskCompletionSource<RoutedEventArgs> _tcs;
            private readonly Action<RoutedEventHandler> _removeEventHandler;

            public RoutedEventHandlerTaskSource(
                Action<RoutedEventHandler> addEventHandler,
                Action<RoutedEventHandler> removeEventHandler,
                Action beginAction = null)
            {
                if (addEventHandler == null)
                {
                    throw new ArgumentNullException("addEventHandler");
                }

                if (removeEventHandler == null)
                {
                    throw new ArgumentNullException("removeEventHandler");
                }

                _tcs = new TaskCompletionSource<RoutedEventArgs>();
                _removeEventHandler = removeEventHandler;
                addEventHandler.Invoke(EventCompleted);

                if (beginAction != null)
                {
                    beginAction.Invoke();
                }
            }

            /// <summary>
            /// Returns a task that waits for the routed to occur.
            /// </summary>
            public Task<RoutedEventArgs> Task
            {
                get { return _tcs.Task; }
            }

            private void EventCompleted(object sender, RoutedEventArgs args)
            {
                _removeEventHandler.Invoke(EventCompleted);
                _tcs.SetResult(args);
            }
        }
    }
}
