using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProcessDataArchiver.DataCore.Database.Schema;
using ProcessDataArchiver.DataCore.DbEntities.Tags;
using MySql.Data.MySqlClient;
using ProcessDataArchiver.DataCore.DbEntities;
using ProcessDataArchiver.DataCore.DbEntities.Events;

namespace ProcessDataArchiver.DataCore.Database.CommandProviders
{
    public class MySqlCommandProvider : IExtendedCmdProvider
    {
        public string ChangeDtCmd(ITag tag, Type newType)
        {
            string type = MapToDbType(newType);
            return $"ALTER TABLE {tag.TagArchive.Name} CHANGE {tag.Name} {tag.Name} {type} NULL;";
        }

        public string ChangeNameCmd(string table, string oldName, string newName, Type t)
        {
            string type = MapToDbType(t);
            return $"ALTER TABLE {table} CHANGE {oldName} {newName} {type} NULL;";
        }

        public IEnumerable<string> CreateArchiveCmd(TagArchive archive)
        {
            string cmd;
            if (archive.ArchiveType == ArchiveType.Normal)
            {
                cmd = "CREATE TABLE " + archive.Name +
                "(Id Integer Auto_Increment Primary Key, TagId Int, TagTimestamp Timestamp(6), TagValue Double);";
            }
            else
            {
                cmd = "CREATE TABLE " + archive.Name +
                "(Id Integer Auto_Increment Primary Key, TagTimestamp Timestamp(6));";
            }

           yield return cmd;
        }

        public IEnumerable<string> CreateDbProcedures()
        {

            yield return
                "Create Procedure Create_GlobalVariable(" +
                "in Name Varchar(50)," +
                "in Address BigInt," +
                "in VariableSize Int," +
                "in NetType Varchar(50)) Begin " +
                "Insert Into GlobalVariable(Name,Address,VariableSize,NetType) Values(" +
                "Name,Address,VariableSize,NetType); End;";

            yield return
                "Create Procedure Create_Tag(" +
                "in ArchiveId Int," +
                "in GlobalVariableId Int," +
                "in TagType Int," +
                "in Name Varchar(50)," +
                "in RefreshSpan BIGINT," +
                "in ArchivingType Int," +
                "in EuName Varchar(50),"+
                "in DeadbandType Int,"+
                "in DeadbandValue Double,"+
                "in LastChanged Timestamp,"+
                "in Comment Varchar(500)) Begin " +
                "Insert Into Tag(" +
                "ArchiveId,GlobalVariableId,TagType,Name,RefreshSpan,ArchivingType,"+
                "EuName,DeadbandType,DeadbandValue,LastChanged,Comment)" +
                "Values(" +
                "ArchiveId,GlobalVariableId,TagType,Name,RefreshSpan,ArchivingType," +
                "EuName,DeadbandType,DeadbandValue,LastChanged,Comment); End; ";

            yield return
                "Create Procedure Create_Archive(" +
                "in ArchiveType Int," +
                "in Name Varchar(50)) Begin " +
                "Insert Into Archive(ArchiveType,Name) Values" +
                "(ArchiveType,Name); End;";
            yield return
                "Create Procedure Create_Event(" +
                "in EventType Int," +
                "in EventActionType Int," +
                "in Name Varchar(50)," +
                "in RefreshSpan BIGINT," +
                "in ActionText Varchar(500)," +
                "in Enabled SmallInt, " +
                "in LastChanged Timestamp,"+
                "in Comment Varchar(500)) Begin " +
                "Insert Into Event(" +
                "EventType, EventActionType,Name, RefreshSpan, ActionText, Enabled, " +
                "LastChanged,Comment) Values" +
                "(EventType, EventActionType, Name, RefreshSpan, ActionText, Enabled, " +
                "LastChanged,Comment); End;";
            yield return
                "Create Procedure Create_AnalogEvent(" +
                "in GlobalVariableId Int," +
                "in EventId Int," +
                "in EventTriggerType Int," +
                "TriggerValue Double) Begin " +
                "Insert Into AnalogEvent(" +
                "GlobalVariableId,EventId,EventTriggerType,TriggerValue) Values" +
                "(GlobalVariableId,EventId,EventTriggerType,TriggerValue); End;";
            yield return
                "Create Procedure Create_DiscreteEvent(" +
                "in GlobalVariableId Int," +
                "in EventId Int," +
                "in EdgeType Int) Begin " +
                "Insert Into DiscreteEvent(" +
                "GlobalVariableId,EventId,EdgeType) Values(" +
                "GlobalVariableId,EventId,EdgeType); End;";
            yield return
                "Create Procedure Create_CyclicEvent(" +
                "in EventId Int," +
                "in EventCycleType Int," +
                "in Days Int,in Hours Int,in Minutes Int,in Seconds Int," +
                "NextEvent Timestamp) Begin " +
                "Insert Into CyclicEvent(EventId,EventCycleType," +
                "Days,Hours,Minutes,Seconds,NextEvent)" +
                "Values(EventId,EventCycleType," +
                "Days,Hours,Minutes,Seconds,NextEvent); End;";
            yield return
                "Create Procedure Insert_EventHistory(" +
                "in EventId Integer) Begin " +
                "Insert Into EventHistory(EventId,EventTimestamp) " +
                "Values (EventId,NOW(3)); End;";
            yield return
                "Create Procedure Insert_EventSnapshot(" +
                "in EventId Int,in TagId Int,in Value Double) Begin " +
                "Insert Into EventSnapshot(EventId,GlobalVariableId,VariableValue,EventTimeStamp) " +
                "Values (EventId,TagId,Value,NOW(3)); End;";
        }

        public IEnumerable<string> CreateDbTables()
        {
            yield return
                "Create Table GlobalVariable(Id Int Auto_Increment Primary Key," +
                "Name Varchar(50)," +
                "Address BigInt," +
                "VariableSize Int," +
                "NetType Varchar(50));";

            yield return
               "Create Table Archive(Id  Int Auto_Increment Primary Key," +
               "ArchiveType Int," +
               "Name Varchar(50));";
            yield return
                "Create Table Tag(Id Int Auto_Increment Primary Key," +
                "ArchiveId Int, " +
                "GlobalVariableId Int,"+
                "TagType Int," +
                "Name Varchar(50)," +
                "RefreshSpan Int," +
                "ArchivingType Int," +
                "EuName Varchar(50),"+
                "DeadbandType Int,"+
                "DeadbandValue Double,"+
                "LastChanged Timestamp," +
                "Comment Varchar(500)," +
                "Constraint FK_GV Foreign Key(GlobalVariableId) References GlobalVariable(Id) On Delete Cascade, " +
                "Constraint FK_Archive Foreign Key(ArchiveId) References Archive(Id) On Delete Cascade);";

            yield return
               "Create Table Event(Id Int Auto_Increment Primary Key," +
               "EventType Int," +
               "EventActionType Int, " +
               "Name Varchar(50)," +
               "RefreshSpan Int," +
               "ActionText Varchar(500)," +
               "Enabled SmallInt," +
               "LastChanged Timestamp," +
               "Comment Varchar(500));";
            yield return
                "Create Table DiscreteEvent(Id Int Auto_Increment Primary Key," +
                "GlobalVariableId Int," +
                "EventId Int," +
                "EdgeType Int," +
                "Constraint FK_DE_EventId Foreign Key (EventId) References Event(Id) On Delete Cascade, " +
                "Constraint FK_DE_GvId Foreign Key (GlobalVariableId) References GlobalVariable(Id) On Delete Cascade);";
            yield return
                "Create Table AnalogEvent(Id Int Auto_Increment Primary Key," +
                "GlobalVariableId Int," +
                "EventId Int," +
                "EventTriggerType Int," +
                "TriggerValue Double," +
                "Constraint FK_AE_EventId Foreign Key (EventId) References Event(Id) On Delete Cascade, " +
                "Constraint FK_AE_GvId Foreign Key (GlobalVariableId) References GlobalVariable(Id) On Delete Cascade);";

            yield return
                "Create Table CyclicEvent(Id Int Auto_Increment Primary Key," +
                "EventId Int," +
                "EventCycleType Int," +
                "Days Int, Hours Int, Minutes Int, Seconds Int," +
                "NextEvent Timestamp, " +
                "Constraint FK_CE_EventId Foreign Key (EventId) References Event(Id) On Delete Cascade);";
            yield return
                "Create Table EventHistory(Id Int Auto_Increment Primary Key," +
                "EventId Int," +
                "EventTimestamp Timestamp," +
                "Constraint FK_EH_EventId Foreign Key (EventId) References Event(Id) On Delete Cascade);";
            yield return
                "Create Table EventSnapshot(Id Int Auto_Increment Primary Key," +
                 "EventId Int," +
                 "TagId Int," +
                 "Value Double," +
                 "EventTimestamp Timestamp," +
                 "Constraint FK_ES_EventId Foreign Key (EventId) References Event(Id) On Delete Cascade," +
                 "Constraint FK_ES_GvId Foreign Key (TagId) References Tag(Id) On Delete Cascade);";
            yield return
                "Create Table EventSummary(Id Int Auto_Increment Primary Key," +
                    "EventId Int," +
                    "TagName Varchar(50)," +
                    "CalcType Varchar(50)," +
                    "FromDate Timestamp," +
                    "ToDate Timestamp," +
                    "CalcValue Double);";

        }

        public IEnumerable<string> CreateDbViews()
        {


            yield return
                "Create View AnalogEventInfo As " +
                "Select E.Id Id, E.Name Name, E.RefreshSpan RefreshSpan, " +
                "E.Enabled Enabled, E.LastChanged LastChanged," +
                "E.Comment Comment, E.ActionText ActionText, " +
                "Ae.GlobalVariableId GlobalVariableId, Ae.Id AnalogEventId,"+
                "E.EventActionType As EventActionType," +
                "Ae.TriggerValue TriggerValue, Ae.EventTriggerType EventTriggerType " +
                "From (Event E " +
                "Inner Join AnalogEvent Ae On E.Id=Ae.EventId);";

            yield return
                "Create View DiscreteEventInfo As " +
                "Select E.Id Id, E.Name Name, E.RefreshSpan RefreshSpan, " +
                "E.Enabled Enabled, E.LastChanged LastChanged," +
                "E.Comment Comment, E.ActionText ActionText, " +
                "E.EventActionType As EventActionType," +
                "De.GlobalVariableId GlobalVariableId, " +
                "De.EdgeType EdgeType, De.Id DiscreteEventId " +
                "From (Event E " +
                "Inner Join DiscreteEvent De On E.Id=De.EventId);";

            yield return
                "Create View CyclicEventInfo As " +
                "Select E.Id Id, E.Name Name, E.RefreshSpan RefreshSpan, " +
                "E.Enabled Enabled, E.LastChanged LastChanged," +
                "E.EventActionType As EventActionType," +
                "E.Comment Comment, E.ActionText ActionText, " +
                "C.Days Days," +
                "C.Hours Hours, C.Minutes Minutes," +
                "C.Seconds Seconds, C.EventCycleType EventCycleType, " +
                "C.Id CyclicEventId, C.NextEvent NextEvent " +
                "From (Event As E " +
                "Inner Join CyclicEvent C On E.Id=C.EventId);";

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

            if (options.Conditions != null && options.Conditions.Count() > 0)
            {
                sb.Append("WHERE ");
                foreach (var con in options.Conditions)
                {
                    var first = options.Conditions.FirstOrDefault();
                    if (!first.Equals(con))
                    {
                        con.AndCondition = con.AndCondition.Replace(" AND ", " AND " + con.ColumnName);
                        con.AndCondition = con.AndCondition.Replace(" OR ", " OR " + con.ColumnName);

                        con.OrCondition = con.OrCondition.Replace(" AND ", " AND " + con.ColumnName);
                        con.OrCondition = con.OrCondition.Replace(" OR ", " OR " + con.ColumnName);

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

            if (options.OrderBy != null && options.OrderBy.Count > 0)
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


            return sb.ToString() + ";";
        }

        public string GetTagValues(ITag tag, DateTime from, DateTime to)
        {
            string query;
            if (tag.TagArchive.ArchiveType == DbEntities.ArchiveType.Normal)
            {
                query = "SELECT TagTimestamp,TagValue FROM " + tag.TagArchive.Name + " " +
                    $"WHERE TagTimestamp>= '{from.Year}-{from.Month}-{from.Day} {from.ToShortTimeString()}:{from.Second}' " +
                    $"AND TagTimestamp<= '{to.Year}-{to.Month}-{to.Day} {to.ToShortTimeString()}:{to.Second}' " +
                    $"AND TagName='{tag.Name}' ;";

            }
            else
            {
                query = $"SELECT TagTimestamp,{tag.Name} FROM {tag.TagArchive.Name} " +
                        $"WHERE TagTimestamp>= '{from.Year}-{from.Month}-{from.Day} {from.ToShortTimeString()}:{from.Second}' " +
                        $"AND TagTimestamp<= '{to.Year}-{to.Month}-{to.Day} {to.ToShortTimeString()}:{to.Second}' " +
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
                    $"'{fd.Year}-{fd.Month}-{fd.Day} {fd.Hour}:{fd.Minute}:{fd.Second}',NOW()," +
                    $"(SELECT {action}({tag.Name}) FROM {tag.TagArchive.Name} " +
                    $"WHERE TagTimestamp>'{fd.Year}-{fd.Month}-{fd.Day} {fd.Hour}:{fd.Minute}:{fd.Second}'));";

            }
            else
            {
                return "INSERT INTO EventSummary(EventId,TagName,CalcType,FromDate,ToDate,CalcValue) " +
                $"Values({archiveEvent.Id},'{tag.Name}','{action}'," +
                $"'{fd.Year}-{fd.Month}-{fd.Day} {fd.Hour}:{fd.Minute}:{fd.Second}',NOW()," +
                $"(SELECT {action}(TagValue) FROM {tag.TagArchive.Name} " +
                $"WHERE TagTimestamp>'{fd.Year}-{fd.Month}-{fd.Day} {fd.Hour}:{fd.Minute}:{fd.Second}')" +
                $" AND TagName='{tag.Name}');";

            }
        }

        public string InsertTagValueCmd(TagArchive archive, IEnumerable<ITag> tags)
        {
            if (archive.ArchiveType == ArchiveType.Wide)
            {
                string cmd = "Insert Into " + archive.Name + " (TagTimestamp,";
                string cmdEnd = " Values(NOW(),";
                var aTags = archive.Tags.ToList();
                var cmdPar = new List<MySqlParameter>();


                for (int i = 0; i < aTags.Count; i++)
                {
                    if (i < (aTags.Count - 1))
                    {
                        cmd += aTags[i].Name + ",";
                        cmdEnd += "@" + aTags[i].Name + ",";
                        //var tagsToSave = item.ToList();
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
                    " Values(@TagId,NOW(),@TagValue);";

            }
        }

        public string MapToDbType(Type type)
        {
            if (type == typeof(bool))
                return "BIT";
            else if (type == typeof(byte))
                return "TINYINT UNSIGNED";
            else if (type == typeof(sbyte))
                return "TINYINT";
            else if (type == typeof(short))
                return "SMALLINT";
            else if (type == typeof(ushort))
                return "SMALLINT UNSIGNED";
            else if (type == typeof(int))
                return "INT";
            else if (type == typeof(uint))
                return "INT UNSIGNED";
            else if (type == typeof(long))
                return "BIGINT";
            else if (type == typeof(ulong))
                return "BIGINT UNSIGNED";
            else if (type == typeof(float))
                return "FLOAT";
            else if (type == typeof(double))
                return "DOUBLE";
            else if (type == typeof(string))
                return "VARCHAR(50)";
            else if (type == typeof(DateTime))
                return "TIMESTAMP(3)";
            else
                throw new ArgumentException("Unsupported data type");
        }

    }
}
