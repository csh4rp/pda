using ProcessDataArchiver.DataCore.DbEntities;
using ProcessDataArchiver.DataCore.DbEntities.Tags;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessDataArchiver.WinGui.ViewModels
{
    public class TagVariable:INotifyPropertyChanged
    {
        private string archiveName;

        public event PropertyChangedEventHandler PropertyChanged;

        //   private 
        private ITag tag;

        public string ArchiveName
        {
            get { return archiveName; }
            set
            {
                archiveName = value;
                SetTags();
            }
        }
        public ObservableCollection<ITag> Tags { get; set; } 
            = new ObservableCollection<ITag>();

        public ITag Tag
        {
            get { return tag; }
            set
            {
                tag = value;
                PropertyChanged(this, new PropertyChangedEventArgs("CurrentValue"));
                PropertyChanged(this, new PropertyChangedEventArgs("NetType"));
            }
        }

        public object CurrentValue
        {
            get { return Tag == null ? 0 : Tag.CurrentValue; }
        }
        public Type NetType
        {
            get { return Tag == null ? typeof(System.Object) : Tag.GlobalVariable.NetType; }
        }

        public TagVariable()
        {
            ArchiveName = EntityContext.GetContext().TagArchives.First().Name;
        }

        public void Refresh()
        {
            PropertyChanged(this, new PropertyChangedEventArgs("CurrentValue"));
        }

        private void SetTags()
        {
            Tags.Clear();
            var tags = EntityContext.GetContext().Tags.Where(t => t.TagArchive.Name.Equals(archiveName));
            foreach (var t in tags)
            {
                Tags.Add(t);
            }
        }
    }
}
