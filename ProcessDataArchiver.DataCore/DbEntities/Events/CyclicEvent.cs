using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ProcessDataArchiver.DataCore.DbEntities.Tags;
using System.Runtime.CompilerServices;
using ProcessDataArchiver.DataCore.DbEntities;

namespace ProcessDataArchiver.DataCore.DbEntities.Events
{
    public class CyclicEvent : IEvent
    {
        public int CyclicEventId { get; set; }
        public string ActionText { get; set; } = "";

        public Action ActionToInvoke { get; set; }

        public EventActionType EventActionType { get; set; }

        public string Comment { get; set; } = "";

        public EventType EventType
        {
            get
            {
                return EventType.Cyclic;
            }
        }

        public TimeSpan CycleStamp { get; set; }
        public EventCycleType EventCycleType{get;set;}

        public DateTime NextEvent { get; set; } = DateTime.Now;

        public int Id { get; set; }

        public bool Enabled { get; set; }

        public DateTime LastChanged { get; set; }

        public string Name { get; set; } = "";

        public TimeSpan RefreshSpan { get; set; }


        public void SetupEvent()
        {
            switch (EventCycleType)
            {
                case EventCycleType.Hourly:

                    int minute = CycleStamp.Minutes;
                    var dt = DateTime.Now;
                    if (dt.Minute >= minute)
                    {
                        int diff = dt.Minute - minute;
                        if(diff!=0)
                            RefreshSpan = new TimeSpan(1, -diff,-dt.Second);
                        else
                            RefreshSpan = new TimeSpan(1, 0, -dt.Second);
                    }
                    else if(DateTime.Now.Minute == CycleStamp.Minutes)
                    {
                        RefreshSpan = new TimeSpan(1, 0, 0);
                    }
                    else
                    {
                        int diff = minute - dt.Minute;
                        RefreshSpan = new TimeSpan(0, diff, -dt.Second);
                    }

                    break;
                case EventCycleType.Daily:

                    if (DateTime.Now.Hour > CycleStamp.Hours)
                    {
                        int hours = DateTime.Now.Hour - CycleStamp.Hours;

                        if (DateTime.Now.Minute > CycleStamp.Minutes)
                        {                           
                            int mins = DateTime.Now.Minute - CycleStamp.Minutes;                         
                            RefreshSpan = new TimeSpan(24-hours, mins, -DateTime.Now.Second);
                        }
                        else
                        {
                            int mins = CycleStamp.Minutes - DateTime.Now.Minute;
                            RefreshSpan = new TimeSpan(24 - hours, -mins, -DateTime.Now.Second);
                        }
                    }
                    else
                    {
                        int hours = CycleStamp.Hours - DateTime.Now.Hour;
                        if (DateTime.Now.Minute > CycleStamp.Minutes)
                        {
                            int mins = DateTime.Now.Minute - CycleStamp.Minutes;
                            RefreshSpan = new TimeSpan(hours, -mins, -DateTime.Now.Second);
                        }
                        else if (DateTime.Now.Minute == CycleStamp.Minutes)
                        {
                            RefreshSpan = new TimeSpan(24, 0, 0);
                         //   RefreshSpan = new TimeSpan(hours, mins, -DateTime.Now.Second);
                        }
                        else
                        {
                            int mins = DateTime.Now.Minute - CycleStamp.Minutes;
                            RefreshSpan = new TimeSpan(hours, mins, -DateTime.Now.Second);
                        }
                    }


                    break;
                case EventCycleType.Weekly:

                    if ((int)DateTime.Now.DayOfWeek > CycleStamp.Days)
                    {
                        int days = (int)DateTime.Now.DayOfWeek - CycleStamp.Days;

                        if (DateTime.Now.Hour > CycleStamp.Hours)
                        {
                            int hours = DateTime.Now.Hour - CycleStamp.Hours;

                            if (DateTime.Now.Minute > CycleStamp.Minutes)
                            {
                                int mins = DateTime.Now.Minute - CycleStamp.Minutes;
                                RefreshSpan = new TimeSpan(7-days, -hours, -mins, -DateTime.Now.Second);
                            }
                            else
                            {
                                int mins = CycleStamp.Minutes - DateTime.Now.Minute;
                                RefreshSpan = new TimeSpan(7-days, - hours, mins, -DateTime.Now.Second);
                            }
                        }
                        else
                        {
                            int hours = CycleStamp.Hours - DateTime.Now.Hour;
                            if (DateTime.Now.Minute > CycleStamp.Minutes)
                            {
                                int mins = DateTime.Now.Minute - CycleStamp.Minutes;
                                RefreshSpan = new TimeSpan(7-days,hours, -mins, -DateTime.Now.Second);
                            }
                            else
                            {
                                int mins = CycleStamp.Minutes - DateTime.Now.Minute;
                                RefreshSpan = new TimeSpan(7-days,hours, mins, -DateTime.Now.Second);
                            }
                        }
                    }
                    else //jest jeszcze dzien
                    {
                        int days =  CycleStamp.Days - (int)DateTime.Now.DayOfWeek;

                        if (DateTime.Now.Hour > CycleStamp.Hours)
                        {
                            int hours = DateTime.Now.Hour - CycleStamp.Hours;

                            if (DateTime.Now.Minute > CycleStamp.Minutes)
                            {
                                int mins = DateTime.Now.Minute - CycleStamp.Minutes;
                                RefreshSpan = new TimeSpan(days, -hours, -mins, -DateTime.Now.Second);
                            }
                            else
                            {
                                int mins = CycleStamp.Minutes - DateTime.Now.Minute;
                                RefreshSpan = new TimeSpan(days, -hours, mins, -DateTime.Now.Second);
                            }
                        }
                        else
                        {
                            int hours = CycleStamp.Hours - DateTime.Now.Hour;
                            if (DateTime.Now.Minute > CycleStamp.Minutes)
                            {
                                int mins = DateTime.Now.Minute - CycleStamp.Minutes;
                                RefreshSpan = new TimeSpan(days, hours, -mins, -DateTime.Now.Second);
                            }
                            else
                            {
                                int mins = CycleStamp.Minutes - DateTime.Now.Minute;
                                RefreshSpan = new TimeSpan(days, hours, mins, -DateTime.Now.Second);
                            }
                        }
                    }
                    

                    break;
                case EventCycleType.Monthly:

                    if ((int)DateTime.Now.Day > CycleStamp.Days)
                    {
                        int days = DateTime.Now.Day - CycleStamp.Days;
                        var next = DateTime.Now.AddDays(-days).AddMonths(1);
                        var daysToAdd = (int)(next - DateTime.Now).TotalDays;

                        if (DateTime.Now.Hour > CycleStamp.Hours)
                        {
                            int hours = DateTime.Now.Hour - CycleStamp.Hours;

                            if (DateTime.Now.Minute > CycleStamp.Minutes)
                            {
                                int mins = DateTime.Now.Minute - CycleStamp.Minutes;
                                RefreshSpan = new TimeSpan(daysToAdd - days, -hours, -mins, -DateTime.Now.Second);
                            }
                            else
                            {
                                int mins = CycleStamp.Minutes - DateTime.Now.Minute;
                                RefreshSpan = new TimeSpan(daysToAdd - days, -hours, mins, -DateTime.Now.Second);
                            }
                        }
                        else
                        {
                            int hours = CycleStamp.Hours - DateTime.Now.Hour;
                            if (DateTime.Now.Minute > CycleStamp.Minutes)
                            {
                                int mins = DateTime.Now.Minute - CycleStamp.Minutes;
                                RefreshSpan = new TimeSpan(daysToAdd - days, hours, -mins, -DateTime.Now.Second);
                            }
                            else
                            {
                                int mins = CycleStamp.Minutes - DateTime.Now.Minute;
                                RefreshSpan = new TimeSpan(daysToAdd - days, hours, mins, -DateTime.Now.Second);
                            }
                        }
                    }
                    else //jest jeszcze dzien
                    {
                        int days = CycleStamp.Days - DateTime.Now.Day;

                        if (DateTime.Now.Hour > CycleStamp.Hours)
                        {
                            int hours = DateTime.Now.Hour - CycleStamp.Hours;

                            if (DateTime.Now.Minute > CycleStamp.Minutes)
                            {
                                int mins = DateTime.Now.Minute - CycleStamp.Minutes;
                                RefreshSpan = new TimeSpan(days, -hours, -mins, -DateTime.Now.Second);
                            }
                            else
                            {
                                int mins = CycleStamp.Minutes - DateTime.Now.Minute;
                                RefreshSpan = new TimeSpan(days, -hours, mins, -DateTime.Now.Second);
                            }
                        }
                        else
                        {
                            int hours = CycleStamp.Hours - DateTime.Now.Hour;
                            if (DateTime.Now.Minute > CycleStamp.Minutes)
                            {
                                int mins = DateTime.Now.Minute - CycleStamp.Minutes;
                                RefreshSpan = new TimeSpan(days, hours, -mins, -DateTime.Now.Second);
                            }
                            else
                            {
                                int mins = CycleStamp.Minutes - DateTime.Now.Minute;
                                RefreshSpan = new TimeSpan(days, hours, mins, -DateTime.Now.Second);
                            }
                        }
                    }

                    break;
                case EventCycleType.Periodic:
                    RefreshSpan = new TimeSpan(CycleStamp.Days, CycleStamp.Hours, CycleStamp.Minutes, 0);
                    break;
                default:
                    break;
            }


            NextEvent = DateTime.Now + RefreshSpan;
        }

        public bool InvokeAction()
        {
            ActionToInvoke?.BeginInvoke(null,null);
            SetupEvent();
            NextEvent = DateTime.Now + RefreshSpan;
            return true;
        }

        public object Clone()
        {
            return new CyclicEvent
            {
                ActionText = this.ActionText,
                ActionToInvoke = this.ActionToInvoke,
                EventActionType = this.EventActionType,
                Comment = this.Comment,
                Id = this.Id,
                Enabled = this.Enabled,
                LastChanged = this.LastChanged,
                Name = this.Name,
                RefreshSpan = this.RefreshSpan,
                CycleStamp = this.CycleStamp,
                EventCycleType = this.EventCycleType,
                CyclicEventId = this.CyclicEventId,
                NextEvent = this.NextEvent
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
            var ce = obj as CyclicEvent;

            return ce != null
                && string.Equals(this.ActionText, ce.ActionText)
                && this.EventActionType == ce.EventActionType
                && string.Equals(this.Comment, ce.Comment)
                && this.EventType == ce.EventType
                && this.Id == ce.Id
                && this.Enabled == ce.Enabled
                && DtComp(this.LastChanged,ce.LastChanged)
                && string.Equals(this.Name, ce.Name)
                && this.CycleStamp.Equals(ce.CycleStamp)
                && this.EventCycleType == ce.EventCycleType
                && this.CyclicEventId == ce.CyclicEventId
                && this.NextEvent.Equals(ce.NextEvent)
                && this.RefreshSpan.Equals(ce.RefreshSpan);

        }

        public override int GetHashCode()
        {
            int hash = 10;
            hash = (hash) +this.ActionText==null?0: this.ActionText.GetHashCode();
            hash = (hash) + this.EventActionType.GetHashCode();
            hash = (hash) + this.Comment==null?0: this.Comment.GetHashCode();
            hash = (hash) + this.EventType.GetHashCode();
            hash = (hash) + this.Id.GetHashCode();
            hash = (hash) + this.Enabled.GetHashCode();
            hash = (hash) + this.LastChanged.GetHashCode();
            hash = (hash) + this.Name==null?0: this.Name.GetHashCode();
            hash = (hash) + this.RefreshSpan.GetHashCode();
            hash = (hash) + this.CycleStamp.GetHashCode();
            hash = (hash) + this.EventCycleType.GetHashCode();
            hash = (hash) + this.CyclicEventId.GetHashCode();
            hash = (hash) + this.NextEvent.GetHashCode();
            return hash;
        }

        public void Copy(IEvent ev)
        {
            var cev = ev as CyclicEvent;
            if (cev != null)
            {
                this.ActionText = cev.ActionText;
                this.ActionToInvoke = cev.ActionToInvoke;
                this.EventActionType = cev.EventActionType;
                this.Comment = cev.Comment;
                this.Enabled = cev.Enabled;
                this.LastChanged = DateTime.Now;
                this.Name = cev.Name;
                this.RefreshSpan = cev.RefreshSpan;
                this.CycleStamp = cev.CycleStamp;
                this.EventCycleType = cev.EventCycleType;
                this.LastChanged = cev.LastChanged;
            }
        }

    }
}
