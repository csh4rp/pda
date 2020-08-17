
using ProcessDataArchiver.DataCore.DbEntities;
using ProcessDataArchiver.DataCore.DbEntities.Tags;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ProcessDataArchiver.DataCore.DbEntities.Events
{
    public class DiscreteEvent : IEvent
    {
        private DateTime lastOccurance;
        public int DiscreteEventId { get; set; }
        public string ActionText { get; set; } = "";
        public Action ActionToInvoke { get; set; }
        public EventActionType EventActionType { get; set; }
        public EdgeType EdgeType { get; set; }
        public string Comment { get; set; } = "";
        public EventType EventType
        {
            get
            {
                return EventType.Discrete;
            }
        }

        public int Id { get; set; }
        public bool Enabled { get; set; }
        public DateTime LastChanged { get; set; }
        public string Name { get; set; } = "";
        public TimeSpan RefreshSpan { get; set; }
        public int GlobalVariableId { get; set; }
        public GlobalVariable GlobalVariable { get; set; }
        public int Delay { get; set; }

        public bool InvokeAction()
        {
            double diff = (DateTime.Now - lastOccurance).TotalMilliseconds;
            if (diff > Delay)
            {
                lastOccurance = DateTime.Now;
                ActionToInvoke?.BeginInvoke(null,null);
                return true;
            }
            return false;
        }

        public object Clone()
        {
            return new DiscreteEvent
            {
                Id = this.Id,
                ActionText = this.ActionText,
                ActionToInvoke = this.ActionToInvoke,
                EventActionType = this.EventActionType,
                Comment = this.Comment,
                EdgeType = this.EdgeType,
                Enabled = this.Enabled,
                LastChanged = this.LastChanged,
                Name = this.Name,
                Delay = this.Delay,
                RefreshSpan = this.RefreshSpan,
                DiscreteEventId = this.DiscreteEventId,
                GlobalVariableId = this.GlobalVariableId
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
            var de = obj as DiscreteEvent;

            return de != null
                && string.Equals(this.ActionText, de.ActionText)
                && this.EventActionType == de.EventActionType
                && this.DiscreteEventId == de.DiscreteEventId
                && string.Equals(this.Comment, de.Comment)
                && this.EdgeType == de.EdgeType
                && this.EventType == de.EventType
                && this.Id == de.Id
                && this.Delay == de.Delay
                && this.Enabled == de.Enabled
                && DtComp(this.LastChanged,de.LastChanged)
                && string.Equals(this.Name, de.Name)
                && this.RefreshSpan.Equals(de.RefreshSpan)
                && this.GlobalVariableId == de.GlobalVariableId;

        }

        public override int GetHashCode()
        {
            int hash = 10;
            hash = (hash) + this.ActionText!=null?0: this.ActionText.GetHashCode();
            hash = (hash) + this.EventActionType.GetHashCode();
            hash = (hash) + this.DiscreteEventId.GetHashCode();
            hash = (hash) + this.Comment != null ?0: this.Comment.GetHashCode();
            hash = (hash) + this.EdgeType.GetHashCode();
            hash = (hash) + this.EventType.GetHashCode();
            hash = (hash) + this.Id.GetHashCode();
            hash = (hash) + this.Enabled.GetHashCode();
            hash = (hash) + this.LastChanged.GetHashCode();
            hash = (hash) + this.Name!=null?0: this.Name.GetHashCode();
            hash = (hash) + this.RefreshSpan.GetHashCode();
            hash = (hash) + this.GlobalVariableId.GetHashCode();
            hash = (hash) + this.Delay.GetHashCode();

            return hash;
        }

        public void Copy(IEvent ev)
        {
            var dev = ev as DiscreteEvent;
            if (dev != null)
            {
                this.ActionText = dev.ActionText;
                this.EventActionType = dev.EventActionType;
                this.Comment = dev.Comment;
                this.Enabled = dev.Enabled;
                this.LastChanged = DateTime.Now;
                this.Name = dev.Name;
                this.RefreshSpan = dev.RefreshSpan;
                this.EdgeType = dev.EdgeType;
                this.GlobalVariable = dev.GlobalVariable;
                this.GlobalVariableId = dev.GlobalVariableId;
                this.Delay = dev.Delay;

            }
        }
    }
}
