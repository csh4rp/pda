using ProcessDataArchiver.DataCore.Database.DbProviders;
using ProcessDataArchiver.DataCore.DbEntities;
using ProcessDataArchiver.DataCore.DbEntities.Tags;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessDataArchiver.DataCore.DbEntities.Events
{
    public interface IEvent:ICloneable
    {
        int Id { get; set; }
        string Name { get; set; }
        EventType EventType { get;}
        EventActionType EventActionType { get; set; }
        TimeSpan RefreshSpan { get; set; }
        DateTime LastChanged { get; set; }
        string Comment { get; set; }
        bool Enabled { get; set; }
        Action ActionToInvoke { get; set; }
        string ActionText { get; set; }
        bool InvokeAction();
        void Copy(IEvent ev);
    }








}
