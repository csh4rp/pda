using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProcessDataArchiver.DataCore.Database.Schema;
using System.Data.OleDb;
using ProcessDataArchiver.DataCore.DbEntities.Events;
using ProcessDataArchiver.DataCore.DbEntities.Tags;
using ProcessDataArchiver.DataCore.DbEntities;

namespace ProcessDataArchiver.DataCore.Database.CommandProviders
{
    public class AccessCommandProvider : ICommandProvider
    {
        

        public AccessCommandProvider()
        {
            
        }




        public IEnumerable<string> CreateDbProcedures()
        {
            yield return
                "Create Procedure Create_GlobalVariable(" +
                "@Name Varchar," +
                "@Address Numeric(20,0)," +
                "@VariableSize Int," +
                "@NetType Varchar) AS " +
                "Insert Into GlobalVariable(Name,Address,VariableSize,NetType) Values(" +
                "@Name,@Address,@VariableSize,@NetType);";

            yield return                
                "Create Procedure Create_Tag("+
                "@ArchiveId Int," +
                "@GlobalVariableId Int,"+
                "@TagType Int," +
                "@Name Varchar,"+
                "@RefreshSpan Numeric(20,0),"+
                "@ArchivingType Int,"+
                "@EuName Varchar,"+
                "@DeadbandType Int,"+
                "@DeadbandValue Double,"+
                "@LastChanged DateTime,"+
                "@Comment LongText) AS " +
                "Insert Into Tag("+
                "ArchiveId,GlobalVariableId,TagType,Name,RefreshSpan,ArchivingType,"+
                "EuName,DeadbandType,DeadbandValue,LastChanged,Comment) " +
                "Values("+
                "@ArchiveId,@GlobalVariableId,@TagType,@Name,@RefreshSpan,"+
                "@ArchivingType,@EuName,@DeadbandType,@DeadbandValue,@LastChanged,@Comment); ";


            yield return
                "Create Procedure Create_Archive("+
                "@ArchiveType Int,"+
                "@Name Varchar) As " +
                "Insert Into Archive(ArchiveType,Name) Values"+
                "(@ArchiveType,@Name);";
            yield return
                "Create Procedure Create_Event("+
                "@EventType Int,"+
                "@EventActionType Int," +
                "@Name Varchar,"+
                "@RefreshSpan Numeric(20),"+
                "@ActionText LongText,"+
                "@Enabled Short, " +
                "@LastChanged DateTime,"+
                "@Comment LongText) AS " +
                "Insert Into Event("+
                "EventType, EventActionType,Name, RefreshSpan, ActionText, Enabled, " +
                "LastChanged,Comment) Values"+
                "(@EventType, @EventActionType,@Name, @RefreshSpan, @ActionText, @Enabled, " +
                "@LastChanged,@Comment);";
            yield return
                "Create Procedure Create_AnalogEvent("+
                "@GlobalVariableId Int,"+
                "@EventId Int,"+
                "@EventTriggerType Int," +
                "@TriggerValue Double) AS " +
                "Insert Into AnalogEvent("+
                "GlobalVariableId,EventId,EventTriggerType,TriggerValue) Values"+
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
                "@NextEvent Date) As " +
                "Insert Into CyclicEvent(EventId,EventCycleType," +
                "Days,Hours,Minutes,Seconds,NextEvent)" +
                "Values(@EventId,@EventCycleType," +
                "@Days,@Hours,@Minutes,@Seconds,@NextEvent);";
            yield return
                "Create Procedure Insert_EventHistory(" +
                "@EventId Int) As " +
                "Insert Into EventHistory(EventId,EventTimestamp) " +
                "Values (@EventId,NOW());";
            yield return
                "Create Procedure Insert_EventSnapshot(" +
                "@EventId Int,@TagId Int,@Value Double) As " +
                "Insert Into EventSnapshot(EventId,GlobalVariableId,VariableValue,EventTimeStamp) " +
                "Values (@EventId,@TagId,@Value,Now())";


            yield return
                "Create Procedure Delete_Tag(@Id Int) As Delete From Tag Where Id=@Id;";
            yield return
                "Create Procedure Delete_Event(@Id Int) As Delete From Event Where Id=@Id;";





        }

        public IEnumerable<string> CreateDbTables()
        {
            yield return
                "Create Table GlobalVariable(Id Autoincrement Primary Key," +
                "Name Varchar(50)," +
                "Address Numeric(20,0)," +
                "VariableSize Int," +
                "NetType Varchar(50));";
            yield return
               "Create Table Archive(Id  Autoincrement Primary Key," +
               "ArchiveType Int," +
               "Name Varchar(50));";
            yield return
                "Create Table Tag(Id  Autoincrement Primary Key," +
                "ArchiveId Int, " +
                "GlobalVariableId Int,"+
                "TagType Int," +
                "Name Varchar(50),"+
                "RefreshSpan Int,"+
                "ArchivingType Int,"+
                "EuName Varchar(50),"+
                "DeadbandType Int,"+
                "DeadbandValue Double,"+
                "LastChanged DateTime,"+
                "Comment LongText," +
                "Constraint FK_GV Foreign Key(GlobalVariableId) References GlobalVariable(Id) On Delete Cascade, "+
                "Constraint FK_Archive Foreign Key(ArchiveId) References Archive(Id) On Delete Cascade);";

            yield return
               "Create Table Event(Id  Autoincrement Primary Key," +
               "EventType Int," +
               "EventActionType Int, " +
               "Name Varchar(50)," +
               "RefreshSpan Int," +
               "ActionText LongText," +
               "Enabled Short," +
               "LastChanged DateTime," +
               "Comment LongText);";
            yield return
                "Create Table DiscreteEvent(Id Autoincrement Primary Key," +
                "GlobalVariableId Int," +
                "EventId Int," +
                "EdgeType Int," +
                "Constraint FK_DE_EventId Foreign Key (EventId) References Event(Id) On Delete Cascade, " +
                "Constraint FK_DE_GvId Foreign Key (GlobalVariableId) References GlobalVariable(Id) On Delete Cascade);";
            yield return
                "Create Table AnalogEvent(Id  Autoincrement Primary Key," +
                "GlobalVariableId Int," +
                "EventId Int," +
                "EventTriggerType Int," +
                "TriggerValue Double," +
                "Constraint FK_AE_EventId Foreign Key (EventId) References Event(Id) On Delete Cascade, " +
                "Constraint FK_AE_GvId Foreign Key (GlobalVariableId) References GlobalVariable(Id) On Delete Cascade);";
            yield return
                "Create Table CyclicEvent(Id  Autoincrement Primary Key," +
                "EventId Int,"+
                "EventCycleType Int,"+
                "Days Int, Hours Int, Minutes Int, Seconds Int,"+
                "NextEvent Date, "+
                "Constraint FK_CE_EventId Foreign Key (EventId) References Event(Id) On Delete Cascade);";
            yield return
                "Create Table EventHistory(Id Autoincrement Primary Key," +
                "EventId Int," +
                "EventTimestamp Date," +
                "Constraint FK_EH_EventId Foreign Key (EventId) References Event(Id) On Delete Cascade);";
            yield return
                "Create Table EventSnapshot(Id Autoincrement Primary Key," +
                 "EventId Int," +
                 "TagId Int," +
                 "Value Double," +
                 "EventTimestamp Date," +
                 "Constraint FK_ES_EventId Foreign Key (EventId) References Event(Id) On Delete Cascade," +
                 "Constraint FK_ES_TagId Foreign Key (TagId) References Tag(Id) On Delete Cascade);";
            yield return
                "Create Table EventSummary(Id Autoincrement Primary Key," +
                    "EventId Int," +
                    "TagName Varchar(50)," +
                    "CalcType Varchar(50)," +
                    "FromDate Date," +
                    "ToDate Date," +
                    "CalcValue Double);";
            

        }

        public IEnumerable<string> CreateDbViews()
        {


            //yield return
            //    "Create View AnalogTagInfo As " +
            //    "Select "+
            //    "[T].[Id] As [Id],"+
            //    "[T].[Name] As [Name], [T].[GlobalVariableId] As [GlobalVariableId]," +
            //    "[T].[ArchiveId] As [ArchiveId]," +
            //    "[T].[RefreshSpan] As [RefreshSpan], [T].[Enabled] As [Enabled]," +
            //    "[T].[LastChanged] As [LastChanged], [T].[Comment] As [Comment]," +
            //    "[At].[Id] As [AnalogTagId], [At].[EuId] As [EuId]," +
            //    "[At].[MinEu] As [MinEu], [At].[MaxEu] As [MaxEu], [At].[MinRaw] As [MinRaw]," +
            //    "[At].[MaxRaw] As [MaxRaw]," +
            //    "[At].[DeadbandValue] As [DeadbandValue]  " +
            //    "From ([Tag] As [T] " +
            //    "Inner Join [AnalogTag] As [At] on [T].[Id]=[At].[TagId]);";
                

            //yield return
            //    "Create View DiscreteTagInfo As " +
            //    "Select "+
            //    "[T].[Id] As [Id], [T].[Name] As [Name]," +
            //    "[T].[ArchiveId] As [ArchiveId], [T].[GlobalVariableId] As [GlobalVariableId],"+
            //    "[T].[RefreshSpan] As [RefreshSpan], [T].[Enabled] As [Enabled]," +
            //    "[T].[LastChanged] As [LastChanged],[T].[Comment] As [Comment],"+
            //    "[Dt].[Id] As [DiscreteTagId], [Dt].[EdgeType] As [EdgeType] " +
            //    "From ([Tag] As [T] "+
            //    "Inner Join [DiscreteTag] As [Dt] on [T].[Id]=[Dt].[TagId]); " ;

            // Widoki dla eventow
            yield return
                "Create View AnalogEventInfo As " +
                "Select [E].[Id] As [Id], [E].[Name] As [Name], [E].[RefreshSpan] As [RefreshSpan], " +
                "[E].[Enabled] As [Enabled], [E].[LastChanged] As [LastChanged]," +
                "[E].[Comment] As [Comment], [E].[ActionText] As [ActionText], " +
                "[E].[EventActionType] As [EventActionType],"+
                "[Ae].[GlobalVariableId] As [GlobalVariableId], [Ae].[Id] As [AnalogEventId],"+
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
                "[C].[Id] As [CyclicEventId], [C].[NextEvent] As [NextEvent] "+
                "From ([Event] As [E] " +
                "Inner Join [CyclicEvent] As [C] On [E].[Id]=[C].[EventId]);";

        }


        

       


        public string MapToDbType(Type type)
        {
            if (type == typeof(String))
                return "VARCHAR(50)";
            else if (type == typeof(DateTime))
                return "TIMESTAMP";
            else if (type == typeof(Int16))
                return "SHORT";
            else if (type == typeof(Int32) || type == typeof(UInt16))
                return "INT";
            else if (type == typeof(UInt32))
                return "NUMERIC(11,0)";
            else if (type == typeof(UInt64) || type == typeof(Int64))
                return "NUMERIC(20,0)";
            else if (type == typeof(float))
                return "SINGLE";
            else if (type == typeof(Double))
                return "DOUBLE";
            else if (type == typeof(Boolean))
                return "SHORT";
            else if (type == typeof(Byte) || type == typeof(sbyte))
                return "SHORT";
            else
                throw new ArgumentException("Unsupported data type");
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
                    $"WHERE TagTimestamp>= CDate(datetime,'{from.Year}-{from.Month}-{from.Day} {from.ToShortTimeString()}:{from.Second}') " +
                    $"AND TagTimestamp<= CDate(datetime,'{to.Year}-{to.Month}-{to.Day} {to.ToShortTimeString()}:{to.Second}') " +
                    $"AND TagName='{tag.Name}' ;";

            }
            else
            {
                query = $"SELECT TagTimestamp,{tag.Name} FROM {tag.TagArchive.Name} " +
                        $"WHERE TagTimestamp>= CDate(datetime,'{from.Year}-{from.Month}-{from.Day} {from.ToShortTimeString()}:{from.Second}') " +
                        $"AND TagTimestamp<= CDate(datetime,'{to.Year}-{to.Month}-{to.Day} {to.ToShortTimeString()}:{to.Second}') " +
                        $"AND {tag.Name} IS NOT NULL;";
            }
            return query;
        }

        public string CreateArchiveCmd(TagArchive archive)
        {
            string cmd;
            if (archive.ArchiveType == ArchiveType.Normal)
            {
                cmd = "CREATE TABLE " + archive.Name +
                "(Id Autoincrement Primary Key, TagId Int, TagTimestamp Date, TagValue Double);";
            }
            else
            {
                cmd = "CREATE TABLE " + archive.Name +
                "(Id Autoincrement Primary Key, TagTimestamp Date);";
            }

            return cmd;
        }





        public string InsertTagValueCmd(TagArchive archive, IEnumerable<ITag> tags)
        {
            if (archive.ArchiveType == ArchiveType.Wide)
            {
                string cmd = "Insert Into " + archive.Name + " (TagTimestamp,";
                string cmdEnd = " Values(NOW(),";
                var aTags = archive.Tags.ToList();
                var cmdPar = new List<OleDbParameter>();


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
                    if (tags.Select(it => it.Name).Contains(aTags[i].Name))
                        cmdPar.Add(new OleDbParameter("@" + aTags[i].Name, aTags[i].CurrentValue));
                    else
                        cmdPar.Add(new OleDbParameter("@" + aTags[i].Name, DBNull.Value));
                }
                return cmd;
            }

                return "Insert Into " + archive.Name + " (TagId,TagTimeStamp,TagValue)" +
                    " Values(@TagId,NOW(),@TagValue);";

            
        }

        public string GetSummaryData(ITag tag,string action,DateTime fd)
        {
            if (tag.TagArchive.ArchiveType == ArchiveType.Wide)
            {
                return $"SELECT {action}({tag.Name}) FROM {tag.TagArchive.Name} " +
                    $"WHERE TagTimestamp>Format('{fd}','dd.mm.yyyy hh:mm:ss');";
            }
            return $"SELECT {action}(TagValue) FROM {tag.TagArchive.Name} " +
                $"WHERE TagTimestamp>Format('{fd}','dd.mm.yyyy hh:mm:ss') AND TAgName='{tag.Name}';";
        }


        public string InsertSummaryCmd(IEvent archiveEvent, ITag tag, string action, DateTime fd, object val)
        {
            if (tag.TagArchive.ArchiveType == ArchiveType.Wide)
            {


                return "INSERT INTO EventSummary(EventId,TagName,CalcType,FromDate,ToDate,CalcValue) " +
                    $"Values({archiveEvent.Id},'{tag.Name}','{action}',Format('" +
                    $"{fd}','dd.mm.yyyy hh:mm:ss'),NOW(),{val});";

            }

                return "INSERT INTO EventSummary(EventId,TagName,CalcType,FromDate,ToDate,CalcValue) " +
                $"Values({archiveEvent.Id},'{tag.Name}','{action}',Format('" +
                $"{fd}','dd.mm.yyyy hh:mm:ss'),NOW(),{val});";

       }
        

        public string ChangeNameCmd(string table, string odlName, string newName)
        {
            return null;
        }

        public string ChangeDtCmd(ITag tag, int oldId, int newId)
        {
            return null;
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
                    cmd += keys[i] + "=" + " Format('" + date + "','dd.mm.yyyy hh:mm:ss')";
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
                    cmd += keys[i] + "=" + " Format('" + date + "','dd.mm.yyyy hh:mm:ss')";
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



    }
}
