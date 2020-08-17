using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProcessDataArchiver.DataCore.Database.Schema;
using ProcessDataArchiver.DataCore.DbEntities.Tags;
using NpgsqlTypes;
using ProcessDataArchiver.DataCore.DbEntities;
using ProcessDataArchiver.DataCore.DbEntities.Events;

namespace ProcessDataArchiver.DataCore.Database.CommandProviders
{
    public class PostgresCommandProvider : IExtendedCmdProvider
    {
        public string ChangeDtCmd(ITag tag, Type newType)
        {
            string typeToChange = MapToDbType(newType);
            return $"ALTER TABLE {tag.TagArchive.Name} ALTER COLUMN  {tag.Name} TYPE {typeToChange};";
        }

        public string ChangeNameCmd(string table, string oldName, string newName, Type t)
        {
           return $"AlTER {table} RENAME COLUMN {oldName} TO {newName};";
        }

        public IEnumerable<string> CreateArchiveCmd(TagArchive archive)
        {
            string cmd;
            if (archive.ArchiveType == ArchiveType.Normal)
            {
                cmd = "CREATE TABLE " + archive.Name +
                "(Id Serial Primary Key, TagId Int, TagTimestamp Timestamp, TagValue Double Precision);";
            }
            else
            {
                cmd = "CREATE TABLE " + archive.Name +
                "(Id Serial Primary Key, TagTimestamp Timestamp);";
            }

           yield return cmd;
        }

        public IEnumerable<string> CreateDbProcedures()
        {
            yield return
                "Create Function Create_GlobalVariable(" +
                "pName character varying," +
                "pAddress BigInt," +
                "pVariableSize Int," +
                "pNetType character varying) Returns Void AS $$ " +
                "Begin "+
                "Insert Into GlobalVariable(Name,Address,VariableSize,NetType) Values(" +
                "pName,pAddress,pVariableSize,pNetType); End; "+
                 "$$ LANGUAGE plpgsql;";
            yield return
                "Create Function Create_Tag(" +
                "pArchiveId Integer," +
                "pGlobalVariableId Integer,"+
                "pTagType Integer," +
                "pName character varying," +
                "pRefreshSpan Integer," +
                "pArchivingType Integer," +
                "pEuName character varying,"+
                "pDeadbandType Integer,"+
                "pDeadbandValue Double Precision,"+
                "pLastChanged Timestamp with time zone,"+
                "pComment character varying) Returns Void As $$ " +
                "Begin " +
                "Insert Into Tag(" +
                "ArchiveId,GlobalVariableId,TagType,Name,RefreshSpan,ArchivingType,"+
                "EuName,DeadbandType,DeadbandValue,LastChanged,Comment) " +
                "Values(" +
                "pArchiveId,pGlobalVariableId,pTagType,pName,pRefreshSpan,pArchivingType," +
                "pEuName,pDeadbandType,pDeadbandValue,pLastChanged,pComment); End; " +
                "$$ LANGUAGE plpgsql;";


            yield return
                "Create Function Create_Archive(" +
                "pArchiveType Integer," +
                "pName character varying)Returns Void As $$ Begin " +
                "Insert Into Archive(ArchiveType,Name) Values" +
                "(pArchiveType,pName); End;"+
                "$$ LANGUAGE plpgsql;";
            yield return
                "Create Function Create_Event(" +
                "pEventType Integer," +
                "pEventActionType Integer," +
                "pName character varying," +
                "pRefreshSpan Integer," +
                "pActionText character varying," +
                "pEnabled Integer, " +
                "pLastChanged Timestamp with time zone,"+
                "pComment character varying)Returns Void As $$ Begin " +
                "Insert Into Event(" +
                "EventType, EventActionType,Name, RefreshSpan, ActionText, Enabled, " +
                "LastChanged,Comment) Values" +
                "(pEventType, pEventActionType, pName, pRefreshSpan, pActionText, pEnabled, " +
                "pLastChanged,pComment); End;"+
                "$$ LANGUAGE plpgsql;";
            yield return
                "Create Function Create_AnalogEvent(" +
                "pGlobalVariableId Integer," +
                "pEventId Integer," +
                "pEventTriggerType Integer," +
                "pTriggerValue Double Precision)Returns Void As $$ Begin " +
                "Insert Into AnalogEvent(" +
                "GlobalVariableId,EventId,EventTriggerType,TriggerValue) Values" +
                "(pGlobalVariableId,pEventId,pEventTriggerType,pTriggerValue); End;" +
                "$$ LANGUAGE plpgsql;";
            yield return
                "Create Function Create_DiscreteEvent(" +
                "pGlobalVariableId Integer," +
                "pEventId Integer," +
                "pEdgeType Integer)Returns Void As $$ Begin " +
                "Insert Into DiscreteEvent(" +
                "GlobalVariableId,EventId,EdgeType) Values(" +
                "pGlobalVariableId,pEventId,pEdgeType); End;" +
                "$$ LANGUAGE plpgsql;";
            yield return
                "Create Function Create_CyclicEvent(" +
                "pEventId Integer," +
                "pEventCycleType Integer," +
                "pDays Integer,pHours Integer,pMinutes Integer,pSeconds Integer," +
                "pNextEvent Timestamp)Returns Void As $$ Begin " +
                "Insert Into CyclicEvent(EventId,EventCycleType," +
                "Days,Hours,Minutes,Seconds,NextEvent)" +
                "Values(pEventId,pEventCycleType," +
                "pDays,pHours,pMinutes,pSeconds,pNextEvent); End;"+
                "$$ LANGUAGE plpgsql;";
            yield return
                "Create Function Insert_EventHistory(" +
                "pEventId Integer) Returns Void As $$"+
                "Begin " +
                "Insert Into EventHistory(EventId,EventTimestamp) " +
                "Values (pEventId,NOW()); End; "+
                "$$ LANGUAGE plpgsql;";
            yield return
                "Create Function Insert_EventSnapshot(" +
                "peventid Integer,ptagid Integer,ptagvalue Double Precision) Returns Void As $$ "+
                "Begin " +
                "Insert Into EventSnapshot(EventId,TagId,TagValue,EventTimeStamp) " +
                "Values (peventid,ptagid,ptagvalue,NOW()); End; "+
                "$$ LANGUAGE plpgsql;";
        }

        public IEnumerable<string> CreateDbTables()
        {
            yield return
                "Create Table GlobalVariable(Id SERIAL Primary Key," +
                "Name character varying(50)," +
                "Address BigInt," +
                "VariableSize Integer," +
                "NetType character varying(50));";

            yield return
               "Create Table Archive(Id  SERIAL Primary Key," +
               "ArchiveType Integer," +
               "Name character varying(50));";

            yield return
                "Create Table Tag(Id SERIAL Primary Key," +
                "ArchiveId Integer, " +
                "GlobalVariableId Integer,"+
                "TagType Integer," +
                "Name character varying(50)," +
                "RefreshSpan Integer," +
                "ArchivingType Integer," +
                "EuName character varying(50),"+
                "DeadbandType Integer,"+
                "DeadbandValue Double Precision,"+
                "LastChanged Timestamp," +
                "Comment character varying(500)," +
                "Constraint FK_GV Foreign Key(GlobalVariableId) References GlobalVariable(Id) On Delete Cascade, " +
                "Constraint FK_Archive Foreign Key(ArchiveId) References Archive(Id) On Delete Cascade);";


            yield return
               "Create Table Event(Id SERIAL Primary Key," +
               "EventType Integer," +
               "EventActionType Integer, " +
               "Name character varying(50)," +
               "RefreshSpan Integer," +
               "ActionText character varying(500)," +
               "Enabled SmallInt," +
               "LastChanged Timestamp," +
               "Comment character varying(500));";
            yield return
                "Create Table DiscreteEvent(Id SERIAL Primary Key," +
                "GlobalVariableId Integer," +
                "EventId Integer," +
                "EdgeType Integer," +
                "Constraint FK_DE_EventId Foreign Key (EventId) References Event(Id) On Delete Cascade, " +
                "Constraint FK_DE_TagId Foreign Key (GlobalVariableId) References GlobalVariable(Id) On Delete Cascade);";
            yield return
                "Create Table AnalogEvent(Id SERIAL Primary Key," +
                "GlobalVariableId Integer," +
                "EventId Integer," +
                "EventTriggerType Integer," +
                "TriggerValue Double Precision," +
                "Constraint FK_AE_EventId Foreign Key (EventId) References Event(Id) On Delete Cascade, " +
                "Constraint FK_AE_TagId Foreign Key (GlobalVariableId) References GlobalVariable(Id) On Delete Cascade);";

            yield return
                "Create Table CyclicEvent(Id SERIAL Primary Key," +
                "EventId Integer," +
                "EventCycleType Integer," +
                "Days Integer, Hours Integer, Minutes Integer, Seconds Integer," +
                "NextEvent Timestamp, " +
                "Constraint FK_CE_EventId Foreign Key (EventId) References Event(Id) On Delete Cascade);";
            yield return
                "Create Table EventHistory(Id SERIAL Primary Key," +
                "EventId Integer," +
                "EventTimestamp Timestamp," +
                "Constraint FK_EH_EventId Foreign Key (EventId) References Event(Id) On Delete Cascade);";
            yield return
                "Create Table EventSnapshot(Id SERIAL Primary Key," +
                 "EventId Integer," +
                 "TagId Integer," +
                 "TagValue Double Precision," +
                 "EventTimestamp Timestamp," +
                 "Constraint FK_ES_EventId Foreign Key (EventId) References Event(Id) On Delete Cascade," +
                 "Constraint FK_ES_TagId Foreign Key (TagId) References Tag(Id) On Delete Cascade);";
            yield return
                "Create Table EventSummary(Id SERIAL Primary Key," +
                "EventId Integer," +
                "TagName character varying(50)," +
                "CalcType character varying(50)," +
                "FromDate Timestamp," +
                "ToDate Timestamp," +
                "CalcValue Double Precision);";

        }

        public IEnumerable<string> CreateDbViews()
        {


            // Widoki dla eventow
            yield return
                "Create View AnalogEventInfo As " +
                "Select E.Id As Id, E.Name As Name, E.RefreshSpan As RefreshSpan, " +
                "E.Enabled As Enabled, E.LastChanged As LastChanged," +
                "E.Comment As Comment, E.ActionText AS ActionText, " +
                "Ae.GlobalVariableId As GlobalVariableId, Ae.Id As AnalogEventId,"+
                "E.EventActionType As EventActionType," +
                "Ae.TriggerValue As TriggerValue, Ae.EventTriggerType As EventTriggerType " +
                "From (Event E " +
                "Inner Join AnalogEvent Ae On E.Id=Ae.EventId);";

            yield return
                "Create View DiscreteEventInfo As " +
                "Select E.Id As Id, E.Name As Name, E.RefreshSpan As RefreshSpan, " +
                "E.Enabled As Enabled, E.LastChanged As LastChanged," +
                "E.Comment As Comment, E.ActionText As ActionText, " +
                "E.EventActionType As EventActionType," +
                "De.GlobalVariableId As GlobalVariableId, " +
                "De.EdgeType As EdgeType, De.Id DiscreteEventId " +
                "From (Event E " +
                "Inner Join DiscreteEvent De On E.Id=De.EventId);";

            yield return
                "Create View CyclicEventInfo As " +
                "Select E.Id As Id, E.Name As Name, E.RefreshSpan As RefreshSpan, " +
                "E.Enabled As Enabled, E.LastChanged As LastChanged," +
                "E.EventActionType As EventActionType," +
                "E.Comment As Comment, E.ActionText As ActionText, " +
                "C.Days As Days," +
                "C.Hours As Hours, C.Minutes As Minutes," +
                "C.Seconds As Seconds, C.EventCycleType As EventCycleType, " +
                "C.Id As CyclicEventId, C.NextEvent As NextEvent " +
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
                    $"'{fd.Year}-{fd.Month}-{fd.Day} {fd.Hour}:{fd.Minute}:{fd.Second}',CURRENT_TIMESTAMP," +
                    $"(SELECT {action}({tag.Name}) FROM {tag.TagArchive.Name} " +
                    $"WHERE TagTimestamp>'{fd.Year}-{fd.Month}-{fd.Day} {fd.Hour}:{fd.Minute}:{fd.Second}'));";
                
            }
            else
            {
                return "INSERT INTO EventSummary(EventId,TagName,CalcType,FromDate,ToDate,CalcValue) " +
                $"Values({archiveEvent.Id},'{tag.Name}','{action}'," +
                $"'{fd.Year}-{fd.Month}-{fd.Day} {fd.Hour}:{fd.Minute}:{fd.Second}',CURRENT_TIMESTAMP," +
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



                for (int i = 0; i < aTags.Count; i++)
                {
                    if (i < (aTags.Count - 1))
                    {
                        cmd += aTags[i].Name + ",";
                        cmdEnd += "@" + aTags[i].Name + ",";
                        var tagsToSave = tags.ToList();
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
                return "BIT";////////////////////////
            else if (type == typeof(byte) || type == typeof(short) || type == typeof(sbyte))
                return "SMALLINT";
            else if (type == typeof(ushort) || type == typeof(int))
                return "INTEGER";
            else if (type == typeof(uint) || type == typeof(long))
                return "BIGINT";
            else if (type == typeof(ulong))
                return "NUMERIC";
            else if (type == typeof(float) || type == typeof(Double))
                return "Double Precision";
            else if (type == typeof(string))
                return "character varying";
            else if (type == typeof(DateTime))
                return "TIMESTAMP";
            else
                throw new ArgumentException("Unsupported data type");
        }
    }
}
