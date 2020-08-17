
using ProcessDataArchiver.DataCore.DbEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ProcessDataArchiver.DataCore.DbEntities.Tags
{
    public class AnalogTag:ITag
    {
        private object lastSavedValue = 0, currentValue = 0;

        public int Id { get; set; }
        public string Name { get;  set; }
        public int TagArchiveId { get; set; }
        public TagArchive TagArchive { get; set; }
        public int GlobalVariableId { get; set; }
        public GlobalVariable GlobalVariable { get; set; }
        public object LastSavedValue
        {
            get
            {
                return lastSavedValue;
            }
        }

        public object CurrentValue
        {
            get
            {
                return GlobalVariable.CurrentValue;
            }
        }

        public TimeSpan RefreshSpan { get; set; }
        public TagType TagType
        {
            get
            {
                return TagType.Analog;
            }
        }

        public ArchivingType ArchivingType { get; set; }
        public string Comment { get; set; } = "";
        public DateTime LastChanged { get; set; }
        public string EuName { get; set; } = "";
        public double DeadbandValue { get; set; } = 0.0;
        public DeadbandType DeadbandType { get; set; }

        public AnalogTag()
        {

        }


        public bool CheckCondition()
        {
            double last = double.Parse(lastSavedValue.ToString());
            double current = double.Parse(CurrentValue.ToString()); ;

            switch (DeadbandType)
            {
                case DeadbandType.None:
                        lastSavedValue = CurrentValue;
                        return true;
                case DeadbandType.Absolute:

                    double upTrig = last + DeadbandValue;
                    double dnTrig = last - DeadbandValue;

                    if(current>=upTrig || current<=dnTrig)
                    {
                        lastSavedValue = CurrentValue;
                        return true;
                    }
                    break;

                case DeadbandType.Percentage:

                    double proc = last * DeadbandValue / 100.0;
                    double upProTrig = last + proc;
                    double dnProTrig = last - proc;

                    if(current>=upProTrig || current<=dnProTrig)
                    {
                        lastSavedValue = CurrentValue;
                        return true;
                    }
                    break;
            }


            return false;
        }

        public object Clone()
        {
            return new AnalogTag()
            {
                TagArchiveId = this.TagArchiveId,
                TagArchive = this.TagArchive,
                Id = this.Id,
                GlobalVariableId = this.GlobalVariableId,
                GlobalVariable = this.GlobalVariable,            
                DeadbandType = this.DeadbandType,            
                Comment = this.Comment,
                EuName = this.EuName,
                LastChanged = this.LastChanged,
                ArchivingType = this.ArchivingType,
                DeadbandValue = this.DeadbandValue,
                Name = this.Name,
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

            

            var at = obj as AnalogTag;

            bool res = at != null && this.Id == at.Id 
                && this.TagArchiveId == at.TagArchiveId 
                && string.Equals(this.Comment,at.Comment)
                && this.DeadbandType == at.DeadbandType 
                && this.DeadbandValue == (at.DeadbandValue)
                && string.Equals(this.EuName,at.EuName)
                && this.GlobalVariableId == at.GlobalVariableId
                && DtComp(this.LastChanged,at.LastChanged)
                && this.ArchivingType == at.ArchivingType 
                && string.Equals(this.Name,at.Name)
                && this.RefreshSpan == (at.RefreshSpan)
                && TagType.Equals(at.TagType);

            return res;
        }

        public override int GetHashCode()
        {
            int hash = 10;
            hash = (hash) + this.Id.GetHashCode();
            hash = (hash) + this.TagArchiveId.GetHashCode();
            hash = (hash) + this.Comment!=null? 0: this.Comment.GetHashCode();
            hash = (hash) + this.DeadbandType.GetHashCode();
            hash = (hash) + this.DeadbandValue.GetHashCode();
            hash = (hash) + this.EuName != null?0:this.EuName.GetHashCode();
            hash = (hash) + this.LastChanged.GetHashCode();
            hash = (hash) + this.ArchivingType.GetHashCode();
            hash = (hash) + this.Name!=null?0: this.Name.GetHashCode();
            hash = (hash) + this.RefreshSpan.GetHashCode();
            hash = (hash) + this.TagType.GetHashCode();
            return hash;
        }

        public void Copy(ITag tag)
        {
            var t = tag as AnalogTag;
            if (t != null)
            {
                this.ArchivingType = t.ArchivingType;
                this.GlobalVariable = t.GlobalVariable;
                this.GlobalVariableId = t.GlobalVariableId;
                this.Comment = t.Comment;
                this.DeadbandType = t.DeadbandType;
                this.DeadbandValue = t.DeadbandValue;
                this.EuName = t.EuName;
                this.Name = t.Name;
                this.RefreshSpan = t.RefreshSpan;
                this.LastChanged = t.LastChanged;
            }
        }

    }
}
