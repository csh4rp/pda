using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessDataArchiver.DataCore.Database.CommandProviders
{
    [Serializable()]
    public class QueryOptions
    {
        public string TableName { get; set; }
        
        public List<string> ColumnNames { get; set; }
        public Dictionary<string,string> Aggregation { get; set; }
        public List<SqlCondition> Conditions { get; set; }
        public Dictionary<string,string> OrderBy { get; set; }
    }
}
