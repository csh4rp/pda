using ProcessDataArchiver.DataCore.Acquisition;
using ProcessDataArchiver.DataCore.DbEntities;
using ProcessDataArchiver.DataCore.DbEntities.Tags;
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
    /// Interaction logic for TagsControl.xaml
    /// </summary>
    public partial class TagsControl : UserControl
    {
        private bool loaded;
        private TagArchive _archive;
        private IoServer server;
        private EntityContext context;

        public ObservableCollection<ITag> Tags { get; }
        public ICollectionView FilteredTags { get; set; }

        public TagsControl()
        {
            InitializeComponent();
            loaded = true;
            server = IoServer.GetIoServer();
            server.StateChanged += Server_StateChanged;

            if (server.IsStarted)
            {
                AddTagButton.IsEnabled = false;
                RemoveTagButton.IsEnabled = false;
            }

            context = EntityContext.GetContext();

            Tags = new ObservableCollection<ITag>();
            this.DataContext = this;
            
        }

        private void Server_StateChanged(object sender, EventArgs e)
        {
            if (server.IsStarted)
            {
                AddTagButton.IsEnabled = false;
                if(TagListView.SelectedItem!=null)
                    RemoveTagButton.IsEnabled = false;
            }
            else
            {
                AddTagButton.IsEnabled = false;
                RemoveTagButton.IsEnabled = false;
            }
        }

        public TagsControl(TagArchive archive) : this()
        {
            _archive = archive;
            if (_archive.Tags != null)
                foreach (var tag in _archive.Tags)
                {
                    Tags.Add(tag);
                }

            FilteredTags = CollectionViewSource.GetDefaultView(Tags);
            loaded = true;
        }



        public void Filter(string text,string type)
        {
            if (!string.IsNullOrEmpty(text) && type.Equals("All"))
            {
                FilteredTags.Filter = (t) =>
                {
                    var tag = t as ITag;
                    if (tag.Name.StartsWith(text))
                        return true;
                    return false;
                };
            }
            else if (!string.IsNullOrEmpty(text) && type.Equals("Discrete"))
            {
                FilteredTags.Filter = (t) =>
                {
                    var tag = t as ITag;
                    if (tag.Name.StartsWith(text) && tag.TagType == ProcessDataArchiver.DataCore.DbEntities.TagType.Discrete)
                        return true;
                    return false;
                };
            }
            else if (!string.IsNullOrEmpty(text) && type.Equals("Analog"))
            {
                FilteredTags.Filter = (t) =>
                {
                    var tag = t as ITag;
                    if (tag.Name.StartsWith(text) && tag.TagType == ProcessDataArchiver.DataCore.DbEntities.TagType.Analog)
                        return true;
                    return false;
                };
            }
            else if (string.IsNullOrEmpty(text)  && type.Equals("All"))
            {
                FilteredTags.Filter = null;
            }
            else if (string.IsNullOrEmpty(text) && type.Equals("Discrete"))
            {
                FilteredTags.Filter = (t) =>
                {
                    var tag = t as ITag;
                    if (tag.TagType == ProcessDataArchiver.DataCore.DbEntities.TagType.Discrete)
                        return true;
                    return false;
                };
            }
            else if (string.IsNullOrEmpty(text) && type.Equals("Analog"))
            {
                FilteredTags.Filter = (t) =>
                {
                    var tag = t as ITag;
                    if (tag.TagType == ProcessDataArchiver.DataCore.DbEntities.TagType.Analog)
                        return true;
                    return false;
                };
            }
           
        }

        private async void TagListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!server.IsStarted)
            {
                var tag = (sender as ListView).SelectedItem as ITag;

                if (tag != null)
                {
                    var tagdialog = new TagDialog(tag, this._archive);
                    tagdialog.ShowDialog();

                    var modTag = tagdialog.NewTag;
                    if (tagdialog.TypeChanged)
                    {
                        var cnx = await EntityContext.GetContextAsync();
                        cnx.Tags.Remove(tag);
                        cnx.Tags.Add(modTag);
                        Tags.Add(modTag);
                        Tags.Remove(tag);
                    }
                    else if (tagdialog.Applied)
                    {
                        tag.Copy(tagdialog.NewTag);

                        Tags.Clear();
                        if (context.Tags.Count > 0)
                            foreach (var t in context.Tags)
                            {
                                Tags.Add(t);
                            }
                        FilteredTags = CollectionViewSource.GetDefaultView(Tags);
                    }

                }
            }
        }



        public void Add()
        {
            var tagDialog = new TagDialog(null, _archive);

            tagDialog.ShowDialog();
            if (tagDialog.Applied)
            {
                var newTag = tagDialog.NewTag;
                newTag.TagArchiveId = _archive.Id;
                Tags.Add(newTag);
                context.Tags.Add(newTag);
            }

        }

        public void Remove()
        {
            var selected = TagListView.SelectedItems;

            if (selected != null)
            {
                RemoveItems(selected);
            }
        }

        

        private void TagListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var sel = TagListView.SelectedItems;
            if (sel != null && !server.IsStarted)
            {
                RemoveTagButton.IsEnabled = true;
            }
            else
            {
                RemoveTagButton.IsEnabled = false;
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            Add();
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            Remove();
        }



        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (loaded)
            {
                string type = (SelectTagCb.SelectedItem as ComboBoxItem).Name;
                string name = SearchTagTb.Text;
                Filter(name, type);
            }
        }

        private void TagListView_KeyDown(object sender, KeyEventArgs e)
        {
            var selected = TagListView.SelectedItems;
            if(e.Key == Key.Delete && selected!=null && !server.IsStarted)
            {
                RemoveItems(selected);
            }
        }

        private void Cb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (loaded)
            {
                string type = (SelectTagCb.SelectedItem as ComboBoxItem).Name;

                    string name = SearchTagTb.Text;
                    Filter(name, type);
               
            }
        }

        private void RemoveItems(IList items)
        {
            var t = new List<ITag>();
            foreach (var item in items)
            {
                t.Add(item as ITag);
            }

            foreach (var item in t)
            {
                Tags.Remove(item);
                EntityContext.GetContext().Tags.Remove(item);
            }

        }

    }
}
