using ProcessDataArchiver.DataCore.DbEntities;
using ProcessDataArchiver.DataCore.DbEntities.Tags;
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

namespace ProcessDataArchiver.WinGui.Windows.Dialogs
{
    /// <summary>
    /// Interaction logic for SelectTagDialog.xaml
    /// </summary>
    public partial class SelectTagDialog : Window
    {

        private bool loaded;
        public IEnumerable<ITag> Tags { get; set; }
        public ICollectionView FilteredTags { get; set; }

        public IList<ITag> SelectedTags { get; set; } = new List<ITag>();

        public List<string> ArchiveNames { get; set; } = new List<string>();

        public bool Applied { get; set; }
        public SelectTagDialog()
        {
            InitializeComponent();

            loaded = true;
            ArchiveNames.Add("-");
            ArchiveNames.AddRange(EntityContext.GetContext().TagArchives.Select(ta => ta.Name));
            this.DataContext = this;
            Tags = EntityContext.GetContext().Tags.Where(t => t.TagType == TagType.Analog);
            FilteredTags = CollectionViewSource.GetDefaultView(Tags);

        }



        private void SetFilter(string text, string gv,string archive)
        {
            if (!string.IsNullOrEmpty(text) && string.IsNullOrEmpty(gv) && archive.Equals("-"))
            {
                FilteredTags.Filter = (t) =>
                {
                    var tag = t as ITag;
                    if (tag.Name.StartsWith(text))
                        return true;
                    return false;
                };
            }
            else if (string.IsNullOrEmpty(text) && string.IsNullOrEmpty(gv) && archive.Equals("-"))
            {
                FilteredTags.Filter = null;
            }

            else if (string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(gv) && archive.Equals("-"))
            {
                FilteredTags.Filter = (t) =>
                {
                    var tag = t as ITag;
                    if (tag.GlobalVariable.Name.StartsWith(gv))
                        return true;
                    return false;
                };
            }
            else if (!string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(gv) && archive.Equals("-"))
            {
                FilteredTags.Filter = (t) =>
                {
                    var tag = t as ITag;
                    if (tag.Name.StartsWith(text) && tag.GlobalVariable.Name.StartsWith(gv))
                        return true;
                    return false;
                };
            }
            else if (!string.IsNullOrEmpty(text) && string.IsNullOrEmpty(gv) && !archive.Equals("-"))
            {
                FilteredTags.Filter = (t) =>
                {
                    var tag = t as ITag;
                    if (tag.Name.StartsWith(text) && tag.TagArchive.Name.Equals(archive))
                        return true;
                    return false;
                };
            }
            else if (string.IsNullOrEmpty(text) && string.IsNullOrEmpty(gv) && !archive.Equals("-"))
            {
                FilteredTags.Filter = (t)=> {
                    var tag = t as ITag;
                    if (tag.TagArchive.Name.Equals(archive))
                        return true;
                    return false;
                };
            }

            else if (string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(gv) && !archive.Equals("-"))
            {
                FilteredTags.Filter = (t) =>
                {
                    var tag = t as ITag;
                    if (tag.GlobalVariable.Name.StartsWith(gv) && tag.TagArchive.Name.Equals(archive))
                        return true;
                    return false;
                };
            }
            else if (!string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(gv) && !archive.Equals("-"))
            {
                FilteredTags.Filter = (t) =>
                {
                    var tag = t as ITag;
                    if (tag.Name.StartsWith(text) && tag.GlobalVariable.Name.StartsWith(gv)
                    && tag.TagArchive.Name.Equals(archive))
                        return true;
                    return false;
                };
            }
        }

        private void FilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (loaded)
            {
                string archive = ArchiveCb.SelectedItem.ToString();
                SetFilter(FilterTextBox.Text, GvFilterTextBox.Text,archive);
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


            if (selected != null)
            {
                foreach (var item in selected)
                {
                    SelectedTags.Add(item as ITag);
                }

            }

            Applied = true;

            this.Close();
        }


        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
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

        private void ArchiveCb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (loaded)
            {
                string archive = ArchiveCb.SelectedItem.ToString();
                SetFilter(FilterTextBox.Text, GvFilterTextBox.Text, archive);
            }
        }
    }
}
