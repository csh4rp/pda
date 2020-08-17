using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProcessDataArchiver.DataCore.Database.Schema;
using Oracle.ManagedDataAccess.Client;
using System.Numerics;
using ProcessDataArchiver.DataCore.DbEntities.Tags;
using ProcessDataArchiver.DataCore.DbEntities;
using ProcessDataArchiver.DataCore.DbEntities.Events;

namespace ProcessDataArchiver.DataCore.Database.CommandProviders
{
    public class OracleCommandProvider : IExtendedCmdProvider
    {
        public string ChangeDtCmd(ITag tag, Type newType)
        {
            string type = MapToDbType(newType);
            return $"ALTER TABLE {tag.TagArchive.Name} MODIFY {tag.Name} {type}";
        }

        public string ChangeNameCmd(string table, string oldName, string newName, Type t)
        {
            return $"ALTER TABLE {table} RENAME COLUMN {oldName} TO {newName}";
        }

        public IEnumerable<string> CreateArchiveCmd(TagArchive archive)
        {
            string cmd;
            if (archive.ArchiveType == ArchiveType.Normal)
            {
                cmd = "CREATE TABLE " + archive.Name +
                "(Id Number(11,0) Primary Key, TagId Number(11,0), TagTimestamp Timestamp, TagValue Number(35,15))";
            }
            else
            {
                cmd = "CREATE TABLE " + archive.Name +
                "(Id Number(11,0) Primary Key, TagTimestamp Timestamp)";
            }

           yield return cmd;
        }

        public IEnumerable<string> CreateDbProcedures()
        {

            yield return
                "Create Procedure Create_GlobalVariable(" +
                "Name Varchar," +
                "Address Number," +
                "VariableSize Number," +
                "NetType Varchar) AS Begin " +
                "Insert Into GlobalVariable(Name,Address,VariableSize,NetType) Values(" +
                "Name,Address,VariableSize,NetType); End;";
            yield return
                "Create Procedure Create_Tag(" +
                "ArchiveId Number," +
                "GlobalVariableId Number,"+
                "TagType Number," +
                "Name Varchar," +
                "RefreshSpan Number," +
                "ArchivingType Number," +
                "EuName Varchar,"+
                "DeadbandType Number,"+
                "DeadbandValue Number,"+
                "LastChanged Timestamp,"+
                @" ""Comment"" Varchar"+
                ") As Begin " +
                "Insert Into Tag(" +
                "ArchiveId,GlobalVariableId,TagType,Name,RefreshSpan,ArchivingType,"+
                "EuName,DeadbandType,DeadbandValue,LastChanged," +
                @"""Comment"") " +
                "Values(" +
                "ArchiveId,GlobalVariableId,TagType,Name,RefreshSpan,ArchivingType," +
                "EuName,DeadbandType,DeadbandValue,"+
                @"LastChanged,""Comment""); End; ";


            yield return
                "Create Procedure Create_Archive(" +
                "ArchiveType Number," +
                "Name Varchar) As Begin " +
                "Insert Into Archive(ArchiveType,Name) Values" +
                "(ArchiveType,Name); End;";
            yield return
                "Create Procedure Create_Event(" +
                "EventType Number," +
                "EventActionType Number," +
                "Name Varchar," +
                "RefreshSpan Number," +
                "ActionText Varchar," +
                "Enabled Number, " +
                "LastChanged Timestamp,"+
                @" ""Comment"" Varchar) As Begin " +
                "Insert Into Event(" +
                "EventType, EventActionType,Name, RefreshSpan, ActionText, Enabled, " +
                @"LastChanged,""Comment"") Values" +
                "(EventType, EventActionType, Name, RefreshSpan, ActionText, Enabled, " +
                @"LastChanged,""Comment""); End;";
            yield return
                "Create Procedure Create_AnalogEvent(" +
                "GlobalVariableId Number," +
                "EventId Number," +
                "EventTriggerType Number," +
                "TriggerValue Number) As Begin " +
                "Insert Into AnalogEvent(" +
                "GlobalVariableId,EventId,EventTriggerType,TriggerValue) Values" +
                "(GlobalVariableId,EventId,EventTriggerType,TriggerValue); End;";
            yield return
                "Create Procedure Create_DiscreteEvent(" +
                "GlobalVariableId Number," +
                "EventId Number," +
                "EdgeType Number) As Begin " +
                "Insert Into DiscreteEvent(" +
                "GlobalVariableId,EventId,EdgeType) Values(" +
                "GlobalVariableId,EventId,EdgeType); End;";
            yield return
                "Create Procedure Create_CyclicEvent(" +
                "EventId Number," +
                "EventCycleType Number," +
                "Days Number, Hours Number, Minutes Number, Seconds Number," +
                "NextEvent Timestamp) As Begin " +
                "Insert Into CyclicEvent(EventId,EventCycleType," +
                "Days,Hours,Minutes,Seconds,NextEvent)" +
                "Values(EventId,EventCycleType," +
                "Days,Hours,Minutes,Seconds,NextEvent); End;";
            yield return
                "Create Procedure Insert_EventHistory(" +
                "EventId Number) As Begin " +
                "Insert Into EventHistory(EventId,EventTimestamp) " +
                "Values (EventId,CURRENT_TIMESTAMP); End;";
            yield return
                "Create Procedure Insert_EventSnapshot(" +
                "EventId Number,TagId Number,Value Number) As Begin " +
                "Insert Into EventSnapshot(EventId,TagId,eValue,EventTimeStamp) " +
                "Values (EventId,TagId,Value,CURRENT_TIMSTAMP); End;";
        }

        public IEnumerable<string> CreateDbTables()
        {
            yield return
                "Create Table GlobalVariable(Id Number(11,0) Primary Key," +
                "Name Varchar(50)," +
                "Address Number(20,0)," +
                "VariableSize Number(11,0)," +
                "NetType Varchar(50))";
            yield return
                "Create Sequence Gv_Seq";
            yield return
                "Create Trigger Gv_Ai " +
                "Before Insert On GlobalVariable " +
                "For EACH ROW " +
                "Begin " +
                "Select Gv_Seq.NEXTVAL " +
                "Into :new.Id " +
                "From dual; " +
                "End; ";



            yield return
               "Create Table Archive(Id  Number(11,0)  Primary Key," +
               "ArchiveType Number(11,0)," +
               "Name Varchar(50))";
            yield return
                "Create Sequence Ar_Seq";
            yield return
                "Create Trigger Ar_Ai " +
                "Before Insert On Archive " +
                "For EACH ROW " +
                "Begin " +
                "Select Ar_Seq.NEXTVAL " +
                "Into :new.Id " +
                "From Dual; " +
                "End;";

            yield return
                "Create Table Tag(Id Number(11,0)  Primary Key," +
                "ArchiveId Number(11,0), " +
                "GlobalVariableId Number(11,0),"+
                "TagType Number(11,0)," +
                "Name Varchar(50)," +
                "RefreshSpan Number(11,0)," +
                "ArchivingType Number(11,0)," +
                "EuName varchar(50),"+
                "DeadbandType Number(11,0),"+
                "DeadbandValue Number(35,15),"+
                "LastChanged Timestamp," +
                @"""Comment"" Varchar(500)," +
                "Constraint FK_GV Foreign Key(GlobalVariableId) References GlobalVariable(Id) On Delete Cascade,"+
                "Constraint FK_Archive Foreign Key(ArchiveId) References Archive(Id) On Delete Cascade)";
            yield return
                "Create Sequence Tag_Seq";
            yield return
                "Create Trigger Tag_Ai " +
                "Before Insert On Tag " +
                "For EACH ROW " +
                "Begin " +
                "Select Tag_Seq.NEXTVAL " +
                "Into :new.Id " +
                "From Dual; " +
                "End;";


            yield return
               "Create Table Event(Id Number(11,0)  Primary Key," +
               "EventType Number(11,0)," +
               "EventActionType Number(11,0), " +
               "Name Varchar(50)," +
               "RefreshSpan Number(11,0)," +
               "ActionText Varchar(500)," +
               "Enabled Number(5,0)," +
               "LastChanged Timestamp," +
               @"""Comment"" Varchar(500))";
            yield return
                "Create Sequence Ev_Seq";
            yield return
                "Create Trigger Ev_Ai " +
                "Before Insert On Event " +
                "For EACH ROW " +
                "Begin " +
                "Select Ev_Seq.NEXTVAL " +
                "Into :new.Id " +
                "From Dual; " +
                "End;";

            yield return
                "Create Table DiscreteEvent(Id Number(11,0)  Primary Key," +
                "GlobalVariableId Number(11,0)," +
                "EventId Number(11,0)," +
                "EdgeType Number(11,0)," +
                "Constraint FK_DE_EventId Foreign Key (EventId) References Event(Id) On Delete Cascade, " +
                "Constraint FK_DE_GvId Foreign Key (GlobalVariableId) References GlobalVariable(Id) On Delete Cascade)";
            yield return
                "Create Sequence De_Seq";
            yield return
                "Create Trigger De_Ai " +
                "Before Insert On DiscreteEvent " +
                "For EACH ROW " +
                "Begin " +
                "Select De_Seq.NEXTVAL " +
                "Into :new.Id " +
                "From Dual; " +
                "End;";

            yield return
                "Create Table AnalogEvent(Id Number(11,0)  Primary Key," +
                "GlobalVariableId Number(11,0)," +
                "EventId Number(11,0)," +
                "EventTriggerType Number(11,0)," +
                "TriggerValue Number(35,15)," +
                "Constraint FK_AE_EventId Foreign Key (EventId) References Event(Id) On Delete Cascade, " +
                "Constraint FK_AE_GvId Foreign Key (GlobalVariableId) References GlobalVariable(Id) On Delete Cascade)";
            yield return
                "Create Sequence Ae_Seq";
            yield return
                "Create Trigger Ae_Ai " +
                "Before Insert On AnalogEvent " +
                "For EACH ROW " +
                "Begin " +
                "Select Ae_Seq.NEXTVAL " +
                "Into :new.Id " +
                "From Dual; " +
                "End;";

            yield return
                "Create Table CyclicEvent(Id Number(11,0)  Primary Key," +
                "EventId Number(11,0)," +
                "EventCycleType Number(11,0)," +
                "Days Number(11,0), Hours Number(11,0), Minutes Number(11,0), Seconds Number(11,0)," +
                "NextEvent Timestamp, " +
                "Constraint FK_CE_EventId Foreign Key (EventId) References Event(Id) On Delete Cascade)";
            yield return
                "Create Sequence Ce_Seq";
            yield return
                "Create Trigger Ce_Ai " +
                "Before Insert On CyclicEvent " +
                "For EACH ROW " +
                "Begin " +
                "Select Ce_Seq.NEXTVAL " +
                "Into :new.Id " +
                "From Dual; " +
                "End;";

            yield return
                "Create Table EventHistory(Id Number(11,0)  Primary Key," +
                "EventId Number(11,0)," +
                "EventTimestamp Timestamp," +
                "Constraint FK_EH_EventId Foreign Key (EventId) References Event(Id) On Delete Cascade)";
            yield return
                "Create Sequence Eh_Seq";
            yield return
                "Create Trigger Eh_Ai " +
                "Before Insert On EventHistory " +
                "For EACH ROW " +
                "Begin " +
                "Select Eh_Seq.NEXTVAL " +
                "Into :new.Id " +
                "From Dual; " +
                "End;";

            yield return
                "Create Table EventSnapshot(Id Number(11,0)  Primary Key," +
                 "EventId Number(11,0)," +
                 "TagId Number(11,0)," +
                 "Value Number(35,15)," +
                 "EventTimestamp Timestamp," +
                 "Constraint FK_ES_EventId Foreign Key (EventId) References Event(Id) On Delete Cascade," +
                 "Constraint FK_ES_TagId Foreign Key (TagId) References Tag(Id) On Delete Cascade)";
            yield return
                "Create Sequence Es_Seq";
            yield return
                "Create Trigger Es_Ai " +
                "Before Insert On EventSnapshot " +
                "For EACH ROW " +
                "Begin " +
                "Select Es_Seq.NEXTVAL " +
                "Into :new.Id " +
                "From Dual; " +
                "End;";

            yield return
                "Create Table EventSummary(Id Number(11,0) Primary Key," +
                     "EventId Number(11,0)," +
                     "TagName Varchar(50)," +
                     "CalcType Varchar(50)," +
                     "FromDate Timestamp," +
                     "ToDate Timestamp," +
                     "CalcValue Number(35,15))";

            yield return
                "Create Sequence Esum_Seq";
            yield return
                "Create Trigger Esum_Ai " +
                "Before Insert On EventSummary " +
                "For EACH ROW " +
                "Begin " +
                "Select Esum_Seq.NEXTVAL " +
                "Into :new.Id " +
                "From Dual; " +
                "End;";
        }

        public IEnumerable<string> CreateDbViews()
        {


            // Widoki dla eventow
            yield return
                "Create View AnalogEventInfo As " +
                "Select E.Id Id, E.Name Name, E.RefreshSpan RefreshSpan, " +
                "E.Enabled Enabled, E.LastChanged LastChanged," +
                @"E.""Comment""  ""Comment""," +
                "E.ActionText ActionText, " +
                "Ae.GlobalVariableId GlobalVariableId, Ae.Id AnalogEventId, E.EventActionType As EventActionType," +
                "Ae.TriggerValue TriggerValue, Ae.EventTriggerType EventTriggerType " +
                "From Event E " +
                "Inner Join AnalogEvent Ae On E.Id=Ae.EventId";

            yield return
                "Create View DiscreteEventInfo As " +
                "Select E.Id Id, E.Name Name, E.RefreshSpan RefreshSpan, " +
                "E.Enabled Enabled, E.LastChanged LastChanged," +
                @"E.""Comment"" ""Comment""," +
                "E.ActionText ActionText, " +
                "E.EventActionType As EventActionType," +
                "De.GlobalVariableId GlobalVariableId, " +
                "De.EdgeType EdgeType, De.Id DiscreteEventId " +
                "From Event E " +
                "Inner Join DiscreteEvent De On E.Id=De.EventId";

            yield return
                "Create View CyclicEventInfo As " +
                "Select E.Id Id, E.Name Name, E.RefreshSpan RefreshSpan, " +
                "E.Enabled Enabled, E.LastChanged LastChanged," +
                "E.EventActionType As EventActionType," +
                @"E.""Comment"" ""Comment""," +
                "E.ActionText ActionText, " +
                "C.Days Days," +
                "C.Hours Hours, C.Minutes Minutes," +
                "C.Seconds Seconds, C.EventCycleType EventCycleType, " +
                "C.Id CyclicEventId, C.NextEvent NextEvent " +
                "From Event E " +
                "Inner Join CyclicEvent C On E.Id=C.EventId";

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


            return sb.ToString();
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
                if (keys[i].Equals("Comment"))
                {
                    cmd += $@"""{keys[i]}""=" + "'" + values[keys[i]] + "'";
                }
                else if (values[keys[i]].GetType().Equals(typeof(string)))
                    cmd += keys[i] + "=" + "'" + values[keys[i]] + "'";
                else if (values[keys[i]].GetType().Equals(typeof(DateTime)))
                {
                    var date = (DateTime)values[keys[i]];
                    cmd += keys[i] + "= TO_TIMESTAMP(" + $"'{date.Year}-{date.Month}-{date.Day} {date.Hour}:{date.Minute}:{date.Second}',"+
                        "'YYYY-MM-DD HH24:MI:SS')";
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
                    $"WHERE TagTimestamp>'{fd.Year}-{fd.Month}-{fd.Day} {fd.Hour}:{fd.Minute}:{fd.Second}'))";
                
            }
            else
            {
                return "INSERT INTO EventSummary(EventId,TagName,CalcType,FromDate,ToDate,CalcValue) " +
                $"Values({archiveEvent.Id},'{tag.Name}','{action}'," +
                $"'{fd.Year}-{fd.Month}-{fd.Day} {fd.Hour}:{fd.Minute}:{fd.Second}',CURRENT_TIMESTAMP," +
                $"(SELECT {action}(TagValue) FROM {tag.TagArchive.Name} " +
                $"WHERE TagTimestamp>'{fd.Year}-{fd.Month}-{fd.Day} {fd.Hour}:{fd.Minute}:{fd.Second}')" +
                $" AND TagName='{tag.Name}')";
            }
        }

        public string InsertTagValueCmd(TagArchive archive, IEnumerable<ITag> tags)
        {
            if (archive.ArchiveType == ArchiveType.Wide)
            {
                string cmd = "Insert Into " + archive.Name + " (TagTimestamp,";
                string cmdEnd = " Values(CURRENT_TIMESTAMP,";
                var aTags = archive.Tags.ToList();
                var cmdPar = new List<OracleParameter>();


                for (int i = 0; i < aTags.Count; i++)
                {
                    if (i < (aTags.Count - 1))
                    {
                        cmd += aTags[i].Name + ",";
                        cmdEnd += ":" + aTags[i].Name + ",";
                        //var tagsToSave = item.ToList();
                    }
                    else
                    {
                        cmd += aTags[i].Name + ") ";
                        cmdEnd += ":" + aTags[i].Name + ")";
                    }
                    if (tags.Select(it => it.Name).Contains(aTags[i].Name))
                        cmdPar.Add(new OracleParameter("" + aTags[i].Name, aTags[i].CurrentValue));
                    else
                        cmdPar.Add(new OracleParameter("" + aTags[i].Name, DBNull.Value));
                }
                return cmd + cmdEnd;
            }
            else
            {
                return "Insert Into " + archive.Name + " (TagId,TagTimeStamp,TagValue)" +
                    " Values(:TagId,CURRENT_TIMESTAMP,:TagValue)";

            }
        }

        public string MapToDbType(Type type)
        {
            if (type == typeof(bool))
                return "NUMBER(1,0)"; ///?????
            else if (type == typeof(byte) || type == typeof(sbyte))
                return "NUMBER(3,0)";
            else if (type == typeof(short) || type == typeof(ushort))
                return "NUMBER(5,0)";
            else if (type == typeof(int) || type == typeof(uint))
                return "NUMBER(11,0)";
            else if (type == typeof(long) || type == typeof(ulong))
                return "NUMBER(20,0)";
            else if (type == typeof(float))
                return "NUMBER(35,7)";
            else if (type == typeof(double))
                return "NUMBER(35,15)";
            else if (type == typeof(string))
                return "VARCHAR2(50)";
            else if (type == typeof(DateTime))
                return "TIMESTAMP";
            else
                throw new ArgumentException("Unsupported data type");
        }

        public  string MapToProcedureType(Type type)
        {
            if (type == typeof(String))
                return "VARCHAR2";
            else if (type == typeof(DateTime))
                return "TIMESTAMP";
            else if (type == typeof(Int32) || type == typeof(Int16)
                || type == typeof(UInt32) || type == typeof(UInt16)
                || type == typeof(UInt64) || type == typeof(Int64)
                || type == typeof(Byte) || type == typeof(sbyte) || type == typeof(BigInteger))
                return "NUMBER";
            else if (type == typeof(float) || type == typeof(double))
                return "NUMBER";
            else if (type == typeof(Boolean))
                return "NUMBER";
            else
                throw new ArgumentException("Unsupported data type");
        }

    }
}
