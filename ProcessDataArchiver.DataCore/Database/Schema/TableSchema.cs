using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

namespace ProcessDataArchiver.DataCore.Database.Schema
{
    public class TableSchema
    {
        public string TableName { get; set; }
        public IEnumerable<ColumnSchema> Columns { get; set; }


    }
}
