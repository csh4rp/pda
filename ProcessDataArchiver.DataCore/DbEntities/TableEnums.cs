using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessDataArchiver.DataCore.DbEntities
{
    public enum EdgeType
    {
        Falling =2,
        Raising = 4,
        Both = 8,
        None = 16
    }

    public enum TagType
    {
        Analog = 2,
        Discrete = 4
    }

    public enum ArchiveType
    {
        Normal = 2,
        Wide = 4
    }

    public enum EventType
    {
        Analog = 2,
        Discrete = 4,
        Cyclic = 6
    }

    public enum ArchivingType
    {
        Disabled = 2,
        Cyclic = 4,
        Acyclic = 8
    }

    public enum DeadbandType
    {
        None = 2,
        Absolute = 4,
        Percentage = 6
    }

    public enum EventCycleType
    {
        Hourly = 2,
        Daily = 4,
        Weekly = 8,
        Monthly = 16,
        Periodic = 32
    }

    public enum EventTriggerType
    {
        Equals = 2,
        NotEquals = 4,
        LessThan = 8,
        MoreThan = 16,
        MoreOrEqual = 32,
        LessOrEqual = 64
    }

    public enum EventActionType
    {
        None = 2,
        Snapshot = 4,
        Email = 8,
        Sql = 16,
        Summary = 32
    }
    

}
