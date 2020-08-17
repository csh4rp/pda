using ProcessDataArchiver.DataCore.Acquisition;
using ProcessDataArchiver.DataCore.DbEntities;
using ProcessDataArchiver.DataCore.DbEntities.Tags;
using ProcessDataArchiver.WinGui.ViewModels;
using ProcessDataArchiver.WinGui.Windows.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
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
    /// Interaction logic for LookupTableControl.xaml
    /// </summary>
    public partial class LookupTableControl : UserControl
    {
        public ObservableCollection<TagVariable> Tags { get; set; } =
            new ObservableCollection<TagVariable>();

        public ObservableCollection<string> ArchiveNames { get; set; } =
            new ObservableCollection<string>();

       

        private Timer timer;
        private bool isRunning;

        public LookupTableControl()
        {
            InitializeComponent();

            var archives = EntityContext.GetContext().TagArchives.Select(a => a.Name);
            foreach (var item in archives)
            {
                ArchiveNames.Add(item);
            }
            
            this.DataContext = this;
            IoServer.GetIoServer().GlobalVariablesReaded += LookupTableControl_GlobalVariablesReaded;
            
        }

        private void LookupTableControl_GlobalVariablesReaded(object sender, EventArgs e)
        {
            foreach (var tag in Tags)
            {
                tag.Refresh();
            }
        }

        private void AddTag_Click(object sender, RoutedEventArgs e)
        {
            var v = new TagVariable();
            Tags.Add(v);
        }

        private void RemoveTag_Click(object sender, RoutedEventArgs e)
        {
            var tag = TagListView.SelectedItem as TagVariable;

            if (tag!=null)
            {
                Tags.Remove(tag);
            }
        }

        private void SetEnability()
        {


            if (TagListView.SelectedItem != null)
            {
                RemoveButton.IsEnabled = true;
            }
            else
            {
                RemoveButton.IsEnabled = false;
            }
        }

        private void TagListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetEnability();
        }


    }
}
