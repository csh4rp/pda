using ProcessDataArchiver.DataCore.Acquisition;
using ProcessDataArchiver.DataCore.DbEntities;
using ProcessDataArchiver.DataCore.DbEntities.Events;
using ProcessDataArchiver.WinGui.Windows.Commands;
using ProcessDataArchiver.WinGui.Windows.Dialogs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ProcessDataArchiver.WinGui.Windows.DockingControls
{
    /// <summary>
    /// Interaction logic for EventsControl.xaml
    /// </summary>
    public partial class EventsControl : UserControl
    {
        private bool loaded;


        private EntityContext context;

        public ObservableCollection<IEvent> Events { get; }
        public ICollectionView FilteredEvents { get; set; }

        private IoServer server { get; }
        public EventsControl()
        {
            InitializeComponent();
            loaded = true;

            context = EntityContext.GetContext();

            server = IoServer.GetIoServer();
            server.StateChanged += EventsControl_StateChanged;
            if (server.IsStarted)
            {
                AddEventsButton.IsEnabled = false;
                RemoveEventsButton.IsEnabled = false;
            }


            Events = new ObservableCollection<IEvent>();
            this.DataContext = this;
            if(context.Events.Count>0)
                foreach (var ev in context.Events)
                {
                    Events.Add(ev);
                }

            FilteredEvents = CollectionViewSource.GetDefaultView(Events);
            
            
        }

        private void EventsControl_StateChanged(object sender, EventArgs e)
        {
            if (server.IsStarted)
            {
                AddEventsButton.IsEnabled = false;
                if(EventsListView.SelectedItem!=null)
                    RemoveEventsButton.IsEnabled = false;
            }
            else
            {
                AddEventsButton.IsEnabled = true;
                RemoveEventsButton.IsEnabled = true;
            }
        }

        public void Filter(string text,string type)
        {

            if (Events.Count > 0)
            {

                if (!string.IsNullOrEmpty(text) && type.Equals("All"))
                {
                    FilteredEvents.Filter = (e) =>
                    {
                        var ev = e as IEvent;
                        if (ev.Name.StartsWith(text))
                            return true;
                        return false;
                    };
                }
                else if (!string.IsNullOrEmpty(text) && type.Equals("Discrete"))
                {
                    FilteredEvents.Filter = (e) =>
                    {
                        var ev = e as IEvent;
                        if (ev.Name.StartsWith(text) && ev.EventType == EventType.Discrete)
                            return true;
                        return false;
                    };
                }
                else if (!string.IsNullOrEmpty(text) && type.Equals("Analog"))
                {
                    FilteredEvents.Filter = (e) =>
                    {
                        var ev = e as IEvent;
                        if (ev.Name.StartsWith(text) && ev.EventType == EventType.Analog)
                            return true;
                        return false;
                    };
                }
                else if (!string.IsNullOrEmpty(text) && type.Equals("Cyclic"))
                {
                    FilteredEvents.Filter = (e) =>
                    {
                        var ev = e as IEvent;
                        if (ev.Name.StartsWith(text) && ev.EventType == EventType.Cyclic)
                            return true;
                        return false;
                    };
                }
                else if (string.IsNullOrEmpty(text) && type.Equals("All"))
                {
                    FilteredEvents.Filter = null;
                }
                else if (string.IsNullOrEmpty(text) && type.Equals("Discrete"))
                {
                    FilteredEvents.Filter = (e) =>
                    {
                        var ev = e as IEvent;
                        if (ev.EventType == EventType.Discrete)
                            return true;
                        return false;
                    };
                }
                else if (string.IsNullOrEmpty(text) && type.Equals("Analog"))
                {
                    FilteredEvents.Filter = (e) =>
                    {
                        var ev = e as IEvent;
                        if (ev.EventType == EventType.Analog)
                            return true;
                        return false;
                    };
                }
                else if (string.IsNullOrEmpty(text) && type.Equals("Cyclic"))
                {
                    FilteredEvents.Filter = (e) =>
                    {
                        var ev = e as IEvent;
                        if (ev.EventType == EventType.Cyclic)
                            return true;
                        return false;
                    };
                }
            }
        }

        private void EventsListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!server.IsStarted)
            {
                var selected = EventsListView.SelectedItem as IEvent;

                if (selected != null)
                {
                    var evDialog = new EventDialog(selected);
                    evDialog.ShowDialog();
                    if (evDialog.TypeChanged)
                    {
                        Events.Remove(selected);
                        context.Events.Remove(selected);
                        var newEvent = evDialog.NewEvent;
                        Events.Add(newEvent);
                        context.Events.Add(newEvent);
                    }
                    else if (evDialog.Applied)
                    {
                        selected.Copy(evDialog.NewEvent);

                        Events.Clear();
                        if (context.Events.Count > 0)
                            foreach (var ev in context.Events)
                            {
                                Events.Add(ev);
                            }
                        FilteredEvents = CollectionViewSource.GetDefaultView(Events);
                    }
                }
            }
        }



        private void EventsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var sel = EventsListView.SelectedItems;
            if (sel != null && !RemoveEventsButton.IsEnabled && !server.IsStarted)
            {
                RemoveEventsButton.IsEnabled = true;
            }
            else if (sel  == null && RemoveEventsButton.IsEnabled)
            {
                RemoveEventsButton.IsEnabled = false;
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var evDialog = new EventDialog();
            evDialog.ShowDialog();
            if (evDialog.Applied)
            {
                var newEvent = evDialog.NewEvent;
                Events.Add(newEvent);
                context.Events.Add(newEvent);
            }
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = EventsListView.SelectedItems;

            if (selected != null)
            {
                RemoveItems(selected);
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (loaded)
            {

                    string type = (SelectEventCb.SelectedItem as ComboBoxItem).Name;
                    string name = SearchEventTb.Text;

                    Filter(name, type);
            }
        }

        private void Cb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (loaded)
            {
                string type = (SelectEventCb.SelectedItem as ComboBoxItem).Name;
                string name = SearchEventTb.Text;
                Filter(name, type);
            }
        }

        private void EventsListView_KeyDown(object sender, KeyEventArgs e)
        {
            var selected = EventsListView.SelectedItems;
            if (e.Key == Key.Delete && selected != null &&!server.IsStarted)
            {
                RemoveItems(selected);
            }
        }

        private void RemoveItems(IList items)
        {

            var t = new List<IEvent>();
            foreach (var item in items)
            {
                t.Add(item as IEvent);
            }

            foreach (var item in t)
            {
                Events.Remove(item);
                EntityContext.GetContext().Events.Remove(item);
            }
        }
    }
}
