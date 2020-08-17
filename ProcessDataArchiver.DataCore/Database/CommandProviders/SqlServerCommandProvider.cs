using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProcessDataArchiver.DataCore.Database.Schema;
using System.Data;
using ProcessDataArchiver.DataCore.DbEntities.Tags;
using ProcessDataArchiver.DataCore.DbEntities;
using ProcessDataArchiver.DataCore.DbEntities.Events;

namespace ProcessDataArchiver.DataCore.Database.CommandProviders
{
    public class SqlServerCommandProvider : IExtendedCmdProvider
    {
        public string ChangeDtCmd(ITag tag, Type newType)
        {
            string typeToChange = MapToDbType(newType);
            return $"ALTER TABLE {tag.TagArchive.Name} ALTER COLUMN  {tag.Name} + {typeToChange} ;";
        }

        public string ChangeNameCmd(string table, string oldName, string newName, Type t)
        {
            return $"sp_rename '{table}.{oldName}', '{newName}', 'COLUMN';";
        }

        public IEnumerable<string> CreateArchiveCmd(TagArchive archive)
        {
            string cmd;
            if (archive.ArchiveType == ArchiveType.Normal)
            {
                cmd = "CREATE TABLE " + archive.Name +
                "(Id Int Primary Key Identity(1,1), TagId Int, TagTimestamp DateTime, TagValue Float);";
            }
            else
            {
                cmd = "CREATE TABLE " + archive.Name +
                "(Id Int Primary Key Identity(1,1), TagTimestamp DateTime);";
            }
           yield return cmd;
        }

        public IEnumerable<string> CreateDbProcedures()
        {
            yield return
                "Create Procedure Create_GlobalVariable(" +
                "@Name NVarchar(50)," +
                "@Address BigInt," +
                "@VariableSize Int," +
                "@NetType NVarchar(50)) AS " +
                "Insert Into GlobalVariable(Name,Address,VariableSize,NetType) Values(" +
                "@Name,@Address,@VariableSize,@NetType);";

            yield return
                "Create Procedure Create_Tag(" +
                "@ArchiveId Int," +
                "@GlobalVariableId Int," +
                "@TagType Int," +
                "@Name NVarchar(50)," +
                "@RefreshSpan BigInt," +
                "@ArchivingType Int," +
                "@EuName NVarchar(50),"+
                "@DeadbandType Int,"+
                "@DeadbandValue Float,"+
                "@LastChanged Datetime,"+
                "@Comment NVarchar(Max)) AS " +
                "Insert Into Tag(" +
                "ArchiveId,GlobalVariableId,TagType,Name,RefreshSpan,ArchivingType,"+
                "EuName,DeadbandType,DeadbandValue,LastChanged,Comment)" +
                "Values(" +
                "@ArchiveId,@GlobalVariableId,@TagType,@Name,@RefreshSpan,@ArchivingType,"+
                "@EuName,@DeadbandType,@DeadbandValue,@LastChanged,@Comment); ";


                
            yield return
                "Create Procedure Create_Archive(" +
                "@ArchiveType Int," +
                "@Name NVarchar(50)) As " +
                "Insert Into Archive(ArchiveType,Name) Values" +
                "(@ArchiveType,@Name);";
            yield return
                "Create Procedure Create_Event(" +
                "@EventType Int," +
                "@EventActionType Int," +
                "@Name NVarchar(50)," +
                "@RefreshSpan BigInt," +
                "@ActionText NVarchar(Max)," +
                "@Enabled SmallInt, " +
                "@LastChanged Datetime,"+
                "@Comment NVarchar(Max)) AS " +
                "Insert Into Event(" +
                "EventType, EventActionType,Name, RefreshSpan, ActionText, Enabled, " +
                "LastChanged,Comment) Values" +
                "(@EventType, @EventActionType,@Name, @RefreshSpan, @ActionText, @Enabled, " +
                "@LastChanged,@Comment);";
            yield return
                "Create Procedure Create_AnalogEvent(" +
                "@GlobalVariableId Int," +
                "@EventId Int," +
                "@EventTriggerType Int," +
                "@TriggerValue Float) AS " +
                "Insert Into AnalogEvent(" +
                "GlobalVariableId,EventId,EventTriggerType,TriggerValue) Values" +
                "(@GlobalVariableId,@EventId,@EventTriggerType,@TriggerValue);";
            yield return
                "Create Procedure Create_DiscreteEvent(" +
                "@GlobalVariableId Int," +
                "@EventId Int," +
                "@EdgeType Int) As " +
                "Insert Into DiscreteEvent(" +
                "GlobalVariableId,EventId,EdgeType) Values(" +
                "@GlobalVariableId,@EventId,@EdgeType);";
            yield return
                "Create Procedure Create_CyclicEvent(" +
                "@EventId Int," +
                "@EventCycleType Int," +
                "@Days Int,@Hours Int,@Minutes Int,@Seconds Int," +
                "@NextEvent Datetime) As " +
                "Insert Into CyclicEvent(EventId,EventCycleType," +
                "Days,Hours,Minutes,Seconds,NextEvent)" +
                "Values(@EventId,@EventCycleType," +
                "@Days,@Hours,@Minutes,@Seconds,@NextEvent);";
            yield return
                "Create Procedure Insert_EventHistory(" +
                "@EventId Int) As " +
                "Insert Into EventHistory(EventId,EventTimestamp) " +
                "Values (@EventId,GetDate());";
            yield return
                "Create Procedure Insert_EventSnapshot(" +
                "@EventId Int,@TagId Int,@Value Float) As " +
                "Insert Into EventSnapshot(EventId,TagId,Value,EventTimeStamp) " +
                "Values (@EventId,@TagId,@Value,GetDate())";

            yield return
                "Create Procedure Delete_Eu(@Id Int) As Delete From EngeneeringUnit Where Id=@Id;";
            yield return
                "Create Procedure Delete_Tag(@Id Int) As Delete From Tag Where Id=@Id;";
            yield return
                "Create Procedure Delete_Event(@Id Int) As Delete From Event Where Id=@Id;";

        }

        public IEnumerable<string> CreateDbTables()
        {
            yield return
                "Create Table GlobalVariable(Id Int Primary Key Identity(1,1)," +
                "Name NVarchar(50)," +
                "Address BigInt," +
                "VariableSize Int," +
                "NetType NVarchar(50));";


            yield return
               "Create Table Archive(Id Int Primary Key Identity(1,1)," +
               "ArchiveType Int," +
               "Name NVarchar(50));";
            yield return
                "Create Table Tag(Id Int Primary Key Identity(1,1)," +
                "ArchiveId Int, " +
                "GlobalVariableId Int,"+
                "TagType Int," +
                "Name NVarchar(50)," +
                "RefreshSpan Int," +
                "ArchivingType Int," +
                "EuName NVarchar(50),"+
                "DeadbandType Int,"+
                "DeadbandValue Float,"+
                "LastChanged Datetime," +
                "Comment NVarchar(Max)," +
                "Constraint FK_GV Foreign Key(GlobalVariableId) References GlobalVariable(Id) On Delete Cascade, " +
                "Constraint FK_Archive Foreign Key(ArchiveId) References Archive(Id) On Delete Cascade);";


            yield return
               "Create Table Event(Id Int Primary Key Identity(1,1)," +
               "EventType Int," +
               "EventActionType Int, " +
               "Name NVarchar(50)," +
               "RefreshSpan Int," +
               "ActionText NVarchar(Max)," +
               "Enabled SmallInt," +
               "LastChanged Datetime," +
               "Comment NVarchar(Max));";
            yield return
                "Create Table DiscreteEvent(Id Int Primary Key Identity(1,1)," +
                "GlobalVariableId Int," +
                "EventId Int," +
                "EdgeType Int," +
                "Constraint FK_DE_EventId Foreign Key (EventId) References Event(Id) On Delete Cascade, " +
                "Constraint FK_DE_GvId Foreign Key (GlobalVariableId) References GlobalVariable(Id) On Delete Cascade);";
            yield return
                "Create Table AnalogEvent(Id Int Primary Key Identity(1,1)," +
                "GlobalVariableId Int," +
                "EventId Int," +
                "EventTriggerType Int," +
                "TriggerValue Float," +
                "Constraint FK_AE_EventId Foreign Key (EventId) References Event(Id) On Delete Cascade, " +
                "Constraint FK_AE_GvId Foreign Key (GlobalVariableId) References GlobalVariable(Id) On Delete Cascade);";
            yield return
                "Create Table CyclicEvent(Id Int Primary Key Identity(1,1)," +
                "EventId Int," +
                "EventCycleType Int," +
                "Days Int, Hours Int, Minutes Int, Seconds Int," +
                "NextEvent Datetime, " +
                "Constraint FK_CE_EventId Foreign Key (EventId) References Event(Id) On Delete Cascade);";
            yield return
                "Create Table EventHistory(Id Int Primary Key Identity(1,1)," +
                "EventId Int," +
                "EventTimestamp Datetime," +
                "Constraint FK_EH_EventId Foreign Key (EventId) References Event(Id) On Delete Cascade);";
            yield return
                "Create Table EventSnapshot(Id Int Primary Key Identity(1,1)," +
                 "EventId Int," +
                 "TagId Int," +
                 "Value Float," +
                 "EventTimestamp Datetime," +
                 "Constraint FK_ES_EventId Foreign Key (EventId) References Event(Id) On Delete Cascade," +
                 "Constraint FK_ES_TagId Foreign Key (TagId) References Tag(Id) On Delete Cascade);";
            yield return
                "Create Table EventSummary(Id Int Primary Key Identity(1,1)," +
                "EventId Int," +
                "TagName Varchar(50)," +
                "CalcType Varchar(50)," +
                "FromDate Datetime," +
                "ToDate Datetime," +
                "CalcValue Float);";
        }

        public IEnumerable<string> CreateDbViews()
        {


            // Widoki dla eventow
            yield return
                "Create View AnalogEventInfo As " +
                "Select [E].[Id] As [Id], [E].[Name] As [Name], [E].[RefreshSpan] As [RefreshSpan], " +
                "[E].[Enabled] As [Enabled], [E].[LastChanged] As [LastChanged]," +
                "[E].[Comment] As [Comment], [E].[ActionText] As [ActionText], " +
                "[E].[EventActionType] As [EventActionType]," +
                "[Ae].[GlobalVariableId] As [GlobalVariableId], [Ae].[Id] As [AnalogEventId]," +
                "[Ae].[TriggerValue] As [TriggerValue], [Ae].[EventTriggerType] As [EventTriggerType] " +
                "From ([Event] As [E] " +
                "Inner Join [AnalogEvent] As [Ae] On [E].[Id]=[Ae].[EventId]);";

            yield return
                "Create View DiscreteEventInfo As " +
                "Select [E].[Id] As [Id], [E].[Name] As [Name], [E].[RefreshSpan] As [RefreshSpan], " +
                "[E].[Enabled] As [Enabled], [E].[LastChanged] As [LastChanged]," +
                "[E].[Comment] As [Comment], [E].[ActionText] As [ActionText], " +
                "[E].[EventActionType] As [EventActionType]," +
                "[De].[GlobalVariableId] As [GlobalVariableId], " +
                "[De].[EdgeType] As [EdgeType], [De].[Id] As [DiscreteEventId] " +
                "From ([Event] As [E] " +
                "Inner Join [DiscreteEvent] As [De] On [E].[Id]=[De].[EventId]);";


            yield return
                "Create View CyclicEventInfo As " +
                "Select [E].[Id] As [Id], [E].[Name] As [Name], [E].[RefreshSpan] As [RefreshSpan], " +
                "[E].[Enabled] As [Enabled], [E].[LastChanged] As [LastChanged]," +
                "[E].[Comment] As [Comment], [E].[ActionText] As [ActionText], " +
                "[E].[EventActionType] As [EventActionType]," +
                "[C].[Days] As [Days]," +
                "[C].[Hours] As [Hours], [C].[Minutes] As [Minutes]," +
                "[C].[Seconds] As [Seconds], [C].[EventCycleType] As [EventCycleType], " +
                "[C].[Id] As [CyclicEventId], [C].[NextEvent] As [NextEvent] " +
                "From ([Event] As [E] " +
                "Inner Join [CyclicEvent] As [C] On [E].[Id]=[C].[EventId]);";

        }

        public string CreateQuery(QueryOptions options)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT ");
            if (options.Aggregation == null || options.Aggregation.Count == 0)
            {
                var columnNames = options.ColumnNames.ToList();
                
                
                for (int i = 0; i < columnNames.Count(); i++)
                {
                    if (i < columnNames.Count() - 1)
                        sb.Append(columnNames[i] + ",");
                    else
                        sb.Append(columnNames[i] + " FROM " + options.TableName + " ");
                }
            }
            else
            {
                var agg = options.Aggregation;
                
                foreach (var item in agg)
                {
                    if (!item.Equals(agg.Last()))
                        sb.Append(item.Value + "(" + item.Key + "),");
                    else
                        sb.Append(item.Value + "(" + item.Key + ") FROM " + options.TableName + " ");
                }
            }

            if(options.Conditions!=null && options.Conditions.Count() > 0)
            {
                /////////////////////
                sb.Append("WHERE ");
                foreach (var cond in options.Conditions)
                {

                    var con = cond;
                    con.AndCondition = cond.AndCondition.Replace(" AND ", " AND " +con.ColumnName);
                    con.AndCondition = cond.AndCondition.Replace(" OR ", " OR " + con.ColumnName);

                    con.OrCondition = cond.OrCondition.Replace(" AND ", " AND " + con.ColumnName);
                    con.OrCondition = cond.OrCondition.Replace(" OR ", " OR " + con.ColumnName);
                    //    con.OrCondition = cond.OrCondition.Replace()

                    var first = options.Conditions.FirstOrDefault();
                    if (!first.Equals(con))
                    {
                        if (string.IsNullOrEmpty(con.AndCondition) && !string.IsNullOrEmpty(con.OrCondition))
                            sb.Append("OR (" + con.ColumnName + " " + con.OrCondition + ") ");
                        else if (!string.IsNullOrEmpty(con.AndCondition) && !string.IsNullOrEmpty(con.OrCondition))
                            sb.Append("AND ((" + con.ColumnName + " " + con.AndCondition + ") OR (" +
                            con.ColumnName + " " + con.OrCondition + ")) ");
                        else if (!string.IsNullOrEmpty(con.AndCondition) && string.IsNullOrEmpty(con.OrCondition))
                            sb.Append("AND (" + con.ColumnName + " " + con.AndCondition + ") ");
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(con.AndCondition) && string.IsNullOrEmpty(con.OrCondition))
                            sb.Append(" (" + con.ColumnName + " " + con.AndCondition + ") ");
                        else if (!string.IsNullOrEmpty(con.AndCondition) && !string.IsNullOrEmpty(con.OrCondition))
                            sb.Append(" ((" + con.ColumnName + " " + con.AndCondition + ") OR (" +
                                con.ColumnName + " " + con.OrCondition + ")) ");
                    }
                
                }
            }

            if(options.OrderBy!=null && options.OrderBy.Count > 0)
            {
                sb.Append("ORDER BY ");
                foreach (var ord in options.OrderBy)
                {

                    if (ord.Value.Equals("Asc"))
                        sb.Append(ord.Key + " Asc");
                    else
                        sb.Append(ord.Key + " Desc");

                    if (!options.OrderBy.Last().Equals(ord))
                        sb.Append(",");
                }
            }
            

            return sb.ToString()+";";
        }

        public string GetTagValues(ITag tag, DateTime from, DateTime to)
        {
            string query;
            if (tag.TagArchive.ArchiveType == DbEntities.ArchiveType.Normal)
            {
                query = "SELECT TagTimestamp,TagValue FROM " + tag.TagArchive.Name + " " +
                    $"WHERE TagTimestamp>= Convert(datetime,'{from.Year}-{from.Month}-{from.Day} {from.ToShortTimeString()}:{from.Second}') "+
                    $"AND TagTimestamp<= Convert(datetime,'{to.Year}-{to.Month}-{to.Day} {to.ToShortTimeString()}:{to.Second}') "+
                    $"AND TagName='{tag.Name}' ;";

            }
            else
            {
                query = $"SELECT TagTimestamp,{tag.Name} FROM {tag.TagArchive.Name} " +
                        $"WHERE TagTimestamp>= Convert(datetime,'{from.Year}-{from.Month}-{from.Day} {from.ToShortTimeString()}:{from.Second}') " +
                        $"AND TagTimestamp<= Convert(datetime,'{to.Year}-{to.Month}-{to.Day} {to.ToShortTimeString()}:{to.Second}') "+
                        $"AND {tag.Name} IS NOT NULL;";
            }
            return query;
        }

        public string GetUpdateCmd(IDictionary<string, object> values, Type type)
        {
            var propNames = type.GetProperties().Select(p => p.Name);
            var keys = values.Keys.Where(k => propNames.Contains(k)).ToList();

            if (keys == null || keys.Count == 0)
                return null;

            string cmd = "SET ";
            for (int i = 0; i < keys.Count; i++)
            {
                if (values[keys[i]].GetType().Equals(typeof(string)))
                    cmd += keys[i] + "=" + "'" + values[keys[i]] + "'";
                else if (values[keys[i]].GetType().Equals(typeof(DateTime)))
                {
                    var date = (DateTime)values[keys[i]];
                    cmd += keys[i] + "=" + $"'{date.Year}-{date.Month}-{date.Day} {date.Hour}:{date.Minute}:{date.Second}'";
                }
                else if (values[keys[i]].GetType().Equals(typeof(TimeSpan)))
                {
                    int span = (int)((TimeSpan)values[keys[i]]).TotalMilliseconds;
                    cmd += keys[i] + "=" + span;
                }
                else if (values[keys[i]] is Enum)
                {
                    int val = (int)values[keys[i]];
                    cmd += keys[i] + "=" + val;
                }
                else if (values[keys[i]].GetType().Equals(typeof(bool)))
                {
                    int val = (bool)values[keys[i]] == true ? 1 : 0;
                    cmd += keys[i] + "=" + val;
                }
                else
                    cmd += keys[i] + "=" + values[keys[i]];
                if (i < (keys.Count - 1))
                    cmd += ", ";
            }
            return cmd;
        }

        public string GetUpdateCmd(IDictionary<string, object> values, Type baseType, Type derrivedType)
        {
            var basePropNames = baseType.GetProperties().Select(p => p.Name);
            var derPropNames = derrivedType.GetProperties()
                .Select(p => p.Name).Where(n => !basePropNames.Contains(n));
            var keys = values.Keys.Where(k => derPropNames.Contains(k)).ToList();

            if (keys == null || keys.Count == 0)
                return null;

            string cmd = "SET ";
            for (int i = 0; i < keys.Count; i++)
            {
                if (values[keys[i]].GetType().Equals(typeof(string)))
                    cmd += keys[i] + "=" + "'" + values[keys[i]] + "'";
                else if (values[keys[i]].GetType().Equals(typeof(DateTime)))
                {
                    var date = (DateTime)values[keys[i]];
                    cmd += keys[i] + "=" + $"'{date.Year}-{date.Month}-{date.Day} {date.Hour}:{date.Minute}:{date.Second}'";
                }
                else if (values[keys[i]].GetType().Equals(typeof(TimeSpan)))
                {
                    int span = (int)((TimeSpan)values[keys[i]]).TotalMilliseconds;
                    cmd += keys[i] + "=" + span;
                }
                else if (values[keys[i]] is Enum)
                {
                    int val = (int)values[keys[i]];
                    cmd += keys[i] + "=" + val;
                }
                else if (values[keys[i]].GetType().Equals(typeof(bool)))
                {
                    int val = (bool)values[keys[i]] == true ? 1 : 0;
                    cmd += keys[i] + "=" + val;
                }
                else
                    cmd += keys[i] + "=" + values[keys[i]];
                if (i < (keys.Count - 1))
                    cmd += ", ";
            }
            return cmd;
        }

        public string InsertSummaryCmd(IEvent archiveEvent, ITag tag, string action, DateTime fd)
        {
            if (tag.TagArchive.ArchiveType == ArchiveType.Wide)
            {
                return "INSERT INTO EventSummary(EventId,TagName,CalcType,FromDate,ToDate,CalcValue) " +
                    $"Values({archiveEvent.Id},'{tag.Name}','{action}'," +
                    $"Convert(Datetime,'{fd.Year}-{fd.Month}-{fd.Day} {fd.Hour}:{fd.Minute}:{fd.Second}')," +
                    "GetDate()," +
                    $"(SELECT {action}({tag.Name}) FROM {tag.TagArchive.Name} " +
                    $"WHERE TagTimestamp>Convert(Datetime,'{fd.Year}-{fd.Month}-{fd.Day} {fd.Hour}:{fd.Minute}:{fd.Second}')));";

            }
            else
            {
                return "INSERT INTO EventSummary(EventId,TagName,CalcType,FromDate,ToDate,CalcValue) " +
                $"Values({archiveEvent.Id},'{tag.Name}','{action}'," +
                $"Convert(Datetime,'{fd.Year}-{fd.Month}-{fd.Day} {fd.Hour}:{fd.Minute}:{fd.Second}')," +
                "GetDate()," +
                $"(SELECT {action}(TagValue) FROM {tag.TagArchive.Name} " +
                $"WHERE TagTimestamp>Convert(Datetime,'{fd.Year}-{fd.Month}-{fd.Day} {fd.Hour}:{fd.Minute}:{fd.Second}'))" +
                $" AND TagName='{tag.Name}');";

            }
        }

        public string InsertTagValueCmd(TagArchive archive, IEnumerable<ITag> tags)
        {
            if (archive.ArchiveType == ArchiveType.Wide)
            {
                string cmd = "Insert Into " + archive.Name + " (TagTimestamp,";
                string cmdEnd = " Values(GetDate(),";
                var aTags = archive.Tags.ToList();
               // var cmdPar = new List<SqlParameter>();


                for (int i = 0; i < aTags.Count; i++)
                {
                    if (i < (aTags.Count - 1))
                    {
                        cmd += aTags[i].Name + ",";
                        cmdEnd += "@" + aTags[i].Name + ",";
                       // var tagsToSave = item.ToList();
                    }
                    else
                    {
                        cmd += aTags[i].Name + ") ";
                        cmdEnd += "@" + aTags[i].Name + ");";
                    }

                }
                return cmd + cmdEnd;
            }
            else
            {
                return "Insert Into " + archive.Name + " (TagId,TagTimeStamp,TagValue)" +
                    " Values(@TagId,GetDate(),@TagValue);";

            }
        }

        public string MapToDbType(Type type)
        {
            if (type == typeof(bool))
                return "BIT";
            else if (type == typeof(byte))
                return "TINYINT";
            else if (type == typeof(sbyte))
                return "SMALLINT";
            else if (type == typeof(short))
                return "SMALLINT";
            else if (type == typeof(ushort) || type == typeof(int))
                return "INT";
            else if (type == typeof(uint) || type == typeof(long))
                return "BIGINT";
            else if (type == typeof(ulong))
                return "DECIMAL(20,0)";
            else if (type == typeof(float))
                return "REAL";
            else if (type == typeof(double))
                return "FLOAT";
            else if (type == typeof(string))
                return "NVARCHAR(50)";
            else if (type == typeof(DateTime))
                return "DATETIME";
            else
                throw new ArgumentException("Unsupported data type");
        }
    }
}
