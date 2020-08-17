
using ProcessDataArchiver.DataCore.Database.Schema;
using ProcessDataArchiver.DataCore.DbEntities;
using ProcessDataArchiver.DataCore.DbEntities.Events;
using ProcessDataArchiver.DataCore.DbEntities.Tags;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessDataArchiver.DataCore.Database.CommandProviders
{
    public interface ICommandProvider
    {
        string MapToDbType(Type type);
        IEnumerable<string> CreateDbTables();
        IEnumerable<string> CreateDbProcedures();
        IEnumerable<string> CreateDbViews();
        string GetTagValues(ITag tag, DateTime from, DateTime to);
        string CreateQuery(QueryOptions options);
      
    }
}

