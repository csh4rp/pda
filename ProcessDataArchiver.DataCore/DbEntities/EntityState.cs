using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessDataArchiver.DataCore.DbEntities
{
    public enum EntityState
    {
        Unchanged = 0,
        Modified = 1,
        Added = 2,
        Removed = 3
    }
}
