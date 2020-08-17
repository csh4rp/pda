using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessDataArchiver.DataCore.DbEntities
{
    public class EntityChangeTracker
    {
        public List<EntityChangesInfo> Changes { get; }
        public EntityChangeTracker()
        {
            Changes = new List<EntityChangesInfo>();
        }
    }
}

