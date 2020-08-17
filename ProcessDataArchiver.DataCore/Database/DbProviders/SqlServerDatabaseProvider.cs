using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProcessDataArchiver.DataCore.Database.Schema;
using ProcessDataArchiver.DataCore.Database.CommandProviders;
using System.Data.SqlClient;
using ProcessDataArchiver.DataCore.DbEntities.Events;
using ProcessDataArchiver.DataCore.DbEntities.Tags;
using ProcessDataArchiver.DataCore.DbEntities;
using System.Xml.Linq;
using System.Transactions;
using System.Threading;

namespace ProcessDataArchiver.DataCore.Database.DbProviders
{
    public class SqlServerDatabaseProvider : IDatabaseProvider
    {

        public SqlServerDatabaseProvider() { }

        public ICommandProvider CommandProvider { get; set; }
        public string ConnectionString { get; set; }

        public DatabaseType DatabaseType
        {
            get { return DatabaseType.SqlServer; }
        }
        public DatabaseType CmdProviderType
        {
            get { return DatabaseType.SqlServer; }
        }
        public SqlServerDatabaseProvider(string connectionString, ICommandProvider provider)
        {
            CommandProvider = provider;
            ConnectionString = connectionString;
        }

        private SqlConnection GetSqlConnection()
        {
            return new SqlConnection(ConnectionString);
        }



        public async Task CallProcedureAsync(string name, IEnumerable<DbParameter> parameters)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(ConnectionString))
                {
                    await conn.OpenAsync();

                    SqlCommand cmd = conn.CreateCommand();
                    cmd.CommandText = name;
                    cmd.CommandType = CommandType.StoredProcedure;


                    foreach (SqlParameter par in parameters)
                    {
                        if (par.Value != null)
                            cmd.Parameters.Add(par);
                        else
                            cmd.Parameters.Add(new SqlParameter(par.ParameterName, DBNull.Value));
                    }

                    await cmd.ExecuteNonQueryAsync();
                }
            }
            catch (SqlException e)
            {
                throw new ConnectionException(e.Message);
            }

        }

        public async Task CallProcedureAsync(string name, IEnumerable<DbParameter> parameters,CancellationToken token)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(ConnectionString))
                {
                    await conn.OpenAsync(token);

                    SqlCommand cmd = conn.CreateCommand();
                    cmd.CommandText = name;
                    cmd.CommandType = CommandType.StoredProcedure;


                    foreach (SqlParameter par in parameters)
                    {
                        if (par.Value != null)
                            cmd.Parameters.Add(par);
                        else
                            cmd.Parameters.Add(new SqlParameter(par.ParameterName, DBNull.Value));
                    }

                    await cmd.ExecuteNonQueryAsync(token);
                }
            }
            catch (SqlException e)
            {
                throw new ConnectionException(e.Message);
            }
            catch (TaskCanceledException) { }
        }


        public Task CallProcedureAsync(string name, params DbParameter[] parameters)
        {
            return CallProcedureAsync(name, (IEnumerable<DbParameter>)parameters);
        }

        public Task CallProcedureAsync(string name,CancellationToken tok, params DbParameter[] parameters)
        {
            return CallProcedureAsync(name, (IEnumerable<DbParameter>)parameters,tok);
        }

        public DbParameter CreateParameter(string name, object value, Type type)
        {
            SqlParameter parameter = new SqlParameter();
            parameter.ParameterName = name;
            if (value != null)
            {
                if (type == typeof(bool))
                    parameter.Value = value.Equals(true) ? 1 : 0;
                else
                    parameter.Value = value;
            }
            else
                parameter.Value = DBNull.Value;
            parameter.SqlDbType = MapToDbType(type);
            return parameter;
        }

        public SqlParameter CreateParameter(string propName, string name, object ob)
        {
            var props = ob.GetType().GetProperties();

            foreach (var item in props)
            {
                if (item.Name.Equals(propName))
                {
                    object val;
                    if (item.PropertyType.IsEnum)
                        val = (int)item.GetValue(ob);
                    else if (item.PropertyType.Equals(typeof(Type)))
                        val = item.GetValue(ob).ToString();
                    else
                        val = item.GetValue(ob);
                    return new SqlParameter("@" + name, val);
                }
            }
            throw new ArgumentException("Wrong parameter name");
        }


        public async Task<int> ExecuteNonQueryAsync(string command)
        {
            int res = 0;
            try
            {
                using (SqlConnection conn = new SqlConnection(ConnectionString))
                {
                    await conn.OpenAsync();
                    SqlCommand cmd = conn.CreateCommand();
                    cmd.CommandText = command;
                    res =await cmd.ExecuteNonQueryAsync();
                }
            }
            catch (SqlException e)
            {
                throw new ConnectionException(e.Message);
            }
            return res;

        }

        public async Task<int> ExecuteNonQueryAsync(string command,CancellationToken token)
        {
            int res = 0;
            try
            {
                using (SqlConnection conn = new SqlConnection(ConnectionString))
                {
                    await conn.OpenAsync(token);
                    SqlCommand cmd = conn.CreateCommand();
                    cmd.CommandText = command;
                    res = await cmd.ExecuteNonQueryAsync(token);
                }
            }
            catch (SqlException e)
            {
                throw new ConnectionException(e.Message);
            }
            catch (TaskCanceledException) { }
            return res;

        }


        public async Task<DataTable> ExecuteQueryAsync(string command)
        {
            var dt = new DataTable();

            try
            {
                using (SqlConnection conn = new SqlConnection(ConnectionString))
                {
                    await conn.OpenAsync();
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = command;
                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                    adapter.Fill(dt);
                }
            }
            catch (SqlException e)
            {
                throw new ConnectionException(e.Message);
            }
            return dt;
        }

        public async Task<DataTable> ExecuteQueryAsync(string command,CancellationToken token)
        {
            var dt = new DataTable();
            dt.TableNewRow += (s, e) =>
            {
                if (token.IsCancellationRequested)
                    token.ThrowIfCancellationRequested();
            };
            try
            {
                using (SqlConnection conn = new SqlConnection(ConnectionString))
                {
                    await conn.OpenAsync(token);
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = command;
                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                    adapter.Fill(dt);
                }
            }
            catch (SqlException e)
            {
                throw new ConnectionException(e.Message);
            }
            catch (TaskCanceledException) { }
            return dt;
        }


        public async Task<int> FillDataSetAsync(string command,DataTable ds)
        {
            int i = 0;
            try
            {
                using (SqlConnection conn = new SqlConnection(ConnectionString))
                {
                    await conn.OpenAsync();
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = command;

                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);

                    i = adapter.Fill(ds);
                }
            }
            catch (SqlException e)
            {
                throw new ConnectionException(e.Message);
            }
            return i;
        }

        public async Task<int> FillDataSetAsync(string command, DataTable ds,CancellationToken token)
        {
            ds.TableNewRow += (s, e) =>
            {
                if (token.IsCancellationRequested)
                    token.ThrowIfCancellationRequested();
            };
            int i = 0;
            try
            {
                using (SqlConnection conn = new SqlConnection(ConnectionString))
                {
                    await conn.OpenAsync(token);
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = command;
                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);

                    i = adapter.Fill(ds);
                }
            }
            catch (SqlException e)
            {
                throw new ConnectionException(e.Message);
            }
            catch (TaskCanceledException) { }
            return i;
        }

        public async Task<int> FillDataSetAsync(QueryOptions options, DataTable ds)
        {
            string command = CommandProvider.CreateQuery(options);
            int i = 0;
            try
            {
                using (SqlConnection conn = new SqlConnection(ConnectionString))
                {
                    await conn.OpenAsync();
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = command;
                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);

                    i = adapter.Fill(ds);
                }
            }
            catch (SqlException e)
            {
                throw new ConnectionException(e.Message);
            }
            return i;

        }

        public async Task<int> FillDataSetAsync(QueryOptions options, DataTable ds,CancellationToken token)
        {
            ds.TableNewRow += (s, e) =>
            {
                if (token.IsCancellationRequested)
                    token.ThrowIfCancellationRequested();
            };
            string command = CommandProvider.CreateQuery(options);
            int i = 0;
            try
            {
                using (SqlConnection conn = new SqlConnection(ConnectionString))
                {
                    await conn.OpenAsync(token);
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = command;
                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);

                    i = adapter.Fill(ds);
                }
            }
            catch (SqlException e)
            {
                throw new ConnectionException(e.Message);
            }
            catch (TaskCanceledException) { }
            return i;

        }



        public Task<DataTable> ExecuteQueryAsync(QueryOptions opts)
        {
            string cmd = CommandProvider.CreateQuery(opts);
            return ExecuteQueryAsync(cmd);
        }

        public Task<DataTable> ExecuteQueryAsync(QueryOptions opts,CancellationToken token)
        {
            string cmd = CommandProvider.CreateQuery(opts);
            return ExecuteQueryAsync(cmd,token);
        }


        public async Task ExecutePreparedAsync(string query, params DbParameter[] parameters)
        {
            try
            {
                using (var conn = new SqlConnection(ConnectionString))
                {
                    await conn.OpenAsync();

                    SqlCommand cmd = conn.CreateCommand();
                    cmd.CommandText = query;
                    foreach (var par in parameters)
                    {
                        (par as SqlParameter).SqlDbType = MapToDbType(par.Value.GetType());
                        cmd.Parameters.Add(par);
                    }
                    cmd.ExecuteNonQuery();
                }
            }
            catch (SqlException e)
            {
                throw new ConnectionException(e.Message);
            }

        }


        public async Task<bool> TryConnectAsync()
        {
            bool polaczenie = false;
            try
            {
                using (SqlConnection conn = new SqlConnection(ConnectionString))
                {
                    await conn.OpenAsync();
                    polaczenie = true;
                }
            }
            catch (SqlException e)
            {
                throw new ConnectionException(e.Message);
            }
            return polaczenie;
        }

        public async Task<bool> TryConnectAsync(CancellationToken token)
        {
            bool polaczenie = false;
            try
            {
                using (SqlConnection conn = new SqlConnection(ConnectionString))
                {
                    await conn.OpenAsync(token);
                    polaczenie = true;
                }
            }
            catch (SqlException e)
            {
                throw new ConnectionException(e.Message);
            }
            catch (TaskCanceledException) { }
            return polaczenie;
        }


        //private SqlType MapToDbType(Type type)
        //{
        //    if (type == typeof(bool))
        //        return SqlType.SmallInt;
        //    else if (type == typeof(byte) || type == typeof(short) || type == typeof(sbyte))
        //        return SqlType.SmallInt;
        //    else if (type == typeof(ushort))
        //        return SqlType.UnsignedSmallInt;
        //    else if (type == typeof(int))
        //        return SqlType.Integer;
        //    else if (type == typeof(uint))
        //        return SqlType.UnsignedInt;
        //    else if (type == typeof(long))
        //        return SqlType.BigInt;
        //    else if (type == typeof(ulong))
        //        return SqlType.UnsignedBigInt;
        //    else if (type == typeof(float))
        //        return SqlType.Single;
        //    else if (type == typeof(double))
        //        return SqlType.Double;
        //    else if (type == typeof(string))
        //        return SqlType.VarChar;
        //    else if (type == typeof(DateTime))
        //        return SqlType.DBTimeStamp;
        //    else
        //        throw new ArgumentException("Unsupported data type");
        //}

        //private Type MapToType(string type)
        //{
        //    if (type.Contains("varchar"))
        //        return typeof(string);
        //    else if (type.Contains("int"))
        //        return typeof(int);
        //    else if (type == "datetime")
        //        return typeof(DateTime);
        //    else if (type == "float")
        //        return typeof(float);
        //    else if (type == "double")
        //        return typeof(double);
        //    else throw new ArgumentException("Unsupported argument type!");
        //}

        public async Task CreateGlobalVariablesAsync(params GlobalVariable[] gv)
        {
            foreach (var gvar in gv)
            {
                var param = new List<SqlParameter>();
                param.Add(new SqlParameter("@Name", gvar.Name));
                param.Add(new SqlParameter("@Address",(long) gvar.Address));
                param.Add(new SqlParameter("@VariableSize", gvar.Size));
                param.Add(new SqlParameter("@NetType", gvar.NetType.ToString()));
                await CallProcedureAsync("Create_GlobalVariable", param.ToArray());
                gvar.Id = await GetIdAsync(gvar.Name, "GlobalVariable");
            }
        }

        public Task<IEnumerable<GlobalVariable>> GetGlobalVariablesAsync()
        {
            return Task.Run(() =>
            {
                var gvars = new List<GlobalVariable>();
                using (var conn = GetSqlConnection())
                {
                    conn.Open();
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = "Select Id,Name,Address,VariableSize,NetType From GlobalVariable;";

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var gvar = new GlobalVariable();
                            gvar.Id = reader.GetInt32(0);
                            gvar.Name = reader.GetString(1);
                            gvar.Address = (uint)(long)reader.GetInt64(2);
                            gvar.Size = reader.GetInt32(3);
                            gvar.NetType = Type.GetType(reader.GetString(4));

                            gvars.Add(gvar);
                        }
                    }
                }

                return (IEnumerable<GlobalVariable>)gvars;
            });
        }


        public async Task CreateDbSchemaAsync()
        {
            List<string> commands = new List<string>(CommandProvider.CreateDbTables());
            commands.AddRange(CommandProvider.CreateDbProcedures());
            commands.AddRange(CommandProvider.CreateDbViews());

            foreach (string cmd in commands)
            {
                await ExecuteNonQueryAsync(cmd);
            }

        }

        public async Task CreateArchivesAsync(params TagArchive[] archives)
        {

            foreach (var archive in archives)
            {
                await CallProcedureAsync("Create_Archive",
                    new SqlParameter("@ArchiveType", (int)archive.ArchiveType),
                     new SqlParameter("@Name", archive.Name));

                string cmd = (CommandProvider as IExtendedCmdProvider).
                    CreateArchiveCmd(archive).First();

                await ExecuteNonQueryAsync(cmd);
                archive.Id = await GetIdAsync(archive.Name, "Archive");
            }

        }

        public async Task DeleteArchivesAsync(params TagArchive[] archives)
        {
            foreach (var archive in archives)
            {
                await ExecutePreparedAsync("Delete From Archive Where Id=@Id",
                    new SqlParameter("@Id", archive.Id));
                await ExecuteNonQueryAsync("Drop Table " + archive.Name + ";");
            }
        }

        public async Task DeleteGlobalVariablesAsync(params GlobalVariable[] gvs)
        {
            foreach (var gv in gvs)
            {
                await ExecutePreparedAsync("Delete From GlobalVariable Where Id=@Id;"
                    , new SqlParameter("@Id", gv.Id));
            }
        }

        public Task<IEnumerable<TagArchive>> GetArchivesAsync()
        {
            return Task.Run(() =>
            {
                List<TagArchive> archives = new List<TagArchive>();
                using (var conn = GetSqlConnection())
                {
                    conn.Open();
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = "Select Id,Name,ArchiveType From Archive;";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var archive = new TagArchive();
                            archive.Id = reader.GetInt32(0);
                            archive.Name = reader.GetString(1);
                            archive.ArchiveType = (ArchiveType)reader.GetInt32(2);
                            archives.Add(archive);
                        }
                    }
                }
                return (IEnumerable<TagArchive>)archives;
            });
        }


        //sprawdz async
        public async Task CreateTagsAsync(params ITag[] tags)
        {
            foreach (var tag in tags)
            {
                List<SqlParameter> pars = new List<SqlParameter>();

                pars.Add(new SqlParameter("@ArchiveId", tag.TagArchiveId));
                pars.Add(new SqlParameter("@GlobalVariableId", tag.GlobalVariableId));
                pars.Add(new SqlParameter("@TagType", (int)tag.TagType));
                pars.Add(new SqlParameter("@Name", tag.Name));
                pars.Add(new SqlParameter("@RefreshSpan", tag.RefreshSpan.TotalMilliseconds));
                pars.Add(new SqlParameter("@ArchivingType", (int)tag.ArchivingType));


                if (tag is AnalogTag)
                {
                    var atag = tag as AnalogTag;
                    pars.Add(new SqlParameter("@EuName", atag.EuName));
                    pars.Add(new SqlParameter("@DeadbandType", (int)atag.DeadbandType));
                    pars.Add(new SqlParameter("@DeadbandValue", atag.DeadbandValue));
                }
                else
                {
                    pars.Add(new SqlParameter("@EuName", DBNull.Value));
                    pars.Add(new SqlParameter("@DeadbandType", DBNull.Value));
                    pars.Add(new SqlParameter("@DeadbandValue", DBNull.Value));
                }

                pars.Add(new SqlParameter("@LastChanged", tag.LastChanged));

                if (tag.Comment != null)
                    pars.Add(new SqlParameter("@Comment", tag.Comment));
                else
                    pars.Add(new SqlParameter("@Comment", DBNull.Value));

                await CallProcedureAsync("Create_Tag", pars);

                tag.Id = await GetIdAsync(tag.Name, "Tag");
            }
        }

        public async Task DeleteTagsAsync(params ITag[] tags)
        {

            foreach (var tag in tags)
            {
                //  await CallProcedureAsync("Delete_Tag", new SqlParameter("@Id", tag.Id));
                await ExecutePreparedAsync("Delete From Tag Where Id=@Id",
                     new SqlParameter("@Id", tag.Id));
            }

        }





        public Task<IEnumerable<ITag>> GetTagsAsync()
        {
            return Task.Run(() =>
            {
                List<ITag> tags = new List<ITag>();
                using (var conn = GetSqlConnection())
                {
                    conn.Open();

                    var dcmd = conn.CreateCommand();
                    dcmd.CommandText = "Select Id,ArchiveId,GlobalVariableId,Name,RefreshSpan," +
                    $"ArchivingType,LastChanged,Comment From Tag Where TagType={(int)TagType.Discrete};";
                    using (var reader = dcmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var discreteTag = new DiscreteTag();
                            discreteTag.Id = (int)reader["Id"];
                            discreteTag.GlobalVariableId = (int)reader["GlobalVariableId"];
                            discreteTag.TagArchiveId = (int)reader["ArchiveId"];
                            discreteTag.Name = reader["Name"].ToString();
                            discreteTag.LastChanged = (DateTime)reader["LastChanged"];
                            discreteTag.ArchivingType = (ArchivingType)reader["ArchivingType"];
                            discreteTag.Comment = reader["Comment"].ToString();
                            discreteTag.RefreshSpan = new TimeSpan(0, 0, 0, 0, (int)reader["RefreshSpan"]);

                            tags.Add(discreteTag);

                        }
                    }

                    var acmd = conn.CreateCommand();
                    acmd.CommandText = "Select Id,ArchiveId,GlobalVariableId,Name,RefreshSpan," +
                    "DeadbandType,DeadbandValue,EuName," +
                    $"ArchivingType,LastChanged,Comment From Tag Where TagType={(int)TagType.Analog};";
                    using (var reader = acmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {


                            var analogTag = new AnalogTag();
                            analogTag.Id = (int)reader["Id"];
                            analogTag.GlobalVariableId = (int)reader["GlobalVariableId"];
                            analogTag.TagArchiveId = (int)reader["ArchiveId"];
                            analogTag.EuName = reader["EuName"].ToString();
                            analogTag.Name = reader["Name"].ToString();
                            analogTag.LastChanged = (DateTime)reader["LastChanged"];
                            analogTag.ArchivingType = (ArchivingType)reader["ArchivingType"];
                            analogTag.Comment = reader["Comment"].ToString();
                            analogTag.DeadbandType = (DeadbandType)reader["DeadbandType"];
                            analogTag.DeadbandValue = (double)reader["DeadbandValue"];
                            analogTag.RefreshSpan = new TimeSpan(0, 0, 0, 0, (int)reader["RefreshSpan"]);

                            tags.Add(analogTag);

                        }
                    }

                }
                return (IEnumerable<ITag>)tags;
            });

        }

        public async Task CreateEventsAsync(params IEvent[] archiveEvents)
        {
            foreach (var archiveEvent in archiveEvents)
            {

                List<SqlParameter> param = new List<SqlParameter>();
                param.Add(new SqlParameter("@EventType", archiveEvent.EventType));
                param.Add(new SqlParameter("@EventActionType", archiveEvent.EventActionType));
                param.Add(new SqlParameter("@Name", archiveEvent.Name));
                param.Add(new SqlParameter("@RefreshSpan", archiveEvent.RefreshSpan.TotalMilliseconds));
                param.Add(new SqlParameter("@ActionText", archiveEvent.ActionText));
                param.Add(new SqlParameter("@Enabled", archiveEvent.Enabled ? 1 : 0));
                param.Add(new SqlParameter("@LastChanged", archiveEvent.LastChanged));
                param.Add(new SqlParameter("@Comment", archiveEvent.Comment));

                await CallProcedureAsync("Create_Event", param);
                archiveEvent.Id = await GetIdAsync(archiveEvent.Name, "Event");

                if (archiveEvent is DiscreteEvent)
                {
                    var dEvent = archiveEvent as DiscreteEvent;
                    List<SqlParameter> dParam = new List<SqlParameter>();
                    dParam.Add(new SqlParameter("@GlobalVariableId", dEvent.GlobalVariableId));
                    dParam.Add(new SqlParameter("@EventId", dEvent.Id));
                    dParam.Add(new SqlParameter("@EdgeType", dEvent.EdgeType));
                    await CallProcedureAsync("Create_DiscreteEvent", dParam);

                    dEvent.DiscreteEventId = await GetEventIdAsync(archiveEvent.Name, "DiscreteEventInfo",
                        "DiscreteEventId");

                }
                else if (archiveEvent is AnalogEvent)
                {
                    var aEvent = archiveEvent as AnalogEvent;
                    var aParam = new List<SqlParameter>();
                    aParam.Add(new SqlParameter("@GlobalVariableId", aEvent.GlobalVariableId));
                    aParam.Add(new SqlParameter("@EventId", aEvent.Id));
                    aParam.Add(new SqlParameter("@EventTriggerType", aEvent.EventTriggerType));
                    aParam.Add(new SqlParameter("@TriggerValue", aEvent.TriggerValue));
                    await CallProcedureAsync("Create_AnalogEvent", aParam);

                    aEvent.AnalogEventId = await GetEventIdAsync(archiveEvent.Name, "AnalogEventInfo", "AnalogEventId");

                }
                else if (archiveEvent is CyclicEvent)
                {
                    var cEvent = archiveEvent as CyclicEvent;
                    var cParam = new List<SqlParameter>();
                    cParam.Add(new SqlParameter("@EventId", cEvent.Id));
                    cParam.Add(new SqlParameter("@EventCycleType", cEvent.EventCycleType));
                    //cParam.Add(new SqlParameter("@Years", cEvent.CycleStamp.Year));
                    //cParam.Add(new SqlParameter("@Months", cEvent.CycleStamp.Month));
                    cParam.Add(new SqlParameter("@Days", cEvent.CycleStamp.Days));
                    cParam.Add(new SqlParameter("@Hours", cEvent.CycleStamp.Hours));
                    cParam.Add(new SqlParameter("@Minutes", cEvent.CycleStamp.Minutes));
                    cParam.Add(new SqlParameter("@Seconds", cEvent.CycleStamp.Seconds));
                    cParam.Add(new SqlParameter("@NextEvent", cEvent.NextEvent));
                    await CallProcedureAsync("Create_CyclicEvent", cParam);

                    cEvent.CyclicEventId = await GetEventIdAsync(archiveEvent.Name, "CyclicEventInfo", "CyclicEventId");

                }
                
            }
        }

        public async Task DeleteEventsAsync(params IEvent[] events)
        {
            foreach (var ev in events)
            {
                await ExecutePreparedAsync("Delete From Event Where Id=@Id;",
                    new SqlParameter("@Id", ev.Id));
            }
        }



        public Task<IEnumerable<IEvent>> GetEventsAsync()
        {
            return Task.Run(() =>
            {
                List<IEvent> events = new List<IEvent>();

                using (var conn = GetSqlConnection())
                {
                    conn.Open();
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = "Select * From DiscreteEventInfo;";

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var ev = new DiscreteEvent();
                            short enb = (short)reader["Enabled"];
                            ev.Id = (int)reader["Id"];
                            ev.EventActionType = (EventActionType)reader["EventActionType"];
                            ev.DiscreteEventId = (int)reader["DiscreteEventId"];
                            ev.Enabled = enb > 0 ? true : false;
                            ev.LastChanged = (DateTime)reader["LastChanged"];
                            ev.Name = reader["Name"].ToString();
                            ev.GlobalVariableId = (int)reader["GlobalVariableId"];
                            ev.ActionText = reader["ActionText"].ToString();
                            ev.Comment = reader["Comment"].ToString();
                            ev.EdgeType = (EdgeType)reader["EdgeType"];
                            ev.RefreshSpan = new TimeSpan(0, 0, 0, 0, (int)reader["RefreshSpan"]);
                            events.Add(ev);
                        }
                    }
                }

                using (var conn = GetSqlConnection())
                {
                    conn.Open();
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = "Select * From AnalogEventInfo;";

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var ev = new AnalogEvent();
                            short enb = (short)reader["Enabled"];
                            ev.Id = (int)reader["Id"];
                            ev.EventActionType = (EventActionType)reader["EventActionType"];
                            ev.AnalogEventId = (int)reader["AnalogEventId"];
                            ev.Enabled = enb > 0 ? true : false;
                            ev.LastChanged = (DateTime)reader["LastChanged"];
                            ev.Name = reader["Name"].ToString();
                            ev.GlobalVariableId = (int)reader["GlobalVariableId"];
                            ev.TriggerValue = (double)reader["TriggerValue"];
                            ev.ActionText = reader["ActionText"].ToString();
                            ev.Comment = reader["Comment"].ToString();
                            ev.EventTriggerType = (EventTriggerType)reader["EventTriggerType"];
                            ev.RefreshSpan = new TimeSpan(0, 0, 0, 0, (int)reader["RefreshSpan"]);
                            events.Add(ev);
                        }
                    }
                }
                using (var conn = GetSqlConnection())
                {
                    conn.Open();
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = "Select * From CyclicEventInfo;";

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var ev = new CyclicEvent();
                            short enb = (short)reader["Enabled"];
                            ev.Id = (int)reader["Id"];
                            ev.EventCycleType = (EventCycleType)reader["EventCycleType"];
                            ev.EventActionType = (EventActionType)reader["EventActionType"];
                            ev.CyclicEventId = (int)reader["CyclicEventId"];
                            ev.Enabled = enb > 0 ? true : false;
                            ev.LastChanged = (DateTime)reader["LastChanged"];
                            ev.Name = reader["Name"].ToString();
                            ev.ActionText = reader["ActionText"].ToString();
                            //int year = (int)reader["Years"];
                            //int month = (int)reader["Months"];
                            int day = (int)reader["Days"];
                            int hour = (int)reader["Hours"];
                            int minute = (int)reader["Minutes"];
                            int second = (int)reader["Seconds"];
                            ev.Comment = reader["Comment"].ToString();
                            ev.CycleStamp = new TimeSpan(day, hour, minute, second);
                            ev.NextEvent = (DateTime)reader["NextEvent"];
                            ev.RefreshSpan = new TimeSpan(0, 0, 0, 0, (int)reader["RefreshSpan"]);
                            events.Add(ev);
                        }
                    }
                }

                foreach (var ev in events)
                {
                    ev.ActionToInvoke = EventActionFactory.CreateAction(ev);
                }

                return (IEnumerable<IEvent>)events;
            });
        }



        public async Task InsertTagsValueAsync(params ITag[] tags)
        {

            var groups = from t in tags
                         group t by t.TagArchive;

            foreach (var item in groups)
            {
                var archive = item.Key;
                if (archive.ArchiveType == ArchiveType.Wide)
                {
                    string cmd = "Insert Into " + archive.Name + " (TagTimestamp,";
                    string cmdEnd = " Values(GetDate(),";
                    var aTags = archive.Tags.ToList();
                    var cmdPar = new List<SqlParameter>();


                    for (int i = 0; i < aTags.Count; i++)
                    {
                        if (i < (aTags.Count - 1))
                        {
                            cmd += aTags[i].Name + ",";
                            cmdEnd += "@" + aTags[i].Name + ",";
                            var tagsToSave = item.ToList();
                        }
                        else
                        {
                            cmd += aTags[i].Name + ") ";
                            cmdEnd += "@" + aTags[i].Name + ");";
                        }
                        if (item.Select(it => it.Name).Contains(aTags[i].Name))
                            cmdPar.Add(new SqlParameter("@" + aTags[i].Name, aTags[i].CurrentValue)
                            { SqlDbType = MapToDbType(aTags[i].GlobalVariable.NetType)
                            });
                        else
                            cmdPar.Add(new SqlParameter("@" + aTags[i].Name, DBNull.Value));
                    }
                    await ExecutePreparedAsync(cmd + cmdEnd, cmdPar.ToArray());
                }
                else
                {
                    string cmd = "Insert Into " + archive.Name + " (TagId,TagTimeStamp,TagValue)" +
                        " Values(@TagId,GetDate(),@TagValue);";
                    foreach (var tag in item)
                    {
                        var tagId = new SqlParameter("@TagId", tag.Id);
                        if (tag is DiscreteTag)
                        {
                            var tagValue = new SqlParameter("@TagValue", (bool)tag.CurrentValue ? 1 : 0);
                            await ExecutePreparedAsync(cmd, tagId, tagValue);
                        }
                        else
                        {
                            var tagValue = new SqlParameter("@TagValue", tag.CurrentValue);
                            await ExecutePreparedAsync(cmd, tagId, tagValue);
                        }
                    }
                }


            }
        }



        public async Task InsertEventsValueAsync(params IEvent[] archiveEvents)
        {
            foreach (var ae in archiveEvents)
            {
                var euid = new SqlParameter("@EventId", ae.Id);
                await CallProcedureAsync("Insert_EventHistory", euid);
            }
        }

        public async Task InsertEventSnapshotAsync(IEvent archiveEvent,params GlobalVariable[] gvs)
        {
            foreach (var gv in gvs)
            {
                var evid = new SqlParameter("@EventId", archiveEvent.Id);
                var gvid = new SqlParameter("@GlobalVariableId", gv.Id);
                var val = new SqlParameter("@VariableValue", gv.CurrentValue);
                val.SqlDbType = MapToDbType(gv.CurrentValue.GetType());
                await CallProcedureAsync("Insert_EventSnapshot", evid, gvid, val);
            }
        }
        public async Task InsertEventSnapshotAsync(IEvent archiveEvent, params ITag[] tags)
        {
            foreach (var tag in tags)
            {
                var evid = new SqlParameter("@EventId", archiveEvent.Id);
                var gvid = new SqlParameter("@TagId", tag.Id);
                var val = new SqlParameter("@Value", tag.CurrentValue);
                val.SqlDbType = MapToDbType(tag.CurrentValue.GetType());
                await CallProcedureAsync("Insert_EventSnapshot", evid, gvid, val);
            }
        }
        public async Task InsertSummaryAsync(IEvent archiveEvent)
        { 
            var doc = XDocument.Parse(archiveEvent.ActionText);
            var root = doc.Descendants("summary").FirstOrDefault();
            var tags = root.Descendants("tag");

            foreach (var item in tags)
            {
                string tagName = item.Descendants("name").FirstOrDefault().Value;
                string archive = item.Descendants("archivename").FirstOrDefault().Value;
                string action = item.Descendants("action").FirstOrDefault().Value;
                string timespan = item.Descendants("timespan").FirstOrDefault().Value;
                int minutes = int.Parse(timespan);

                TimeSpan ts = new TimeSpan(0, minutes, 0);

                var fd = DateTime.Now - ts;

                var tag = EntityContext.GetContext().Tags.Where(t => t.Name.Equals(tagName) &&
                t.TagArchive!=null && t.TagArchive.Name.Equals(archive)).FirstOrDefault();
                if (tag.TagArchive.ArchiveType == ArchiveType.Wide)
                {
                    string cmd = "INSERT INTO EventSummary(EventId,TagName,CalcType,FromDate,ToDate,CalcValue) " +
                        $"Values({archiveEvent.Id},'{tagName}','{action}'," +
                        $"Convert(Datetime,'{fd.Year}-{fd.Month}-{fd.Day} {fd.Hour}:{fd.Minute}:{fd.Second}'),"+
                        "GetDate()," +
                        $"(SELECT {action}({tagName}) FROM {tag.TagArchive.Name} " +
                        $"WHERE TagTimestamp>Convert(Datetime,'{fd.Year}-{fd.Month}-{fd.Day} {fd.Hour}:{fd.Minute}:{fd.Second}')));";
                    await ExecuteNonQueryAsync(cmd);
                }
                else
                {
                    string cmd = "INSERT INTO EventSummary(EventId,TagName,CalcType,FromDate,ToDate,CalcValue) " +
                    $"Values({archiveEvent.Id},'{tagName}','{action}'," +
                    $"Convert(Datetime,'{fd.Year}-{fd.Month}-{fd.Day} {fd.Hour}:{fd.Minute}:{fd.Second}'),"+
                    "GetDate()," +
                    $"(SELECT {action}(TagValue) FROM {tag.TagArchive.Name} " +
                    $"WHERE TagTimestamp>Convert(Datetime,'{fd.Year}-{fd.Month}-{fd.Day} {fd.Hour}:{fd.Minute}:{fd.Second}'))" +
                    $" AND TagName='{tag.Name}');";
                    await ExecuteNonQueryAsync(cmd);
                }
            }
        }


        private Task<int> GetIdAsync(string name, string table)
        {
            return Task.Run(() =>
            {
                string txt = $"SELECT ID FROM {table} WHERE NAME='{name}';";
                int id;

                using (var conn = GetSqlConnection())
                {
                    conn.Open();
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = txt;
                    using (var reader = cmd.ExecuteReader())
                    {
                        reader.Read();
                        id = reader.GetInt32(0);
                    }
                }
                return id;
            });
        }
        private Task<int> GetEventIdAsync(string name, string table, string secondId)
        {
            return Task.Run(() =>
            {
                string txt = "SELECT " + secondId + $" FROM {table} WHERE NAME='{name}';";
                int id;

                using (var conn = GetSqlConnection())
                {
                    conn.Open();
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = txt;
                    using (var reader = cmd.ExecuteReader())
                    {
                        reader.Read();
                        id = reader.GetInt32(0);
                    }
                }
                return id;
            });
        }


        private Task<int> GetTagIdAsync(int id, string table)
        {
            return Task.Run(() =>
            {
                string txt = "SELECT ID FROM " + table + " WHERE TagId=" + id + ";";
                using (var conn = GetSqlConnection())
                {
                    conn.Open();
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = txt;
                    using (var reader = cmd.ExecuteReader())
                    {
                        reader.Read();
                        id = reader.GetInt32(0);
                    }
                }
                return id;
            });
        }

        private Task<int> GetEventIdAsync(int id, string table)
        {
            return Task.Run(() =>
            {
                string txt = "SELECT ID FROM " + table + " WHERE EventId=" + id + ";";
                using (var conn = GetSqlConnection())
                {
                    conn.Open();
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = txt;
                    using (var reader = cmd.ExecuteReader())
                    {
                        reader.Read();
                        id = reader.GetInt32(0);
                    }
                }
                return id;
            });
        }



        public async Task UpdateEntitiesAsync(params EntityChangesInfo[] changes)
        {
            var gvsChanges = changes.Where(c => c.Entity is GlobalVariable &&
                c.State == EntityState.Modified);
            var gvsToAdd = changes.Where(c => c.Entity is GlobalVariable &&
            c.State == EntityState.Added).Select(c => c.Entity as GlobalVariable);
            var gvsToRemvoe = changes.Where(c => c.Entity is GlobalVariable &&
            c.State == EntityState.Removed).Select(c => c.Entity as GlobalVariable);

            await UpdateGlobalVariablesAsync(gvsChanges);
            await CreateGlobalVariablesAsync(gvsToAdd.ToArray());
            await DeleteGlobalVariablesAsync(gvsToRemvoe.ToArray());

            //var archivesChanges = changes.Where(c => c.Entity is TagArchive &&
            //c.State == EntityState.Modified);
            var archivesToAdd = changes.Where(c => c.Entity is TagArchive &&
            c.State == EntityState.Added).Select(c => c.Entity as TagArchive);
            var archivesToRemove = changes.Where(c => c.Entity is TagArchive &&
            c.State == EntityState.Removed).Select(c => c.Entity as TagArchive);

            //await UpdateArchivesAsync(archivesChanges);
            await DeleteArchivesAsync(archivesToRemove.ToArray());
            await CreateArchivesAsync(archivesToAdd.ToArray());
            

            var tagsChanges = changes.Where(c => c.Entity is ITag &&
            c.State == EntityState.Modified);
            var tagsToAdd = changes.Where(c => c.Entity is ITag &&
            c.State == EntityState.Added).Select(c => c.Entity as ITag);
            var tagsToRemove = changes.Where(c => c.Entity is ITag &&
            c.State == EntityState.Removed).Select(c => c.Entity as ITag);

            await UpdateTagsAsync(tagsChanges);
            await DeleteTagsAsync(tagsToRemove.ToArray());
            await CreateTagsAsync(tagsToAdd.ToArray());
            

            var eventsChanges = changes.Where(c => c.Entity is IEvent &&
            c.State == EntityState.Modified);
            var eventsToAdd = changes.Where(c => c.Entity is IEvent &&
            c.State == EntityState.Added).Select(c => c.Entity as IEvent);
            var eventsToRemove = changes.Where(c => c.Entity is IEvent &&
            c.State == EntityState.Removed).Select(c => c.Entity as IEvent);

            await UpdateEventsAsync(eventsChanges);
            await DeleteEventsAsync(eventsToRemove.ToArray());
            await CreateEventsAsync(eventsToAdd.ToArray());
            

            var wideArchives = await GetWideArchives();
            foreach (var archive in wideArchives)
            {
                var tags = tagsToRemove.Where(t => t.TagArchiveId == archive.Key);
                await RemoveArchvieTagsAsync(archive.Value, tags);
            }
            foreach (var archive in wideArchives)
            {
                var tags = tagsToAdd.Where(t => t.TagArchiveId == archive.Key);
                await AddArchvieTagsAsync(archive.Value, tags);
            }




            var changedNames = changes.Where(c => c.Entity is ITag && c.Changes!=null &&
            c.Changes.Keys.Contains("Name")
               && c.Original!=null) ;
            foreach (var item in changedNames)
            {
                var tag = item.Entity as ITag;
                await ChangeNamesAsync(tag.TagArchive.Name, item.Original["Name"].ToString(),
                    item.Changes["Name"].ToString());
            }

            var colTypeChanged = changes.Where(c => c.Entity is ITag && c.Changes!=null &&
            c.Changes.Keys.Contains("GlobalVariableId")&& c.Original != null);

            foreach (var item in colTypeChanged)
            {
                var t = item.Entity as ITag;
                await ChangeDataType(t, (int)item.Original["GlobalVariableId"],
                    (int)item.Changes["GlobalVariableId"]);
            }
            
        }

        private async Task ChangeNamesAsync(string table, string oldName, string newName)
        {
            string query = $"sp_rename '{table}.{oldName}', '{newName}', 'COLUMN';";
            await ExecuteNonQueryAsync(query);
        }

        private async Task ChangeDataType(ITag tag, int oldId, int newId)
        {
            string archive = tag.TagArchive.Name;
            string colName = tag.Name;

            var gv = EntityContext.GetContext().GlobalVariables;
            var oldGv= gv.Where(g => g.Id.Equals(oldId)).FirstOrDefault();
            var newGv = gv.Where(g => g.Id.Equals(newId)).FirstOrDefault();

            var oldType = oldGv.NetType;
            var newType = newGv.NetType;

            var eqType = DataTypeComparer.GetEquivalentType(oldType, newType);

            if (!oldType.Equals(eqType))
            {
                string typeToChange = CommandProvider.MapToDbType(eqType);
                string cmd = "ALTER TABLE " + archive + " ALTER COLUMN " + colName + " " + typeToChange + ";";
                await ExecuteNonQueryAsync(cmd);
            }

        }

        private async Task UpdateTagsAsync(IEnumerable<EntityChangesInfo> tagsChanges)
        {
            foreach (var tagChanges in tagsChanges)
            {
                string upTags = "UPDATE TAG ";
                string tagCmdText = GetUpdateCmd(tagChanges.Changes, typeof(ITag));
                if (tagCmdText != null)
                {
                    upTags += tagCmdText + " WHERE Id=" + (tagChanges.Entity as ITag).Id + ";";
                    await ExecuteNonQueryAsync(upTags);
                }

                string upATags = "UPDATE TAG ";
                string aTagCmdText = GetUpdateCmd(tagChanges.Changes, typeof(ITag), typeof(AnalogTag));
                if (aTagCmdText != null)
                {
                    upATags += aTagCmdText + " WHERE Id=" + (tagChanges.Entity as AnalogTag).Id + ";";
                    await ExecuteNonQueryAsync(upATags);
                }

                string upDTags = "UPDATE TAG ";
                string dTagCmdText = GetUpdateCmd(tagChanges.Changes, typeof(ITag), typeof(DiscreteTag));
                if (dTagCmdText != null)
                {
                    upDTags += dTagCmdText + " WHERE Id=" + (tagChanges.Entity as DiscreteTag).Id + ";";
                    await ExecuteNonQueryAsync(upDTags);
                }

            }
            var grouped = tagsChanges.GroupBy(t => t.Entity);
            foreach (var item in grouped)
            {
                string upd = "UPDATE Tag SET LastChanged = "+
                $"{ConvertDateTime((item.Key as ITag).LastChanged)}" +
                    "WHERE Id=" +
                    + (item.Key as ITag).Id + ";";
            }

        }

        private async Task UpdateEventsAsync(IEnumerable<EntityChangesInfo> eventsChanges)
        {
            foreach (var eventChanges in eventsChanges)
            {
                bool choosen = false;
                string upEvents = "UPDATE Event ";
                string evCmdText = GetUpdateCmd(eventChanges.Changes, typeof(IEvent));
                if (evCmdText != null)
                {
                    upEvents += evCmdText + " WHERE Id=" + (eventChanges.Entity as IEvent).Id + ";";
                    await ExecuteNonQueryAsync(upEvents);
                }

                string upAEvents = "UPDATE AnalogEvent ";
                string aEvCmdText = GetUpdateCmd(eventChanges.Changes, typeof(IEvent),
                    typeof(AnalogEvent));
                if (aEvCmdText != null && !choosen)
                {
                    choosen = true;
                    upAEvents += aEvCmdText + " WHERE Id=" + (eventChanges.Entity as AnalogEvent)
                    .AnalogEventId + ";";
                    await ExecuteNonQueryAsync(upAEvents);
                }

                string upDEvents = "UPDATE DiscreteEvent ";
                string dEvCmdText = GetUpdateCmd(eventChanges.Changes, typeof(IEvent),
                    typeof(DiscreteEvent));
                if (dEvCmdText != null && !choosen)
                {
                    choosen = true;
                    upDEvents += dEvCmdText + " WHERE Id=" + (eventChanges.Entity as DiscreteEvent)
                    .DiscreteEventId + ";";
                    await ExecuteNonQueryAsync(upDEvents);
                }

                string upCEvents = "UPDATE CyclicEvent ";
                string cEvCmdText = GetUpdateCmd(eventChanges.Changes, typeof(IEvent),
                    typeof(CyclicEvent));
                if (cEvCmdText != null && !choosen)
                {
                    upCEvents += cEvCmdText + " WHERE Id=" + (eventChanges.Entity as CyclicEvent)
                    .CyclicEventId + ";";
                    await ExecuteNonQueryAsync(upCEvents);
                }

            }
            var grouped = eventsChanges.GroupBy(t => t.Entity);
            foreach (var item in grouped)
            {
                string upd = "UPDATE Event SET LastChanged = "+
                $"{ConvertDateTime((item.Key as IEvent).LastChanged)}"+
                    "WHERE Id="+
                    + (item.Key as IEvent).Id + ";";
            }

        }

        private string ConvertDateTime(DateTime dt)
        {
            return $" Convert(datetime,'{dt.Year}-{dt.Month}-{dt.Day} {dt.Hour}:{dt.Minute}:{dt.Second}') ";
        }



        private async Task UpdateGlobalVariablesAsync(IEnumerable<EntityChangesInfo> gvsChanges)
        {
            foreach (var euChanges in gvsChanges)
            {
                string upEus = "UPDATE GlobalVariable ";
                string euCmdText = GetUpdateCmd(euChanges.Changes, typeof(GlobalVariable));
                if (euCmdText != null)
                {
                    upEus += euCmdText + " WHERE Id=" + (euChanges.Entity as GlobalVariable).Id + ";";
                    await ExecuteNonQueryAsync(upEus);
                }
            }
        }


        private async Task UpdateArchivesAsync(IEnumerable<EntityChangesInfo> archviesChanges)
        {
            foreach (var archiveChanges in archviesChanges)
            {
                string upAr = "UPDATE Archive ";
                string arCmdText = GetUpdateCmd(archiveChanges.Changes, typeof(TagArchive));
                if (arCmdText != null)
                {
                    upAr += arCmdText + " WHERE Id=" + (archiveChanges.Entity as TagArchive).Id + ";";
                    await ExecuteNonQueryAsync(upAr);
                }
            }

        }

        private async Task AddArchvieTagsAsync(string archiveName, IEnumerable<ITag> tags)
        {
            foreach (var tag in tags)
            {
                string cmd = "ALTER TABLE " + archiveName + " ";
                cmd += "ADD " + tag.Name + " " + CommandProvider.MapToDbType(tag.GlobalVariable.NetType) + ";";
                await ExecuteNonQueryAsync(cmd);
            }
        }
        private async Task RemoveArchvieTagsAsync(string archiveName, IEnumerable<ITag> tags)
        {
            foreach (var tag in tags)
            {
                string cmd = "ALTER TABLE " + archiveName + " ";
                cmd += "DROP COLUMN " + tag.Name + ";";
                await ExecuteNonQueryAsync(cmd);
            }

        }

        private string GetUpdateCmd(IDictionary<string, object> values, Type type)
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
                    cmd+= keys[i] + "=" +span;
                }
                else if (values[keys[i]] is Enum)
                {
                    int val = (int)values[keys[i]];
                    cmd += keys[i] + "=" + val;
                }
                else if (values[keys[i]].GetType().Equals(typeof(bool)))
                {
                    int val = (bool)values[keys[i]] == true?1:0;
                    cmd += keys[i] + "=" + val;
                }
                else
                    cmd += keys[i] + "=" + values[keys[i]];
                if (i < (keys.Count - 1))
                    cmd += ", ";
            }
            return cmd;
        }

        private string GetUpdateCmd(IDictionary<string, object> values, Type baseType, Type derrivedType)
        {
            bool cycleStamp = false;
            var basePropNames = baseType.GetProperties().Select(p => p.Name);
            var derPropNames = derrivedType.GetProperties()
                .Select(p => p.Name).Where(n => !basePropNames.Contains(n));
            var keys = values.Keys.Where(k => derPropNames.Contains(k)).ToList();

            if (keys == null || keys.Count == 0)
                return null;

            string cmd = "SET ";
            for (int i = 0; i < keys.Count; i++)
            {
                if (keys[i] == "CycleStamp")
                {
                    var date = (TimeSpan)values[keys[i]];
                    cmd += $"Days={date.Days}, Hours={date.Hours}, Minutes={date.Minutes}, Seconds={date.Seconds}";
                }
                else if (values[keys[i]].GetType().Equals(typeof(string)))
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

        private Task<Dictionary<int, string>> GetWideArchives()
        {
            return Task.Run(() =>
            {
                var dict = new Dictionary<int, string>();
                using (var conn = GetSqlConnection())
                {
                    conn.Open();
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT ID,NAME FROM ARCHIVE WHERE ARCHIVETYPE="
                    + (int)ArchiveType.Wide + ";";

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            dict.Add(reader.GetInt32(0), reader.GetString(1));
                        }
                    }
                }
                return dict;
            });
        }

        private SqlDbType MapToDbType(Type type)
        {
            if (type == typeof(bool))
                return SqlDbType.Bit;
            else if (type == typeof(byte))
                return SqlDbType.TinyInt;
            else if (type == typeof(short) || type == typeof(sbyte))
                return SqlDbType.SmallInt;
            else if (type == typeof(ushort) || type == typeof(int))
                return SqlDbType.Int;
            else if (type == typeof(uint) || type == typeof(long))
                return SqlDbType.BigInt;
            else if (type == typeof(ulong))
                return SqlDbType.Decimal;
            else if (type == typeof(float))
                return SqlDbType.Real;
            else if (type == typeof(double))
                return SqlDbType.Float;
            else if (type == typeof(string))
                return SqlDbType.NVarChar;
            else if (type == typeof(DateTime))
                return SqlDbType.DateTime;
            else
                throw new ArgumentException("Unsupported data type");
        }

        private Type MapToType(string type)
        {
            switch (type)
            {
                case "nvarchar":
                    return typeof(string);
                case "datetime":
                    return typeof(DateTime);
                case "int":
                    return typeof(int);
                case "real":
                    return typeof(double);
                default:
                    throw new ArgumentException("Unsupported data type!");
            }
        }

        public Task<IEnumerable<string>> GetTableNamesAsync()
        {
            return Task.Run(() =>
            {
                string[] tableNames = null;

                try
                {
                    using (SqlConnection conn = new SqlConnection(ConnectionString))
                    {
                        conn.Open();
                        DataTable dt = conn.GetSchema("Tables");
                        tableNames = new string[dt.Rows.Count];


                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            DataRow dr = dt.Rows[i];
                            tableNames[i] = dr["TABLE_NAME"].ToString();
                        }

                    }
                }
                catch (SqlException e)
                {
                    throw new ConnectionException(e.Message);
                }

                return (IEnumerable<string>)tableNames;
            });
        }

        public Task<IEnumerable<ColumnSchema>> GetColumnsSchemaAsync(string tableName)
        {
            return Task.Run(() =>
            {
                ColumnSchema[] columns = null;
                SqlConnectionStringBuilder csb = new SqlConnectionStringBuilder(ConnectionString);
                string db = csb.InitialCatalog;
                try
                {
                    using (SqlConnection conn = new SqlConnection(ConnectionString))
                    {
                        conn.Open();
                        DataTable dt = conn.GetSchema("Columns", new[] { db, null, tableName });
                        columns = new ColumnSchema[dt.Rows.Count];

                        int index = 0;

                       // Console.WriteLine(dt.Columns.Count);

                        foreach (DataRow dr in dt.Rows)
                        {
                            columns[index] = new ColumnSchema(dr["COLUMN_NAME"].ToString());
                            columns[index].DataType = MapTypeFromDb(dr["DATA_TYPE"].ToString());
                            columns[index].IsNullable = dr["IS_NULLABLE"].ToString() == "true" ? true : false;
                            if (columns[index].DataType == typeof(string))
                                columns[index].MaxLength = (int)dr["CHARACTER_MAXIMUM_LENGTH"];
                            index++;

                        }

                    }
                }
                catch (SqlException e)
                {
                    throw new ConnectionException(e.Message);
                }

                return (IEnumerable<ColumnSchema>)columns;
            });
        }

        public async Task<DatabaseSchema> GetDatabaseSchemaAsync()
        {
            var tableNames = await GetTableNamesAsync();
            var tabsSchema = new List<TableSchema>();

            foreach (string tabName in tableNames)
            {
                var cs = await GetColumnsSchemaAsync(tabName);
                tabsSchema.Add(new TableSchema { TableName = tabName, Columns = cs });
            }

            return new DatabaseSchema { Tables = tabsSchema };
        }


        public async Task<bool> CheckSchemaExists()
        {
            var tableNames = await GetTableNamesAsync();
            var c = StringComparer.InvariantCultureIgnoreCase;

            return tableNames.Contains("GlobalVariable", c) && tableNames.Contains("EventSummary", c)
            && tableNames.Contains("Archive", c) && tableNames.Contains("Tag", c)
            && tableNames.Contains("Event", c) && tableNames.Contains("AnalogEvent", c)
            && tableNames.Contains("DiscreteEvent", c) && tableNames.Contains("CyclicEvent", c)
            && tableNames.Contains("EventHistory", c) && tableNames.Contains("EventSnapshot", c);
        }






        private static Type MapTypeFromDb(string type)
        {

            switch (type)
            {
                case "bit":
                    return typeof(Boolean);
                case "nvarchar":
                case "varchar":
                    return typeof(string);
                case "datetime":
                    return typeof(DateTime);
                case "smallint":
                    return typeof(short);
                case "tinyint":
                    return typeof(short);
                case "int":
                    return typeof(int);
                case "bigint":
                    return typeof(long);
                case "real":
                    return typeof(float);
                case "float":
                    return typeof(double);
                default:
                    return typeof(Object);
            }


        }

        public Task<IEnumerable<KeyValuePair<DateTime, double>>> GetTagValuesAsync(ITag tag, DateTime from, DateTime to)
        {

            return Task.Run(() =>
            {
                string query = CommandProvider.GetTagValues(tag, from, to);
                var list = new List<KeyValuePair<DateTime, double>>();
                using (var conn = GetSqlConnection())
                {
                    conn.Open();

                    var cmd = conn.CreateCommand();
                    cmd.CommandText = query;

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (tag.TagArchive.ArchiveType == ArchiveType.Normal)
                        {
                            while (reader.Read())
                            {
                                DateTime date = (DateTime)reader["TagTimestamp"];
                                double value = (double)reader["TagValue"];
                                list.Add(new KeyValuePair<DateTime, double>(date, value));
                            }
                        }
                        else
                        {
                            while (reader.Read())
                            {
                                DateTime date = (DateTime)reader["TagTimestamp"];
                                object val = reader[tag.Name];
                                if (val.GetType() != typeof(bool))
                                {
                                    double value = double.Parse(val.ToString());
                                    list.Add(new KeyValuePair<DateTime, double>(date, value));
                                }
                                else
                                {
                                    if (((bool)val) == true)
                                    {
                                        list.Add(new KeyValuePair<DateTime, double>(date, 1));
                                    }
                                    else
                                    {
                                        list.Add(new KeyValuePair<DateTime, double>(date, 0));
                                    }
                                }


                            }
                        }
                    }
                }

                return (IEnumerable<KeyValuePair<DateTime, double>>)list;

            });

        }
    }
}
