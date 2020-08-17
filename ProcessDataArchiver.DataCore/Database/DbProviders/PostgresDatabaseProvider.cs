using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProcessDataArchiver.DataCore.Database.Schema;
using ProcessDataArchiver.DataCore.Database.CommandProviders;
using Npgsql;
using NpgsqlTypes;
using ProcessDataArchiver.DataCore.DbEntities.Events;
using ProcessDataArchiver.DataCore.DbEntities.Tags;
using ProcessDataArchiver.DataCore.DbEntities;
using System.Xml.Linq;
using System.Transactions;
using System.Threading;

namespace ProcessDataArchiver.DataCore.Database.DbProviders
{
    public class PostgresDatabaseProvider : IDatabaseProvider
    {
        public PostgresDatabaseProvider() { }

        public PostgresDatabaseProvider(string connectionStrng, ICommandProvider provider)
        {
            CommandProvider = provider;
            ConnectionString = connectionStrng;
        }

        public ICommandProvider CommandProvider { get; set; }
        public DatabaseType CmdProviderType
        {
            get { return DatabaseType.PostgreSql; }
        }
        public string ConnectionString { get; set; }

        public DatabaseType DatabaseType
        {
            get { return DatabaseType.PostgreSql; }
        }

        public NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(ConnectionString);
        }

        public DbConnection GetDbConnection()
        {
            return new NpgsqlConnection(ConnectionString);
        }

        public DataAdapter GetDataAdapter(DbCommand cmd)
        {
            var sCmd = cmd as NpgsqlCommand;
            return new NpgsqlDataAdapter(sCmd);
        }


        public NpgsqlParameter CreateParameter(string propName, string name, object ob)
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
                    return  new NpgsqlParameter(name, val);

                }
            }
            throw new ArgumentException("Wrong parameter name");
        }
        public NpgsqlParameter CreateParameter(string propName, string name, object ob, NpgsqlDbType type)
        {
            var par = CreateParameter(propName, name, ob);
            par.NpgsqlDbType = type;
            return par;
        }


        public Task ExecutePreparedAsync(string query, params DbParameter[] parameters)
        {
            return Task.Run(() =>
            {
                try
                {
                    using (var conn = GetConnection())
                    {
                        conn.Open();

                        var cmd = conn.CreateCommand();
                        cmd.CommandText = query;
                        foreach (var par in parameters)
                        {
                            var para = (par as NpgsqlParameter);
                            para.NpgsqlDbType = MapToDbType(par.Value.GetType());
                            cmd.Parameters.Add(para);
                        }
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (NpgsqlException e)
                {
                    throw new ConnectionException(e.Message);
                }

            });
        }

        public async Task CallProcedureAsync(string name, IEnumerable<DbParameter> parameters)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    await conn.OpenAsync();
                    var cmd = conn.CreateCommand();

                    cmd.CommandText = name;
                    cmd.CommandType = CommandType.StoredProcedure;

                    foreach (var par in parameters)
                    {
                        if (par.Value != null)
                        {
                            var para = par as NpgsqlParameter;
                            para.NpgsqlDbType = MapToDbType(par.Value.GetType());
                            cmd.Parameters.Add(para);
                        }
                        else
                        {
                            cmd.Parameters.Add(new NpgsqlParameter(par.ParameterName, DBNull.Value));
                        }
                    }

                    await cmd.ExecuteNonQueryAsync();
                }
            }
            catch (PostgresException e)
            {
                throw new ConnectionException(e.Message);
            }
            catch (NpgsqlException e)
            {
                throw new ConnectionException(e.Message);
            }

        }

        public async Task CallProcedureAsync(string name, IEnumerable<DbParameter> parameters,CancellationToken token)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    await conn.OpenAsync(token);
                    var cmd = conn.CreateCommand();

                    cmd.CommandText = name;
                    cmd.CommandType = CommandType.StoredProcedure;

                    foreach (var par in parameters)
                    {
                        if (par.Value != null)
                            cmd.Parameters.Add((NpgsqlParameter)par);
                        else
                            cmd.Parameters.Add(new NpgsqlParameter(par.ParameterName, DBNull.Value));
                    }

                    await cmd.ExecuteNonQueryAsync(token);
                }
            }
            catch (PostgresException e)
            {
                throw new ConnectionException(e.Message);
            }
            catch (NpgsqlException ex)
            {
                throw new ConnectionException(ex.Message);
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

        public async Task CreateGlobalVariablesAsync(params GlobalVariable[] gv)
        {
            foreach (var gvar in gv)
            {
                var param = new List<NpgsqlParameter>();
                param.Add(new NpgsqlParameter("pname", gvar.Name)
                { NpgsqlDbType = NpgsqlDbType.Varchar });
                param.Add(new NpgsqlParameter("paddress", (long)gvar.Address)
                { NpgsqlDbType = NpgsqlDbType.Bigint });
                param.Add(new NpgsqlParameter("pvariablesize", gvar.Size)
                { NpgsqlDbType = NpgsqlDbType.Integer });
                param.Add(new NpgsqlParameter("pnettype", gvar.NetType.ToString())
                { NpgsqlDbType = NpgsqlDbType.Varchar });
                await CallProcedureAsync("Create_GlobalVariable", param.ToArray());
                gvar.Id = await GetIdAsync(gvar.Name, "GlobalVariable");
            }
        }

        public Task<IEnumerable<GlobalVariable>> GetGlobalVariablesAsync()
        {
            return Task.Run(() =>
            {
                var gvars = new List<GlobalVariable>();
                using (var conn = GetConnection())
                {
                    conn.Open();
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = "Select Id,Name,Address,VariableSize,NetType From GlobalVariable "+
                    "Order By Id;";

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var gvar = new GlobalVariable();
                            gvar.Id = reader.GetInt32(0);
                            gvar.Name = reader.GetString(1);
                            gvar.Address = (uint)reader.GetDecimal(2);
                            gvar.Size = reader.GetInt32(3);
                            gvar.NetType = Type.GetType(reader.GetString(4));

                            gvars.Add(gvar);
                        }
                    }
                }
                return (IEnumerable<GlobalVariable>)gvars;
            });
        }

        public async Task CreateArchivesAsync(params TagArchive[] archives)
        {
            foreach (var archive in archives)
            {
                var pType = new NpgsqlParameter("parchivetype", (int)archive.ArchiveType);
                pType.NpgsqlDbType = NpgsqlDbType.Integer;
                var pName = new NpgsqlParameter("pname", archive.Name);
                pName.NpgsqlDbType = NpgsqlDbType.Varchar;

                await CallProcedureAsync("Create_Archive",pType,pName);
                var prov = CommandProvider as IExtendedCmdProvider;
                string cmd = prov.CreateArchiveCmd(archive).FirstOrDefault();

                await ExecuteNonQueryAsync(cmd);
                archive.Id = await GetIdAsync(archive.Name, "Archive");


            }
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



        public async Task CreateEventsAsync(params IEvent[] archiveEvents)
        {
            foreach (var archiveEvent in archiveEvents)
            {

                List<NpgsqlParameter> param = new List<NpgsqlParameter>();
                param.Add(new NpgsqlParameter("peventtype", (int)archiveEvent.EventType)
                { NpgsqlDbType = NpgsqlDbType.Integer });
                param.Add(new NpgsqlParameter("peventactiontype",(int) archiveEvent.EventActionType)
                { NpgsqlDbType = NpgsqlDbType.Integer });
                param.Add(new NpgsqlParameter("pname", archiveEvent.Name)
                { NpgsqlDbType = NpgsqlDbType.Varchar });
                param.Add(new NpgsqlParameter("prefreshspan", (int)archiveEvent.RefreshSpan.TotalMilliseconds)
                { NpgsqlDbType = NpgsqlDbType.Integer });
                param.Add(new NpgsqlParameter("pactiontext", archiveEvent.ActionText)
                {NpgsqlDbType = NpgsqlDbType.Varchar});
                param.Add(new NpgsqlParameter("penabled", archiveEvent.Enabled ? 1 : 0)
                {NpgsqlDbType = NpgsqlDbType.Integer});
                param.Add(new NpgsqlParameter("plastchanged", archiveEvent.LastChanged)
                { NpgsqlDbType = NpgsqlDbType.Timestamp });
                param.Add(new NpgsqlParameter("pcomment", archiveEvent.Comment)
                { NpgsqlDbType = NpgsqlDbType.Varchar });

                await CallProcedureAsync("Create_Event", param);
                archiveEvent.Id = await GetIdAsync(archiveEvent.Name, "Event");

                if (archiveEvent is DiscreteEvent)
                {
                    var dEvent = archiveEvent as DiscreteEvent;
                    List<NpgsqlParameter> dParam = new List<NpgsqlParameter>();
                    dParam.Add(new NpgsqlParameter("pglobalvariableid", dEvent.GlobalVariableId)
                    { NpgsqlDbType = NpgsqlDbType.Integer });
                    dParam.Add(new NpgsqlParameter("peventid", dEvent.Id)
                    { NpgsqlDbType = NpgsqlDbType.Integer });
                    dParam.Add(new NpgsqlParameter("pedgetype",(int) dEvent.EdgeType)
                    { NpgsqlDbType = NpgsqlDbType.Integer });
                    await CallProcedureAsync("Create_DiscreteEvent", dParam);

                    var id = await GetIdAsync(archiveEvent.Name, "DiscreteEventInfo", "DiscreteEventId");
                    dEvent.DiscreteEventId = id;
                }
                else if (archiveEvent is AnalogEvent)
                {
                    var aEvent = archiveEvent as AnalogEvent;
                    var aParam = new List<NpgsqlParameter>();
                    aParam.Add(new NpgsqlParameter("pglobalvariableid", aEvent.GlobalVariableId)
                    { NpgsqlDbType = NpgsqlDbType.Integer });
                    aParam.Add(new NpgsqlParameter("peventid", aEvent.Id)
                    { NpgsqlDbType = NpgsqlDbType.Integer });
                    aParam.Add(new NpgsqlParameter("peventtriggertype",(int)aEvent.EventTriggerType)
                    { NpgsqlDbType = NpgsqlDbType.Integer });
                    aParam.Add(new NpgsqlParameter("ptriggervalue", aEvent.TriggerValue)
                    { NpgsqlDbType = NpgsqlDbType.Double });
                    await CallProcedureAsync("Create_AnalogEvent", aParam);

                    var id = await GetIdAsync(archiveEvent.Name, "AnalogEventInfo", "AnalogEventId");

                    aEvent.AnalogEventId = id;
                }
                else if (archiveEvent is CyclicEvent)
                {
                    var cEvent = archiveEvent as CyclicEvent;
                    var cParam = new List<NpgsqlParameter>();
                    cParam.Add(new NpgsqlParameter("peventid", cEvent.Id));
                    cParam.Add(new NpgsqlParameter("peventcycletype",(int)cEvent.EventCycleType));

                    //cParam.Add(new NpgsqlParameter("pyears", cEvent.CycleStamp.Year)
                    //{ NpgsqlDbType = NpgsqlDbType.Integer });
                    //cParam.Add(new NpgsqlParameter("pmonths", cEvent.CycleStamp.Month)
                    //{ NpgsqlDbType = NpgsqlDbType.Integer });
                    cParam.Add(new NpgsqlParameter("pdays", cEvent.CycleStamp.Days)
                    { NpgsqlDbType = NpgsqlDbType.Integer });
                    cParam.Add(new NpgsqlParameter("phours", cEvent.CycleStamp.Hours)
                    { NpgsqlDbType = NpgsqlDbType.Integer });
                    cParam.Add(new NpgsqlParameter("pminutes", cEvent.CycleStamp.Minutes)
                    { NpgsqlDbType = NpgsqlDbType.Integer });
                    cParam.Add(new NpgsqlParameter("pseconds", cEvent.CycleStamp.Seconds)
                    { NpgsqlDbType = NpgsqlDbType.Integer });

                    cParam.Add(new NpgsqlParameter("pnextevent", cEvent.NextEvent)
                    { NpgsqlDbType = NpgsqlDbType.Timestamp });
                    await CallProcedureAsync("Create_CyclicEvent", cParam);

                    var id = await GetIdAsync(archiveEvent.Name, "CyclicEventInfo", "CyclicEventId");
                    cEvent.CyclicEventId = id;

                }

            }
        }

        public async Task CreateTagsAsync(params ITag[] tags)
        {
            try
            {
                foreach (var tag in tags)
                {
                    List<NpgsqlParameter> pars = new List<NpgsqlParameter>();
                    pars.Add(new NpgsqlParameter("parchiveid", tag.TagArchiveId)
                    { NpgsqlDbType = NpgsqlDbType.Integer });
                    pars.Add(new NpgsqlParameter("pglobalvariableid", tag.GlobalVariableId)
                    { NpgsqlDbType = NpgsqlDbType.Integer });
                    pars.Add(new NpgsqlParameter("ptagtype", (int)tag.TagType)
                    { NpgsqlDbType = NpgsqlDbType.Integer });
                    pars.Add(new NpgsqlParameter("pname", tag.Name)
                    { NpgsqlDbType = NpgsqlDbType.Varchar });
                    pars.Add(new NpgsqlParameter("prefreshspan", (int)tag.RefreshSpan.TotalMilliseconds)
                    { NpgsqlDbType = NpgsqlDbType.Integer });
                    pars.Add(new NpgsqlParameter("parchivingtype", (int)tag.ArchivingType)
                    { NpgsqlDbType = NpgsqlDbType.Integer });


                    if (tag is AnalogTag)
                    {
                        var atag = tag as AnalogTag;
                        if (!string.IsNullOrEmpty(atag.EuName))
                        {
                            pars.Add(new NpgsqlParameter("peuname", atag.EuName)
                            {
                                NpgsqlDbType = NpgsqlDbType.Varchar
                            });
                        }
                        else
                        {
                            pars.Add(new NpgsqlParameter("peuname", DBNull.Value)
                            {
                                NpgsqlDbType = NpgsqlDbType.Varchar
                            });
                        }
                        pars.Add(new NpgsqlParameter("pdeadbandtype", (int)atag.DeadbandType)
                        {
                            NpgsqlDbType = NpgsqlDbType.Integer
                        });
                        pars.Add(new NpgsqlParameter("pdeadbandvalue", atag.DeadbandValue)
                        {
                            NpgsqlDbType = NpgsqlDbType.Double
                        });
                    }
                    else
                    {
                        pars.Add(new NpgsqlParameter("peuname", DBNull.Value)
                        {
                            NpgsqlDbType = NpgsqlDbType.Varchar
                        });
                        pars.Add(new NpgsqlParameter("pdeadbandtype", DBNull.Value)
                        {
                            NpgsqlDbType = NpgsqlDbType.Integer
                        });
                        pars.Add(new NpgsqlParameter("pdeadbandvalue", DBNull.Value)
                        {
                            NpgsqlDbType = NpgsqlDbType.Double
                        });
                    }
                    pars.Add(new NpgsqlParameter("plastchanged", tag.LastChanged)
                    { NpgsqlDbType = NpgsqlDbType.Timestamp });

                    if (tag.Comment != null)
                    {
                        pars.Add(new NpgsqlParameter("pcomment", tag.Comment)
                        { NpgsqlDbType = NpgsqlDbType.Varchar });
                    }
                    else
                    {
                        pars.Add(new NpgsqlParameter("pcomment", DBNull.Value)
                        { NpgsqlDbType = NpgsqlDbType.Varchar });
                    }

                    await CallProcedureAsync("Create_Tag", pars);
                    tag.Id = await GetIdAsync(tag.Name, "Tag");

                }
            }
            catch(Exception ex)
            {

            }
        }

        public async Task DeleteArchivesAsync(params TagArchive[] archives)
        {
            foreach (var archive in archives)
            {
                await ExecutePreparedAsync("Delete From Archive Where Id=@Id",
                    new NpgsqlParameter("@Id", archive.Id));
                await ExecuteNonQueryAsync("Drop Table " + archive.Name + ";");
            }
        }



        public async Task DeleteGlobalVariablesAsync(params GlobalVariable[] gvs)
        {
            foreach (var gv in gvs)
            {
                await ExecutePreparedAsync("Delete From GlobalVariable Where Id=@Id;"
                    , new NpgsqlParameter("@Id", gv.Id));
            }
        }

        public async Task DeleteEventsAsync(params IEvent[] events)
        {
            foreach (var ev in events)
            {
                await ExecutePreparedAsync("Delete From Event Where Id=@Id;",
                    new NpgsqlParameter("@Id", ev.Id));
            }
        }

        public async Task DeleteTagsAsync(params ITag[] tags)
        {
            foreach (var tag in tags)
            {
                await ExecutePreparedAsync("Delete From Tag Where Id=@Id",
                     new NpgsqlParameter("@Id", tag.Id));
            }
        }

        public async Task<int> ExecuteNonQueryAsync(string command)
        {
            int count = 0;
            try
            {
                using (var conn = GetConnection())
                {
                    await conn.OpenAsync();
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = command;
                    count = await cmd.ExecuteNonQueryAsync();
                }
            }
            catch (PostgresException pe)
            {
                throw new ConnectionException(pe.Message);
            }
            catch (NpgsqlException e)
            {
                throw new ConnectionException(e.Message);
            }
            catch (OverflowException ex)
            {
                throw new ConnectionException(ex.Message);
            }
            return count;

        }


        public async Task<int> ExecuteNonQueryAsync(string command,CancellationToken token)
        {
            int count = 0;
            try
            {
                using (var conn = GetConnection())
                {
                    await conn.OpenAsync(token);
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = command;
                    count = await cmd.ExecuteNonQueryAsync(token);
                }
            }
            catch (PostgresException pe)
            {
                throw new ConnectionException(pe.Message);
            }
            catch (NpgsqlException e)
            {
                throw new ConnectionException(e.Message);
            }
            catch (OverflowException ex)
            {
                throw new ConnectionException(ex.Message);
            }
            catch (TaskCanceledException) { }
            return count;

        }



        public async Task<DataTable> ExecuteQueryAsync(string command)
        {
            DataTable dt = new DataTable();

            try
            {
                using (var conn = GetConnection())
                {
                    await conn.OpenAsync();
                    var adapter = new NpgsqlDataAdapter(command, conn);
                    adapter.Fill(dt);

                }
            }
            catch (NpgsqlException e)
            {
                throw new ConnectionException(e.Message);
            }

            return dt;
        }

        public async Task<DataTable> ExecuteQueryAsync(string command,CancellationToken token)
        {
            DataTable dt = new DataTable();
            dt.TableNewRow += (s, e) =>
            {
                if (token.IsCancellationRequested)
                    token.ThrowIfCancellationRequested();
            };
            try
            {
                using (var conn = GetConnection())
                {
                    await conn.OpenAsync();
                    var adapter = new NpgsqlDataAdapter(command, conn);
                    adapter.Fill(dt);

                }
            }
            catch (NpgsqlException e)
            {
                throw new ConnectionException(e.Message);
            }
            catch (TaskCanceledException) { }
            return dt;
        }



        public async Task<int> FillDataSetAsync(string command, DataTable ds)
        {
            int i = 0;
            try
            {
                using (var conn = GetConnection())
                {
                    await conn.OpenAsync();
                    var adapter = new NpgsqlDataAdapter(command, conn);
                    i = adapter.Fill(ds);
                }
            }
            catch (NpgsqlException e)
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
                using (var conn = GetConnection())
                {
                    await conn.OpenAsync(token);
                    var adapter = new NpgsqlDataAdapter(command, conn);
                    i = adapter.Fill(ds);
                }
            }
            catch (NpgsqlException e)
            {
                throw new ConnectionException(e.Message);
            }
            catch (TaskCanceledException) { }
            return i;

        }

        public async Task<int> FillDataSetAsync(QueryOptions options, DataTable ds)
        {
                int i = 0;
                string command = CommandProvider.CreateQuery(options);
                try
                {
                    using (var conn = GetConnection())
                    {
                        await conn.OpenAsync();
                        var adapter = new NpgsqlDataAdapter(command, conn);
                        i = adapter.Fill(ds);

                    }
                }
                catch (NpgsqlException e)
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
            int i = 0;
            string command = CommandProvider.CreateQuery(options);
            try
            {
                using (var conn = GetConnection())
                {
                    await conn.OpenAsync(token);
                    var adapter = new NpgsqlDataAdapter(command, conn);
                    i = adapter.Fill(ds);

                }
            }
            catch (NpgsqlException e)
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

        public Task<IEnumerable<TagArchive>> GetArchivesAsync()
        {
            return Task.Run(() =>
            {
                List<TagArchive> archives = new List<TagArchive>();
                using (var conn = GetConnection())
                {
                    conn.Open();
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = "Select Id,Name,ArchiveType From Archive Order By Id;";
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



        public Task<IEnumerable<IEvent>> GetEventsAsync()
        {
            return Task.Run(() =>
            {
                List<IEvent> events = new List<IEvent>();

                using (var conn = GetConnection())
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

                using (var conn = GetConnection())
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
                            ev.AnalogEventId = (int)reader["AnalogEventId"];
                            ev.EventActionType = (EventActionType)reader["EventActionType"];
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
                using (var conn = GetConnection())
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
                            ev.CyclicEventId = (int)reader["CyclicEventId"];
                            ev.EventActionType = (EventActionType)reader["EventActionType"];
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

        public Task<IEnumerable<ITag>> GetTagsAsync()
        {
            return Task.Run(() =>
            {
                List<ITag> tags = new List<ITag>();
                using (var conn = GetConnection())
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

        public async Task InsertEventSnapshotAsync(IEvent archiveEvent,params GlobalVariable[] gvs)
        {
            foreach (var gv in gvs)
            {
                var evid = new NpgsqlParameter("peventid", archiveEvent.Id);
                var gvid = new NpgsqlParameter("pglobalvariableId", gv.Id);
                var val = new NpgsqlParameter("pvariablevalue", gv.CurrentValue);
                await CallProcedureAsync("Insert_EventSnapshot", evid, gvid, val);
            }
        }

        public async Task InsertEventsValueAsync(params IEvent[] archiveEvents)
        {
            foreach (var ae in archiveEvents)
            {
                var euid = new NpgsqlParameter("peventid", ae.Id);
                await CallProcedureAsync("Insert_EventHistory", euid);
            }
        }
        public async Task InsertEventSnapshotAsync(IEvent archiveEvent, params ITag[] tags)
        {
            foreach (var tag in tags)
            {
                    var evid = new NpgsqlParameter("peventid", archiveEvent.Id);
                    var gvid = new NpgsqlParameter("ptagid", tag.Id);
                    var val = new NpgsqlParameter("ptagvalue", tag.CurrentValue);
                    await CallProcedureAsync("Insert_EventSnapshot", evid, gvid, val);
            }
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
                    string cmdEnd = " Values(NOW(),";
                    var aTags = archive.Tags.ToList();
                    var cmdPar = new List<NpgsqlParameter>();


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
                            cmdPar.Add(new NpgsqlParameter("@" + aTags[i].Name, aTags[i].CurrentValue)
                            { NpgsqlDbType = MapToDbType(aTags[i].CurrentValue.GetType())});
                        else
                            cmdPar.Add(new NpgsqlParameter("@" + aTags[i].Name, DBNull.Value)
                            { NpgsqlDbType = MapToDbType(aTags[i].CurrentValue.GetType()) });
                    }
                    await ExecutePreparedAsync(cmd + cmdEnd, cmdPar.ToArray());
                }
                else
                {
                    string cmd = "Insert Into " + archive.Name + " (TagId,TagTimeStamp,TagValue)" +
                        " Values(@TagId,NOW(),@TagValue);";
                    foreach (var tag in archive.Tags)
                    {
                        var tagId = new NpgsqlParameter("@TagId", tag.Id);
                        if (tag is DiscreteTag)
                        {
                            var tagValue = new NpgsqlParameter("@TagValue", (bool)tag.CurrentValue ? 1 : 0);
                            await ExecutePreparedAsync(cmd, tagId, tagValue);
                        }
                        else
                        {
                            var tagValue = new NpgsqlParameter("@TagValue", tag.CurrentValue);
                            await ExecutePreparedAsync(cmd, tagId, tagValue);
                        }
                    }
                }


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
                TimeSpan ts = TimeSpan.Parse(timespan);

                var fd = DateTime.Now - ts;

                var tag = EntityContext.GetContext().Tags.Where(t => t.Name.Equals(tagName) &&
                t.TagArchive != null && t.TagArchive.Name.Equals(archive))
                    .FirstOrDefault();
                if (tag.TagArchive.ArchiveType == ArchiveType.Wide)
                {
                    string cmd = "INSERT INTO EventSummary(EventId,TagName,CalcType,FromDate,ToDate,CalcValue) " +
                        $"Values({archiveEvent.Id},'{tagName}','{action}'," +
                        $"'{fd.Year}-{fd.Month}-{fd.Day} {fd.Hour}:{fd.Minute}:{fd.Second}',NOW()," +
                        $"(SELECT {action}({tagName}) FROM {tag.TagArchive.Name} " +
                        $"WHERE TagTimestamp>'{fd.Year}-{fd.Month}-{fd.Day} {fd.Hour}:{fd.Minute}:{fd.Second}'));";
                    await ExecuteNonQueryAsync(cmd);
                }
                else
                {
                    string cmd = "INSERT INTO EventSummary(EventId,TagName,CalcType,FromDate,ToDate,CalcValue) " +
                    $"Values({archiveEvent.Id},'{tagName}','{action}'," +
                    $"'{fd.Year}-{fd.Month}-{fd.Day} {fd.Hour}:{fd.Minute}:{fd.Second}',NOW()," +
                    $"(SELECT {action}(TagValue) FROM {tag.TagArchive.Name} " +
                    $"WHERE TagTimestamp>'{fd.Year}-{fd.Month}-{fd.Day} {fd.Hour}:{fd.Minute}:{fd.Second}'" +
                    $" AND TagId='{tag.Id}'));";
                    await ExecuteNonQueryAsync(cmd);
                }
            }
        }



        public async Task<bool> TryConnectAsync()
        {

                bool polaczenie = false;
                try
                {
                    using (var conn = GetConnection())
                    {
                        await conn.OpenAsync();
                        polaczenie = true;
                    }
                }
                catch (NpgsqlException e)
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
                using (var conn = GetConnection())
                {
                    await conn.OpenAsync(token);
                    polaczenie = true;
                }
            }
            catch (NpgsqlException e)
            {
                throw new ConnectionException(e.Message);
            }
            catch (TaskCanceledException) { }

            return polaczenie;
        }


        private Task<int> GetIdAsync(string name, string table)
        {
            return Task.Run(() =>
            {
                string txt = $"SELECT ID FROM {table} WHERE NAME='{name}';";
                int id;

                using (var conn = GetConnection())
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


        private Task<DateTime> GetLastChangedAsync(string name, string table)
        {
            return Task.Run(() =>
            {
                string txt = $"SELECT LastChanged FROM {table} WHERE NAME='{name}';";
                DateTime dt;

                using (var conn = GetConnection())
                {
                    conn.Open();
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = txt;
                    using (var reader = cmd.ExecuteReader())
                    {
                        reader.Read();
                        dt = reader.GetDateTime(0);
                    }
                }
                return dt;
            });
        }

        private Task<int> GetIdAsync(string name, string table, string secondId)
        {
            return Task.Run(() =>
            {
                string txt = $"SELECT " + secondId + $" FROM {table} WHERE NAME='{name}';";
                int id;

                using (var conn = GetConnection())
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
                using (var conn = GetConnection())
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
                using (var conn = GetConnection())
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
            await DeleteGlobalVariablesAsync(gvsToRemvoe.ToArray());
            await CreateGlobalVariablesAsync(gvsToAdd.ToArray());
            


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


            //var tagsToUpdate = tagsChanges.Select(c => c.Entity as ITag);
            //foreach (var tag in tagsToUpdate)
            //{
            //    tag.LastChanged = await GetLastChangedAsync(tag.Name, "Tag");
            //}
            //var evsToUpdate = eventsChanges.Select(c => c.Entity as IEvent);
            //foreach (var ev in evsToUpdate)
            //{
            //    ev.LastChanged = await GetLastChangedAsync(ev.Name, "Event");
            //}





            var changedNames = changes.Where(c => c.Entity is ITag && c.Changes!=null &&
            c.Changes.Keys.Contains("Name")
              && c.Original != null);
            foreach (var item in changedNames)
            {
                var tag = item.Entity as ITag;
                await ChangeNamesAsync(tag.TagArchive.Name, item.Original["Name"].ToString(),
                    item.Changes["Name"].ToString());
            }

            var colTypeChanged = changes.Where(c => c.Entity is ITag && c.Changes!=null &&
            c.Changes.Keys.Contains("GlobalVariableId") && c.Original != null);

            foreach (var item in colTypeChanged)
            {
                var t = item.Entity as ITag;
                await ChangeDataType(t, (int)item.Original["GlobalVariableId"],
                    (int)item.Changes["GlobalVariableId"]);
            }


        }



        private async Task ChangeNamesAsync(string table, string oldName, string newName)
        {
            string query = $"AlTER {table} RENAME COLUMN {oldName} TO {newName};";
            await ExecuteNonQueryAsync(query);
        }

        private async Task ChangeDataType(ITag tag, int oldId, int newId)
        {
            string archive = tag.TagArchive.Name;
            string colName = tag.Name;

            var gv = EntityContext.GetContext().GlobalVariables;
            var oldGv = gv.Where(g => g.Id.Equals(oldId)).FirstOrDefault();
            var newGv = gv.Where(g => g.Id.Equals(newId)).FirstOrDefault();

            var oldType = oldGv.NetType;
            var newType = newGv.NetType;
            var eqType = DataTypeComparer.GetEquivalentType(oldType, newType);
            if (!oldType.Equals(eqType))
            {
                string typeToChange = CommandProvider.MapToDbType(eqType);
                string cmd = "ALTER TABLE " + archive + " ALTER COLUMN " + colName + " TYPE " + typeToChange + ";";
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

                string upATags = "UPDATE Tag ";
                string aTagCmdText = GetUpdateCmd(tagChanges.Changes, typeof(ITag), typeof(AnalogTag));
                if (aTagCmdText != null)
                {
                    upATags += aTagCmdText + " WHERE Id=" + (tagChanges.Entity as AnalogTag).Id + ";";
                    await ExecuteNonQueryAsync(upATags);
                }

                string upDTags = "UPDATE Tag ";
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
                string upd = "UPDATE Tag SET LastChanged = CURRENT_TIMESTAMP WHERE Id="
                    + (item.Key as ITag).Id + ";";
                await ExecuteNonQueryAsync(upd);
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
                string upd = "UPDATE Event SET LastChanged = CURRENT_TIMESTAMP WHERE Id="
                    + (item.Key as IEvent).Id + ";";
                await ExecuteNonQueryAsync(upd);

            }


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
                cmd += "DROP " + tag.Name + ";";
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

        private string GetUpdateCmd(IDictionary<string, object> values, Type baseType, Type derrivedType)
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
                using (var conn = GetConnection())
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

        private NpgsqlDbType MapToDbType(Type type)
        {
            if (type == typeof(bool))
                return NpgsqlDbType.Bit;
            else if (type == typeof(byte) || type == typeof(sbyte) || type == typeof(short))
                return NpgsqlDbType.Smallint;
            else if (type == typeof(ushort) || type == typeof(int))
                return NpgsqlDbType.Integer;
            else if (type == typeof(uint) || type == typeof(long))
                return NpgsqlDbType.Bigint;
            else if (type == typeof(ulong))
                return NpgsqlDbType.Numeric;
            else if (type == typeof(float))
                return NpgsqlDbType.Real;
            else if ((type == typeof(double)))
                return NpgsqlDbType.Double;
            else if (type == typeof(string))
                return NpgsqlDbType.Varchar;
            else if (type == typeof(DateTime))
                return NpgsqlDbType.Timestamp;
            else if (type == DBNull.Value.GetType())
                return NpgsqlDbType.Varchar;
            else
                throw new ArgumentException("Unsupported data type");
        }

        private Type MapToType(string type)
        {
            if (type.Contains("varchar"))
                return typeof(string);
            else if (type.Contains("int"))
                return typeof(int);
            else if (type == "datetime")
                return typeof(DateTime);
            else if (type == "float")
                return typeof(float);
            else if (type == "double")
                return typeof(double);
            else throw new ArgumentException("Unsupported argument type!");
        }

        public Task<IEnumerable<string>> GetTableNamesAsync()
        {
            return Task.Run(() =>
            {
                List<string> tableNames = new List<string>();

                using (NpgsqlConnection conn = new NpgsqlConnection(ConnectionString))
                {
                    conn.Open();
                    string command = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES " +
                    "WHERE TABLE_SCHEMA = 'public'";

                    NpgsqlCommand cmd = new NpgsqlCommand(command, conn);

                    using (NpgsqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            tableNames.Add(dr[0].ToString());
                        }
                    }
                }

                return (IEnumerable<string>)tableNames;
            });
        }

        public Task<IEnumerable<ColumnSchema>> GetColumnsSchemaAsync(string tableName)
        {
            return Task.Run(() =>
            {
                List<ColumnSchema> columns = new List<ColumnSchema>();
                string command = "SELECT COLUMN_NAME, IS_NULLABLE, DATA_TYPE" +
                   " FROM INFORMATION_SCHEMA.COLUMNS " +
                   $"WHERE TABLE_SCHEMA = 'public' AND TABLE_NAME='{tableName}';";

                using (NpgsqlConnection conn = new NpgsqlConnection(ConnectionString))
                {
                    conn.Open();

                    NpgsqlCommand cmd = new NpgsqlCommand(command, conn);

                    using (NpgsqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            ColumnSchema cs = new ColumnSchema(dr["COLUMN_NAME"].ToString());
                            string type = dr["DATA_TYPE"].ToString();
                            cs.DataType = MapTypeFromDb(type);
                            cs.IsNullable = dr["IS_NULLABLE"].ToString().Equals("YES") ? true : false;
                            //if (cs.DataType == typeof(string) && type.Contains("("))

                                columns.Add(cs);
                        }
                    }
                }

                return (IEnumerable<ColumnSchema>) columns;
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

            if (type.Contains("character"))
                return typeof(string);
            else if (type.Contains("int"))
                return typeof(int);
            else if (type.Contains("datetime") || type.Contains("timestamp"))
                return typeof(DateTime);
            else if (type.Contains("float"))
                return typeof(float);
            else if (type.Contains("double"))
                return typeof(double);
            else throw new ArgumentException("Unsupported argument type!");

        }

        public Task<IEnumerable<KeyValuePair<DateTime, double>>> GetTagValuesAsync(ITag tag, DateTime from, DateTime to)
        {
            return Task.Run(() =>
            {
                string query = CommandProvider.GetTagValues(tag, from, to);
                var list = new List<KeyValuePair<DateTime, double>>();
                using (var conn = GetConnection())
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
