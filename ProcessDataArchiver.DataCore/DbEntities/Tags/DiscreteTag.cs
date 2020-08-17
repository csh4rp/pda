
using ProcessDataArchiver.DataCore.DbEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ProcessDataArchiver.DataCore.DbEntities.Tags
{
    public class DiscreteTag : ITag
    {
        private object lastVal = false;

        public int GlobalVariableId { get; set; }
        public GlobalVariable GlobalVariable { get; set; }
        public TagArchive TagArchive { get; set; }
        public int TagArchiveId { get; set; }
        public string Comment { get; set; }
        public object CurrentValue
        {
            get
            {
                return GlobalVariable.CurrentValue;
            }
        }

        public TagType TagType
        {
            get
            {
                return TagType.Discrete;
            }
        }

        public int Id { get; set; }
        public DateTime LastChanged { get; set; }

        public object LastSavedValue
        {
            get
            {
                return lastVal;
            }
        }

        public ArchivingType ArchivingType { get; set; }
        public string Name { get; set; }       
        public TimeSpan RefreshSpan { get; set; }

        public bool CheckCondition()
        {
            bool current = bool.Parse(CurrentValue.ToString());
            bool last = bool.Parse(LastSavedValue.ToString());

            switch (ArchivingType)
            {
                case ArchivingType.Disabled:
                    return false;
                case ArchivingType.Cyclic:
                    return true;
                case ArchivingType.Acyclic:
                    return !LastSavedValue.Equals(CurrentValue);
                default:
                    return false;
            }
 
        }


        public object Clone()
        {
            return new DiscreteTag()
            {
                TagArchiveId = this.TagArchiveId,
                TagArchive = this.TagArchive,
                Comment = this.Comment,
                GlobalVariableId = this.GlobalVariableId,
                GlobalVariable = this.GlobalVariable,
                Id = this.Id,
                Name = this.Name,
                LastChanged = this.LastChanged,
                ArchivingType = this.ArchivingType,
                RefreshSpan = this.RefreshSpan
            };

        }
        public override bool Equals(object obj)
        {
            Func<DateTime, DateTime, bool> DtComp = (d1, d2) =>
            {
                return d1.Year.Equals(d2.Year) && d1.Month.Equals(d2.Month)
                && d1.Day.Equals(d2.Day) && d1.Hour.Equals(d2.Hour)
                && d1.Minute.Equals(d2.Minute) && d1.Second.Equals(d2.Second);
            };

            var dt = obj as DiscreteTag;

            return dt != null && this.Id == dt.Id
                && this.GlobalVariableId == dt.GlobalVariableId
                && string.Equals(this.Comment, dt.Comment)
                && DtComp(this.LastChanged, dt.LastChanged)
                && this.ArchivingType == dt.ArchivingType
                && string.Equals(this.Name, dt.Name)
                && this.RefreshSpan.Equals(dt.RefreshSpan);

        }
        public override int GetHashCode()
        {
            int hash = 10;
            hash = (hash) + this.Id.GetHashCode();
            hash = (hash) + this.GlobalVariableId.GetHashCode();
            hash = (hash) + this.TagArchiveId.GetHashCode();
            hash = (hash) + this.Comment!=null?0: this.Comment.GetHashCode();
            hash = (hash ) + this.LastChanged.GetHashCode();
            hash = (hash) + this.ArchivingType.GetHashCode();
            hash = (hash) + this.Name == null?0:this.Name.GetHashCode();
            hash = (hash) + this.RefreshSpan.GetHashCode();
            return hash;
        }

        public void Copy(ITag tag)
        {
            var t = tag as DiscreteTag;

            if (t != null)
            {
                this.ArchivingType = t.ArchivingType;
                this.Comment = t.Comment;
                this.GlobalVariable = t.GlobalVariable;
                this.GlobalVariableId = t.GlobalVariableId;
                this.Name = t.Name;
                this.LastChanged = t.LastChanged;
                this.RefreshSpan = t.RefreshSpan;
            }
        }
    }
}
