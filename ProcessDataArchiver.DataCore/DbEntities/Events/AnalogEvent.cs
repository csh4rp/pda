
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProcessDataArchiver.DataCore.DbEntities.Tags;
using System.Runtime.CompilerServices;
using ProcessDataArchiver.DataCore.DbEntities;

namespace ProcessDataArchiver.DataCore.DbEntities.Events
{
    public class AnalogEvent : IEvent
    {
        private DateTime lastOccurance;

        public string ActionText { get; set; } = "";
        public Action ActionToInvoke { get; set; } 
        public EventActionType EventActionType { get; set; }
        public EventTriggerType EventTriggerType { get; set; }
        public double TriggerValue { get; set; }
        public int GlobalVariableId { get; set; }
        public GlobalVariable GlobalVariable { get; set; }
        public int AnalogEventId { get; set; }

        public string Comment { get; set; } = "";

        public EventType EventType
        {
            get
            {
                return EventType.Analog;
            }
        }

        public int Id { get; set; }

        public bool Enabled { get; set; }

        public DateTime LastChanged { get; set; }

        public string Name { get; set; } = "";

        public TimeSpan RefreshSpan { get; set; }

        public int Delay { get; set; }
        public bool InvokeAction()
        {
            double diff = (DateTime.Now - lastOccurance).TotalMilliseconds;
            if (diff > Delay)
            {
                double val = double.Parse(GlobalVariable.CurrentValue.ToString());

                switch (EventTriggerType)
                {
                    case EventTriggerType.Equals:
                        if (GlobalVariable.CurrentValue.Equals(TriggerValue))
                        {
                            lastOccurance = DateTime.Now;
                            ActionToInvoke?.BeginInvoke(null, null);
                            return true;
                        }
                        break;
                    case EventTriggerType.NotEquals:
                        if (!GlobalVariable.CurrentValue.Equals(TriggerValue))
                        {
                            lastOccurance = DateTime.Now;
                            ActionToInvoke?.BeginInvoke(null, null);
                            return true;
                        }
                        break;
                    case EventTriggerType.LessThan:                       
                        if (val<TriggerValue)
                        {
                            lastOccurance = DateTime.Now;
                            ActionToInvoke?.BeginInvoke(null, null);
                            return true;
                        }
                        break;
                    case EventTriggerType.MoreThan:
                        if (val > TriggerValue)
                        {
                            lastOccurance = DateTime.Now;
                            ActionToInvoke?.BeginInvoke(null, null);
                            return true;
                        }
                        break;
                    case EventTriggerType.MoreOrEqual:
                        if (val >= TriggerValue)
                        {
                            lastOccurance = DateTime.Now;
                            ActionToInvoke?.BeginInvoke(null, null);
                            return true;
                        }
                        break;
                    case EventTriggerType.LessOrEqual:
                        if (val <= TriggerValue)
                        {
                            lastOccurance = DateTime.Now;
                            ActionToInvoke?.BeginInvoke(null, null);
                            return true;
                        }
                        break;

                }

            }
            return false;

            
        }

        public object Clone()
        {
            return new AnalogEvent
            {
                ActionText = this.ActionText,
                AnalogEventId = this.AnalogEventId,
                ActionToInvoke = this.ActionToInvoke,
                EventActionType = this.EventActionType,
                Comment = this.Comment,
                Delay = this.Delay,
                Id = this.Id,
                Enabled = this.Enabled,
                LastChanged = this.LastChanged,
                Name = this.Name,
                RefreshSpan = this.RefreshSpan,
                GlobalVariableId = this.GlobalVariableId,
                EventTriggerType = this.EventTriggerType,
                TriggerValue = this.TriggerValue
            };
        }

        public override bool Equals(object obj)
        {
            Func<DateTime, DateTime, bool> DtComp = (d1, d2) =>
            {
                bool wyn = d1.Year.Equals(d2.Year) && d1.Month.Equals(d2.Month)
                && d1.Day.Equals(d2.Day) && d1.Hour.Equals(d2.Hour)
                && d1.Minute.Equals(d2.Minute) && d1.Second.Equals(d2.Second);
                return wyn;
            };
            var ae = obj as AnalogEvent;

            bool res = ae != null 
                && string.Equals(this.ActionText, ae.ActionText)
                && this.EventActionType == ae.EventActionType
                && this.AnalogEventId == ae.AnalogEventId
                && string.Equals(this.Comment, ae.Comment)
                && this.EventTriggerType == ae.EventTriggerType
                && this.EventType == ae.EventType
                && this.Id == ae.Id
                && this.Delay == ae.Delay
                && this.Enabled == ae.Enabled
                && DtComp(this.LastChanged,ae.LastChanged)
                && string.Equals(this.Name, ae.Name)
                && this.RefreshSpan.Equals(ae.RefreshSpan)
                && this.GlobalVariableId == ae.GlobalVariableId
                && this.TriggerValue == ae.TriggerValue;
            return res;
        }

        public override int GetHashCode()
        {
            int hash = 10;
            hash = (hash) +this.ActionText!=null?0: this.ActionText.GetHashCode();
            hash = (hash) + this.EventActionType.GetHashCode();
            hash = (hash) + this.AnalogEventId.GetHashCode();
            hash = (hash) +this.Comment!=null?0: this.Comment.GetHashCode();
            hash = (hash) + this.EventTriggerType.GetHashCode();
            hash = (hash) + this.EventType.GetHashCode();
            hash = (hash) + this.Id.GetHashCode();
            hash = (hash) + this.Enabled.GetHashCode();
            hash = (hash) + this.LastChanged.GetHashCode();
            hash = (hash) +this.Name!=null?0: this.Name.GetHashCode();
            hash = (hash) + this.RefreshSpan.GetHashCode();
            hash = (hash) + this.GlobalVariableId.GetHashCode();
            hash = (hash) + this.TriggerValue.GetHashCode();
            hash = (hash) + this.Delay.GetHashCode();
            return hash;
        }


        public void Copy(IEvent ev)
        {
            if(ev is AnalogEvent)
            {
                var aev = ev as AnalogEvent;

                this.ActionText = aev.ActionText;
                this.ActionToInvoke = aev.ActionToInvoke;
                this.EventActionType = aev.EventActionType;
                this.Comment = aev.Comment;
                this.Delay = aev.Delay;
                this.LastChanged = aev.LastChanged;
                this.GlobalVariable = aev.GlobalVariable;
                this.GlobalVariableId = aev.GlobalVariableId;
                this.Enabled = aev.Enabled;
                this.RefreshSpan = aev.RefreshSpan;
                this.EventTriggerType = aev.EventTriggerType;
            }
        }

    }
}
