using ProcessDataArchiver.DataCore.DbEntities;
using ProcessDataArchiver.DataCore.DbEntities.Tags;
using ProcessDataArchiver.WinGui.ViewModels;
using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using System.Xml.Linq;

namespace ProcessDataArchiver.WinGui.Windows.Dialogs
{
    /// <summary>
    /// Interaction logic for SelectSummaryDialog.xaml
    /// </summary>
    public partial class SelectSummaryDialog : Window
    {
        private KeyValuePair<string, TimeSpan> selectedCycle;
        private bool loaded;
        public IEnumerable<ITag> Tags { get; set; }
        public ICollectionView FilteredTags { get; set; }

        public IDictionary<string, TimeSpan> CycleTimes { get; set; }

        public KeyValuePair<string, TimeSpan> SelectedCycle
        {
            get { return selectedCycle; }
            set
            {
                selectedCycle = value;
            }
        }

        public bool Applied { get; set; }
        public SummaryInfoViewModel TagSummaryInfo { get; set; }
        public IList<SummaryInfoViewModel> TagSummaryInfos { get; set; } 
            = new List<SummaryInfoViewModel>();


        public SelectSummaryDialog()
        {
            InitializeComponent();
            loaded = true;
            this.DataContext = this;
            Tags = EntityContext.GetContext().Tags.Where(t=>t.TagType == TagType.Analog);
            FilteredTags = CollectionViewSource.GetDefaultView(Tags);
            SetCycleTimes();
        }

        private void SetCycleTimes()
        {
            CycleTimes = new Dictionary<string, TimeSpan>();
            CycleTimes.Add("1 Minuta", new TimeSpan(0, 1, 0));
            CycleTimes.Add("2 Minuty", new TimeSpan(0, 2, 0));
            CycleTimes.Add("5 Minut", new TimeSpan(0, 5, 0));
            CycleTimes.Add("10 Minut", new TimeSpan(0, 10, 0));
            CycleTimes.Add("15 Minut", new TimeSpan(0, 15, 0));
            CycleTimes.Add("20 Minut", new TimeSpan(0, 20, 0));
            CycleTimes.Add("30 Minut", new TimeSpan(0, 30, 0));
            CycleTimes.Add("1 Godzina", new TimeSpan(1, 0, 0));
            CycleTimes.Add("2 Godziny", new TimeSpan(2, 0, 0));
            CycleTimes.Add("6 Godzin", new TimeSpan(6, 0, 0));
            CycleTimes.Add("12 Godzin", new TimeSpan(12, 0, 0));
            CycleTimes.Add("1 Dzień", new TimeSpan(1, 0, 0,0));
            CycleTimes.Add("2 Dni", new TimeSpan(1, 0, 0, 0));
            CycleTimes.Add("1 Tydzień", new TimeSpan(7, 0, 0, 0));
            CycleTimes.Add("2 Tygodnie", new TimeSpan(14, 0, 0, 0));
            CycleTimes.Add("1 Miesiac", new TimeSpan(30, 0, 0, 0));
            TimeSpanCb.SelectedIndex = 0;
        }


        private void SetFilter(string text, string gv)
        {
            if (!string.IsNullOrEmpty(text) && string.IsNullOrEmpty(gv))
            {
                FilteredTags.Filter = (t) =>
                {
                    var tag = t as ITag;
                    if (tag.Name.StartsWith(text))
                        return true;
                    return false;
                };
            }
            else if (string.IsNullOrEmpty(text) && string.IsNullOrEmpty(gv))
            {
                FilteredTags.Filter = null;
            }

            else if (string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(gv))
            {
                FilteredTags.Filter = (t) =>
                {
                    var tag = t as ITag;
                    if (tag.GlobalVariable.Name.StartsWith(gv))
                        return true;
                    return false;
                };
            }
            else if (!string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(gv))
            {
                FilteredTags.Filter = (t) =>
                {
                    var tag = t as ITag;
                    if (tag.Name.StartsWith(text) && tag.GlobalVariable.Name.StartsWith(gv))
                        return true;
                    return false;
                };
            }
        }

        private void FilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (loaded)
            {
               
                SetFilter(FilterTextBox.Text,GvFilterTextBox.Text);
            }
        }

        private void TagListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SetInfo();
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            SetInfo();
        }

        private void SetInfo()
        {
            var selected = TagListView.SelectedItems;
            var span = ((KeyValuePair<string, TimeSpan>)TimeSpanCb.SelectedItem).Value;
            var action = (ActionTypeCb.SelectedItem as ComboBoxItem).Name.ToString();

            if (selected != null)
            {
                foreach (var item in selected)
                {
                    TagSummaryInfos.Add(new SummaryInfoViewModel
                    {
                        ArchiveName = (item as ITag).TagArchive.Name,
                        Action = action,
                        TagName = (item as ITag).Name,
                        TimeSpan = span
                    });
                }
                TagSummaryInfo = TagSummaryInfos.FirstOrDefault();
            }

            Applied = true;


            this.Close();
        }


        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            TagSummaryInfo = null;
            this.Close();
        }

        private void TagListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var sel = TagListView.SelectedItem;
            if (sel != null && !SelectButton.IsEnabled)
            {
                SelectButton.IsEnabled = true;
            }
        }
    }
}
