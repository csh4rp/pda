using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProcessDataArchiver.DataCore.Database.CommandProviders;
using System.Data;
using System.Data.Common;
using ProcessDataArchiver.DataCore.Database.Schema;
using ProcessDataArchiver.DataCore.DbEntities.Tags;
using ProcessDataArchiver.DataCore.DbEntities.Events;
using ProcessDataArchiver.DataCore.DbEntities;
using System.Transactions;
using System.Threading;

namespace ProcessDataArchiver.DataCore.Database.DbProviders
{
    public interface IDatabaseProvider
    {
        string ConnectionString { get; set; }
        ICommandProvider CommandProvider { get; set; }    
        DatabaseType DatabaseType { get; }
        DatabaseType CmdProviderType { get; }

        Task<bool> TryConnectAsync(CancellationToken token);
        Task CallProcedureAsync(string name, IEnumerable<DbParameter> parameters,
            CancellationToken token);
        Task<DataTable> ExecuteQueryAsync(string command, CancellationToken token);
        Task<DataTable> ExecuteQueryAsync(QueryOptions opts, CancellationToken token);
        Task<int> ExecuteNonQueryAsync(string command, CancellationToken token);
        Task<int> FillDataSetAsync(string command, DataTable ds, CancellationToken token);
        Task<int> FillDataSetAsync(QueryOptions options, DataTable ds, 
            CancellationToken token);

        Task<bool> TryConnectAsync();
        Task CallProcedureAsync(string name, IEnumerable<DbParameter> parameters);
        Task<DataTable> ExecuteQueryAsync(string command);
        Task<DataTable> ExecuteQueryAsync(QueryOptions opts);
        Task<int> ExecuteNonQueryAsync(string command);
        Task<int> FillDataSetAsync(string command, DataTable ds);
        Task<int> FillDataSetAsync(QueryOptions options, DataTable ds);

        Task CreateDbSchemaAsync();
        Task<bool> CheckSchemaExists();
        Task CreateGlobalVariablesAsync(params GlobalVariable[] gvars);
        Task DeleteGlobalVariablesAsync(params GlobalVariable[] gvars);
        Task<IEnumerable<GlobalVariable>> GetGlobalVariablesAsync();

        Task CreateArchivesAsync(params TagArchive[] archive);
        Task DeleteArchivesAsync(params TagArchive[] archive);
        Task<IEnumerable<TagArchive>> GetArchivesAsync();

        Task CreateTagsAsync(params ITag[] tag);
        Task DeleteTagsAsync(params ITag[] tag);
        Task<IEnumerable<ITag>> GetTagsAsync();

        Task CreateEventsAsync(params IEvent[] archiveEvent);
        Task DeleteEventsAsync(params IEvent[] ev); 
        Task<IEnumerable<IEvent>> GetEventsAsync();
       
        Task InsertTagsValueAsync(params ITag[] tags);
        Task InsertEventsValueAsync(params IEvent[] archiveEvents);
        Task InsertEventSnapshotAsync(IEvent archiveEvent,params ITag[] tags);
        Task<IEnumerable<KeyValuePair<DateTime,double>>> GetTagValuesAsync(ITag tag, 
            DateTime from, DateTime to);

        Task InsertSummaryAsync(IEvent archiveEvent);
        Task UpdateEntitiesAsync(params EntityChangesInfo[] changes);
        Task<DatabaseSchema> GetDatabaseSchemaAsync();

    }
}


