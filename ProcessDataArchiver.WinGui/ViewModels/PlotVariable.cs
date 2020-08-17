using ProcessDataArchiver.DataCore.DbEntities;
using ProcessDataArchiver.DataCore.DbEntities.Tags;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace ProcessDataArchiver.WinGui.ViewModels
{
    public class PlotVariable
    {
        
        private string archiveName;


        public string ArchiveName
        {
            get { return archiveName; }
            set
            {
                archiveName = value;
                SetTags();
            }
        }
        public ObservableCollection<ITag> Tags { get; set; } =
            new ObservableCollection<ITag>();

        public ITag Tag { get; set; }
        public Color LineColor { get; set; }
        public double LineThickness { get; set; }
        public List<KeyValuePair<DateTime, double>> Values { get; set; }

        private  void SetTags()
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
