using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProcessDataArchiver.DataCore.Database.Schema;
using ProcessDataArchiver.DataCore.DbEntities.Events;
using ProcessDataArchiver.DataCore.DbEntities.Tags;
using FirebirdSql.Data.FirebirdClient;
using ProcessDataArchiver.DataCore.DbEntities;

namespace ProcessDataArchiver.DataCore.Database.CommandProviders
{
    public class FirebirdCommandProvider : IExtendedCmdProvider
    {


        public IEnumerable<string> CreateArchiveCmd(TagArchive archive)
        {
            string cmd;
            if (archive.ArchiveType == ArchiveType.Normal)
            {
                cmd = "CREATE TABLE " + archive.Name +
                "(Id Integer Primary Key, TagId Int, TagTimestamp Timestamp, TagValue Double Precision);";
            }
            else
            {
                cmd = "CREATE TABLE " + archive.Name +
                "(Id Integer Primary Key, TagTimestamp Timestamp);";
            }

            yield return cmd;

            yield return "Create Sequence " + archive.Name + "_Seq;";


            yield return "Create Trigger Ai_" + archive.Name + " For " + archive.Name + " " +
            "Active Before Insert Position 0 " +
            "As Begin " +
            "New.Id = next value for " + archive.Name + "_Seq;" +
            "End;";
        }

        public IEnumerable<string> CreateDbProcedures()
        {
            yield return
                "Create Procedure Create_GlobalVariable(" +
                "Name Varchar(50)," +
                "Address  BigInt," +
                "VariableSize Integer," +
                "NetType Varchar(50)) AS Begin " +
                "Insert Into GlobalVariable(Name,Address,VariableSize,NetType) Values(" +
                ":Name,:Address,:VariableSize,:NetType); End;";

            yield return
                "Create Procedure Create_Tag(" +
                "ArchiveId Integer," +
                "GlobalVariableId Integer,"+
                "TagType Integer," +
                "Name Varchar(50)," +
                "RefreshSpan Double Precision," +
                "ArchivingType Integer," +
                "EuName Varchar(50),"+
                "DeadbandType Integer,"+
                "DeadbandValue Double Precision,"+
                "LastChanged Timestamp,"+
                "Comment Varchar(500)) AS Begin " +
                "Insert Into Tag(" +
                "ArchiveId,GlobalVariableId,TagType,Name,RefreshSpan,ArchivingType,"+
                "EuName,DeadbandType,DeadbandValue,LastChanged,Comment)" +
                "Values(" +
                ":ArchiveId,:GlobalVariableId,:TagType,:Name,:RefreshSpan,:ArchivingType,"+
                ":EuName,:DeadbandType,:DeadbandValue,"+
                ":LastChanged,:Comment); End; ";

            yield return
                "Create Procedure Create_Archive(" +
                "ArchiveType Integer," +
                "Name Varchar(50)) As Begin " +
                "Insert Into Archive(ArchiveType,Name) Values" +
                "(:ArchiveType,:Name); End;";
            yield return
                "Create Procedure Create_Event(" +
                "EventType Integer," +
                "EventActionType Integer," +
                "Name Varchar(50)," +
                "RefreshSpan Double Precision," +
                "ActionText Varchar(500)," +
                "Enabled SmallInt, " +
                "LastChanged Timestamp,"+
                "Comment Varchar(500)) AS Begin " +
                "Insert Into Event(" +
                "EventType, EventActionType,Name, RefreshSpan, ActionText, Enabled, " +
                "LastChanged,Comment) Values" +
                "(:EventType, :EventActionType,:Name, :RefreshSpan, :ActionText, :Enabled, " +
                ":LastChanged,:Comment); End;";
            yield return
                "Create Procedure Create_AnalogEvent(" +
                "GlobalVariableId Integer," +
                "EventId Integer," +
                "EventTriggerType Integer," +
                "TriggerValue Double Precision) AS Begin " +
                "Insert Into AnalogEvent(" +
                "GlobalVariableId,EventId,EventTriggerType,TriggerValue) Values" +
                "(:GlobalVariableId,:EventId,:EventTriggerType,:TriggerValue); End;";
            yield return
                "Create Procedure Create_DiscreteEvent(" +
                "GlobalVariableId Integer," +
                "EventId Integer," +
                "EdgeType Integer) As Begin " +
                "Insert Into DiscreteEvent(" +
                "GlobalVariableId,EventId,EdgeType) Values(" +
                ":GlobalVariableId,:EventId,:EdgeType); End;";
            yield return
                "Create Procedure Create_CyclicEvent(" +
                "EventId Integer," +
                "EventCycleType Integer," +
                "Days Integer,Hours Integer,Minutes Integer,Seconds Integer," +
                "NextEvent Timestamp) As Begin " +
                "Insert Into CyclicEvent(EventId,EventCycleType," +
                "Days,Hours,Minutes,Seconds,NextEvent)" +
                "Values(:EventId,:EventCycleType," +
                ":Days,:Hours,:Minutes,:Seconds,:NextEvent); End;";
            yield return
                "Create Procedure Insert_EventHistory(" +
                "EventId Integer) As Begin " +
                "Insert Into EventHistory(EventId,EventTimestamp) " +
                "Values (:EventId,CURRENT_TIMESTAMP); End;";
            yield return
                "Create Procedure Insert_EventSnapshot(" +
                "EventId Integer,GlobalVariableId Integer,VariableValue Double Precision) As Begin " +
                "Insert Into EventSnapshot(EventId,TagId,VariableValue,EventTimeStamp) " +
                "Values (:EventId,:GlobalVariableId,:VariableValue,CURRENT_TIMESTAMP); End;";
        }

        public IEnumerable<string> CreateDbTables()
        {
            yield return
                "Create Table GlobalVariable(Id Integer Primary Key," +
                "Name Varchar(50)," +
                "Address BigInt," +
                "VariableSize Integer," +
                "NetType Varchar(50));";
            yield return
                "Create Sequence Gv_Seq;";
            yield return
                "Create Trigger Gu_Ai For GlobalVariable " +
                "Active Before Insert Position 0 " +
                "As Begin " +
                "New.Id = next value for Gv_Seq;" +
                "End;";

            yield return
               "Create Table Archive(Id  Integer Primary Key," +
               "ArchiveType Integer," +
               "Name Varchar(50));";
            yield return
                "Create Sequence Archive_Seq;";
            yield return
                "Create Trigger Ar_Ai For Archive " +
                "Active Before Insert Position 0 " +
                "As Begin " +
                "New.Id = next value for Archive_Seq;" +
                "End;";

            yield return
                "Create Table Tag(Id Integer Primary Key," +
                "ArchiveId Integer, " +
                "GlobalVariableId Integer," +
                "TagType Integer," +
                "Name Varchar(50)," +
                "RefreshSpan Integer," +
                "ArchivingType Integer," +
                "EuName Varchar(50),"+
                "DeadbandType Integer,"+
                "DeadbandValue Double Precision,"+
                "LastChanged Timestamp," +
                "Comment Varchar(500)," +
                "Constraint FK_GV Foreign Key(GlobalVariableId) References GlobalVariable(Id) On Delete Cascade, "+
                "Constraint FK_Archive Foreign Key(ArchiveId) References Archive(Id) On Delete Cascade);";
            yield return
                "Create Sequence Tag_Seq;";
            yield return
                "Create Trigger T_Ai For Tag " +
                "Active Before Insert Position 0 " +
                "As Begin " +
                "New.Id = next value for Tag_Seq;" +
                "End;";

            yield return
               "Create Table Event(Id Integer Primary Key," +
               "EventType Integer," +
               "EventActionType Integer, " +
               "Name Varchar(50)," +
               "RefreshSpan Integer," +
               "ActionText Varchar(500)," +
               "Enabled SmallInt," +
               "LastChanged Timestamp," +
               "Comment Varchar(500));";
            yield return
                "Create Sequence Ev_Seq;";
            yield return
                "Create Trigger Ev_Ai For Event " +
                "Active Before Insert Position 0 " +
                "As Begin " +
                "New.Id = next value for Ev_Seq;" +
                "End;";
            yield return
                "Create Table DiscreteEvent(Id Integer Primary Key," +
                "GlobalVariableId Integer," +
                "EventId Integer," +
                "EdgeType Integer," +
                "Constraint FK_DE_EventId Foreign Key (EventId) References Event(Id) On Delete Cascade, " +
                "Constraint FK_DE_GvId Foreign Key (GlobalVariableId) References GlobalVariable(Id) On Delete Cascade);";
            yield return
                "Create Sequence De_Seq;";
            yield return
                "Create Trigger De_Ai For DiscreteEvent " +
                "Active Before Insert Position 0 " +
                "As Begin " +
                "New.Id = next value for De_Seq;" +
                "End;";
            yield return
                "Create Table AnalogEvent(Id Integer Primary Key," +
                "GlobalVariableId Integer," +
                "EventId Integer," +
                "EventTriggerType Integer," +
                "TriggerValue Double Precision," +
                "Constraint FK_AE_EventId Foreign Key (EventId) References Event(Id) On Delete Cascade, " +
                "Constraint FK_AE_TagId Foreign Key (GlobalVariableId) References GlobalVariable(Id) On Delete Cascade);";
            yield return
                "Create Sequence Ae_Seq;";
            yield return
                "Create Trigger Ae_Ai For AnalogEvent " +
                "Active Before Insert Position 0 " +
                "As Begin " +
                "New.Id = next value for Ae_Seq;" +
                "End;";
            yield return
                "Create Table CyclicEvent(Id Integer Primary Key," +
                "EventId Integer," +
                "EventCycleType Integer," +
                "Days Integer, Hours Integer, Minutes Integer, Seconds Integer," +
                "NextEvent Timestamp, " +
                "Constraint FK_CE_EventId Foreign Key (EventId) References Event(Id) On Delete Cascade);";
            yield return
                "Create Sequence Ce_Seq;";
            yield return
                "Create Trigger Ce_Ai For CyclicEvent " +
                "Active Before Insert Position 0 " +
                "As Begin " +
                "New.Id = next value for Ce_Seq;" +
                "End;";
            yield return
                "Create Table EventHistory(Id Integer Primary Key," +
                "EventId Integer," +
                "EventTimestamp Timestamp," +
                "Constraint FK_EH_EventId Foreign Key (EventId) References Event(Id) On Delete Cascade);";
            yield return
                "Create Sequence Eh_Seq;";
            yield return
                "Create Trigger Eh_Ai For EventHistory " +
                "Active Before Insert Position 0 " +
                "As Begin " +
                "New.Id = next value for Eh_Seq;" +
                "End;";
            yield return
                "Create Table EventSnapshot(Id Integer Primary Key," +
                 "EventId Integer," +
                 "TagId Integer," +
                 "VariableValue Double Precision," +
                 "EventTimestamp Timestamp," +
                 "Constraint FK_ES_EventId Foreign Key (EventId) References Event(Id) On Delete Cascade," +
                 "Constraint FK_ES_TagId Foreign Key (TagId) References Tag(Id) On Delete Cascade);";
            yield return
                "Create Sequence Es_Seq;";
            yield return
                "Create Trigger Es_Ai For EventSnapshot " +
                "Active Before Insert Position 0 " +
                "As Begin " +
                "New.Id = next value for Es_Seq;" +
                "End;";

            yield return
                "Create Table EventSummary(Id Integer Primary Key," +
                "EventId Integer," +
                "TagName Varchar(50)," +
                "CalcType Varchar(50)," +
                "FromDate Timestamp," +
                "ToDate Timestamp," +
                "CalcValue Double Precision);";
            yield return
                "Create Sequence Esum_Seq;";
            yield return
                "Create Trigger Esum_Ai For EventSummary " +
                "Active Before Insert Position 0 " +
                "As Begin " +
                "New.Id = next value for Esum_Seq;" +
                "End;";

        }

        public IEnumerable<string> CreateDbViews()
        {

            yield return
                "Create View AnalogEventInfo As " +
                "Select E.Id Id, E.Name Name, E.RefreshSpan RefreshSpan, " +
                "E.Enabled Enabled, E.LastChanged LastChanged," +
                "E.Comment Comment, E.ActionText ActionText, " +
                "Ae.GlobalVariableId GlobalVariableId, Ae.Id AnalogEventId, E.EventActionType As EventActionType," +
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
                "E.EventActionType As EventActionType,"+
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
                    cmd += keys[i] + "=" + $"'{date}'";// " Format('" + date + "','dd.mm.yyyy hh:mm:ss')";
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
                    cmd += keys[i] + "=" + $"'{date}'";// keys[i] + "=" + " Format('" + date + "','dd.mm.yyyy hh:mm:ss')";
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

        public string ChangeDtCmd(ITag tag, Type newType)
        {
            return $"ALTER TABLE {tag.TagArchive.Name} ALTER COLUMN {tag.Name} TYPE {MapToDbType(newType)};";
        }

        public string ChangeNameCmd(string table, string oldName, string newName,Type t)
        {
            return $"ALTER TABLE {table} ALTER COLUMN {oldName} TO {newName};";
        }

        public string InsertSummaryCmd(IEvent archiveEvent, ITag tag, string action, DateTime fd)
        {
            if (tag.TagArchive.ArchiveType == ArchiveType.Wide)
            {
                return  "INSERT INTO EventSummary(EventId,TagName,CalcType,FromDate,ToDate,CalcValue) " +
                    $"Values({archiveEvent.Id},'{tag.Name}','{action}'," +
                    $"'{fd.Year}-{fd.Month}-{fd.Day} {fd.Hour}:{fd.Minute}:{fd.Second}',CURRENT_TIMESTAMP," +
                    $"(SELECT {action}({tag.Name}) FROM {tag.TagArchive.Name} " +
                    $"WHERE TagTimestamp>'{fd.Year}-{fd.Month}-{fd.Day} {fd.Hour}:{fd.Minute}:{fd.Second}'));";

            }

                return "INSERT INTO EventSummary(EventId,TagName,CalcType,FromDate,ToDate,CalcValue) " +
                $"Values({archiveEvent.Id},'{tag.Name}','{action}'," +
                $"'{fd.Year}-{fd.Month}-{fd.Day} {fd.Hour}:{fd.Minute}:{fd.Second}',CURRENT_TIMESTAMP," +
                $"(SELECT {action}(TagValue) FROM {tag.TagArchive.Name} " +
                $"WHERE TagTimestamp>'{fd.Year}-{fd.Month}-{fd.Day} {fd.Hour}:{fd.Minute}:{fd.Second}')" +
                $" AND TagName='{tag.Name}');";
            
        }



        public string InsertTagValueCmd(TagArchive archive, IEnumerable<ITag> tags)
        {
            if (archive.ArchiveType == ArchiveType.Wide)
            {
                string cmd = "Insert Into " + archive.Name + " (TagTimestamp,";
                string cmdEnd = " Values(CURRENT_TIMESTAMP,";
                var aTags = archive.Tags.ToList();
                var cmdPar = new List<FbParameter>();


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
                    " Values(@TagId,CURRENT_TIMESTAMP,@TagValue);";
            }

        }

        public string MapToDbType(Type type)
        {
            if (type == typeof(bool))
                return "SMALLINT";/// z char(1)
            else if (type == typeof(byte) || type == typeof(sbyte) || type == typeof(short))
                return "SMALLINT";
            else if (type == typeof(ushort) || type == typeof(int))
                return "INTEGER";
            else if (type == typeof(uint) || type == typeof(long))
                return "BIGINT";
            else if (type == typeof(ulong))
                return "DOUBLE PRECISION";
            else if (type == typeof(float))
                return "FLOAT";
            else if (type == typeof(double))
                return "DOUBLE PRECISION";
            else if (type == typeof(string))
                return "VARCHAR(50)";
            else if (type == typeof(DateTime))
                return "TIMESTAMP";
            else
                throw new ArgumentException("Unsupported data type");
        }

    }
}
