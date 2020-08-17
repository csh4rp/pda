using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessDataArchiver.DataCore.Database.Schema
{
    public class DatabaseSchema
    {

        public string DatabaseName { get; private set; }

        public IEnumerable<TableSchema> Tables { get; set; }

    }
}
