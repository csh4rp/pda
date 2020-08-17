using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessDataArchiver.DataCore.DbEntities
{
    public class EntityChangesInfo
    {
        public object Entity { get; }
        public IEnumerable<string> PropertyNames
        {
            get
            {
                return Changes.Keys;
            }
        }
        public ReadOnlyDictionary<string,object> Changes { get; }
        public ReadOnlyDictionary<string, object> Original { get; }
        public EntityState State { get;  }

        public EntityChangesInfo(object entity, EntityState state,
            IDictionary<string,object> changes, IDictionary<string,object> original)
        {
            Entity = entity;
            State = state;
            Changes = changes == null ? null : new ReadOnlyDictionary<string,object>(changes);
            Original = original == null ? null : new ReadOnlyDictionary<string, object>(original);
        }
    }
}

