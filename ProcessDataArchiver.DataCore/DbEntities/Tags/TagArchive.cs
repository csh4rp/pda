using ProcessDataArchiver.DataCore.Database.DbProviders;
using ProcessDataArchiver.DataCore.Database.Schema;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using ProcessDataArchiver.DataCore.DbEntities;

namespace ProcessDataArchiver.DataCore.DbEntities.Tags
{
    public class TagArchive:ICloneable
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public IEnumerable<ITag> Tags { get; set; }
        public ArchiveType ArchiveType { get; set; }
        public TagArchive()
        {
            Tags = new ObservableCollection<ITag>();
            
        }
        public object Clone()
        {
            return new TagArchive
            {
                Id = this.Id,
                Name = this.Name,
                Tags = this.Tags,
                ArchiveType = this.ArchiveType
            };
        }
        public override bool Equals(object obj)
        {
            var ta = obj as TagArchive;

            return ta != null && this.Id == ta.Id
                && string.Equals(this.Name, ta.Name)
                && this.ArchiveType == ta.ArchiveType;
        }
        public override int GetHashCode()
        {
            int hash = 10;
            hash = (hash) + this.Id.GetHashCode();
            hash = (hash) + this.Name.GetHashCode();
            hash = (hash) + this.ArchiveType.GetHashCode();
            return hash;
        }
    }
}
