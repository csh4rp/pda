using ProcessDataArchiver.DataCore.Database.DbProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProcessDataArchiver.DataCore.Database.Schema;
using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using ProcessDataArchiver.DataCore.Database.CommandProviders;
using ProcessDataArchiver.DataCore.DbEntities.Events;
using ProcessDataArchiver.DataCore.DbEntities.Tags;
using ProcessDataArchiver.DataCore.DbEntities;
using System.Xml.Linq;
using System.Transactions;
using System.Threading;

namespace ProcessDataArchiver.DataCore.Database.DbProviders
{
    public  class OdbcProvider : IDatabaseProvider
    {
        public DatabaseType DbType { get; set; }
        public string Driver { get; set; }

        public string ConnectionString { get; set; }

        public ICommandProvider CommandProvider { get; set; }

        public DatabaseType DatabaseType
        {
            get;set;
        }

        public DatabaseType CmdProviderType
        {
            get;set;
        }

        public OdbcProvider(DatabaseType type, ICommandProvider provider,string driver, string connStr)
        {
            DatabaseType = type;
            Driver = driver;
            CommandProvider = provider;
            ConnectionString = connStr;
        }

       
        private OdbcConnection GetConnection()
        {
            return new OdbcConnection(ConnectionString);
        }



        protected Type MapToType(string type)
        {
            switch (type)
            {
                case "System.Boolean":
                    return typeof(bool);
                case "System.Int16":
                    return typeof(short);
                case "System.UInt16":
                    return typeof(ushort);
                case "System.Int32":
                    return typeof(int);
                case "System.UInt32":
                    return typeof(uint);
                case "System.Int64":
                    return typeof(long);
                case "System.UInt64":
                    return typeof(ulong);
                case "System.Byte":
                    return typeof(byte);
                case "System.String":
                    return typeof(string);
                case "System.Single":
                    return typeof(float);
                case "System.Double":
                    return typeof(double);
                case "System.Decimal":
                    return typeof(decimal);
                default:
                    throw new ArgumentException("Unsupported data type!");
            }
        }

        protected OdbcType MapToDbType(Type type)
        {
            if (type == typeof(bool))
                return OdbcType.Bit;
            else if (type == typeof(byte))
                return OdbcType.TinyInt;
            else if (type == typeof(short) || type == typeof(sbyte))
                return OdbcType.SmallInt;
            else if (type == typeof(ushort) || type == typeof(int))
                return OdbcType.Int;
            else if (type == typeof(uint) || type == typeof(long))
                return OdbcType.BigInt;
            else if (type == typeof(ulong))
                return OdbcType.Decimal;
            else if (type == typeof(float))
                return OdbcType.Real;
            else if (type == typeof(double))
                return OdbcType.Double;
            else if (type == typeof(string))
                return OdbcType.NVarChar;
            else if (type == typeof(DateTime))
                return OdbcType.DateTime;
            else
                throw new ArgumentException("Unsupported data type");
            
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

        private Task<int> GetEventIdAsync(string name, string table, string secondId)
        {
            return Task.Run(() =>
            {
                string txt = "SELECT " + secondId + $" FROM {table} WHERE NAME='{name}';";
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

        public async Task CreateGlobalVariablesAsync(params GlobalVariable[] gv)
        {
            foreach (var gvar in gv)
            {
                var param = new List<OdbcParameter>();
                if (CmdProviderType == DatabaseType.PostgreSql)
                {
                    param.Add(new OdbcParameter("pname", gvar.Name) );
                    param.Add(new OdbcParameter("paddress", (long)gvar.Address));
                    param.Add(new OdbcParameter("pvariableSize", gvar.Size) );
                    param.Add(new OdbcParameter("pnetType", gvar.NetType.ToString()));
                }
                else if(CmdProviderType == DatabaseType.Oracle)
                {
                    decimal adr = gvar.Address;
                    param.Add(new OdbcParameter("name", gvar.Name));
                    param.Add(new OdbcParameter("address", adr));
                    param.Add(new OdbcParameter("variableSize", gvar.Size));
                    param.Add(new OdbcParameter("netType", gvar.NetType.ToString()));

                }
                else
                {
                    param.Add(new OdbcParameter("@Name", gvar.Name) { OdbcType = OdbcType.VarChar });
                    param.Add(new OdbcParameter("@Address", (long)gvar.Address) { OdbcType = OdbcType.BigInt });
                    param.Add(new OdbcParameter("@VariableSize", gvar.Size) { OdbcType = OdbcType.BigInt });
                    param.Add(new OdbcParameter("@NetType", gvar.NetType.ToString()) { OdbcType = OdbcType.VarChar });
                }
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
            catch (OdbcException e)
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
            catch (OdbcException e)
            {
                throw new ConnectionException(e.Message);
            }
            catch (TaskCanceledException) { }
            return polaczenie;

        }

        public async Task<DataTable> ExecuteQueryAsync(string command)
        {
            var ds = new DataTable();

            try
            {
                using (var conn = GetConnection())
                {
                    await conn.OpenAsync();
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = command;
                    var adapter = new OdbcDataAdapter(cmd);

                    adapter.Fill(ds);
                }
            }
            catch (OdbcException e)
            {
                throw new ConnectionException(e.Message);
            }
            return ds;

        }


        public async Task<DataTable> ExecuteQueryAsync(string command,CancellationToken token)
        {
            var ds = new DataTable();
            ds.TableNewRow += (s, e) =>
            {
                if (token.IsCancellationRequested)
                    token.ThrowIfCancellationRequested();
            };

            try
            {
                using (var conn = GetConnection())
                {
                    await conn.OpenAsync(token);
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = command;
                    var adapter = new OdbcDataAdapter(cmd);

                    adapter.Fill(ds);
                }
            }
            catch (OdbcException e)
            {
                throw new ConnectionException(e.Message);
            }
            catch (TaskCanceledException) { }
            return ds;

        }

        public async Task<int> FillDataSetAsync(string command,DataTable ds)
        {
            int i = 0;
            try
            {
                using (var conn = GetConnection())
                {
                    await conn.OpenAsync();
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = command;

                    var adapter = new OdbcDataAdapter(cmd);

                    i = adapter.Fill(ds);
                }
            }
            catch (OdbcException e)
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
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = command;

                    var adapter = new OdbcDataAdapter(cmd);

                    i = adapter.Fill(ds);
                }
            }
            catch (OdbcException e)
            {
                throw new ConnectionException(e.Message);
            }
            catch(TaskCanceledException){ }
            return i;

        }


        public async Task<int> FillDataSetAsync(QueryOptions options, DataTable ds)
        {
            string command = CommandProvider.CreateQuery(options);
            int i = 0;
            try
            {
                using (var conn = GetConnection())
                {
                    await conn.OpenAsync();
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = command;

                    var adapter = new OdbcDataAdapter(cmd);

                    i = adapter.Fill(ds);
                }
            }
            catch (OdbcException e)
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
                using (var conn = GetConnection())
                {
                    await conn.OpenAsync(token);
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = command;

                    var adapter = new OdbcDataAdapter(cmd);

                    i = adapter.Fill(ds);
                }
            }
            catch (OdbcException e)
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

        public async Task<int> ExecuteNonQueryAsync(string command)
        {
            int res;
            try
            {
                using (var conn = GetConnection())
                {
                    await conn.OpenAsync();
                    var cmd = conn.CreateCommand();

                    cmd.CommandText = command;
                    res = cmd.ExecuteNonQuery();

                }
            }
            catch (OdbcException e)
            {
                throw new ConnectionException(e.Message);
            }
            return res;

        }

        public async Task<int> ExecuteNonQueryAsync(string command,CancellationToken token)
        {
            int res=0;
            try
            {
                using (var conn = GetConnection())
                {
                    await conn.OpenAsync();
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = command;
                    res = cmd.ExecuteNonQuery();

                }
            }
            catch (OdbcException e)
            {
                throw new ConnectionException(e.Message);
            }
            catch (TaskCanceledException) { }
            return res;

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




        public Task<IEnumerable<TagArchive>> GetArchivesAsync()
        {
            return Task.Run(() =>
            {
                List<TagArchive> archives = new List<TagArchive>();
                using (var conn = GetConnection())
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



        public Task<IEnumerable<ITag>> GetTagsAsync()
        {
            return Task.Run(() =>
            {
                List<ITag> tags = new List<ITag>();
                using (var conn = GetConnection())
                {
                    conn.Open();

                    var dcmd = conn.CreateCommand();
                    dcmd.CommandText = $"Select * From Tag Where TagType={(int)TagType.Discrete};";


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
                    acmd.CommandText = $"Select * From Tag Where TagType={(int)TagType.Analog};";
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

        public Task ExecutePreparedAsync(string query, params DbParameter[] parameters)
        {
            return Task.Run(() =>
            {
                OdbcTransaction transaction = null;
                try
                {
                    using (var conn = GetConnection())
                    {
                        conn.Open();
                        transaction = conn.BeginTransaction();

                        var cmd = conn.CreateCommand();
                        cmd.Transaction = transaction;
                        cmd.CommandText = query;
                        foreach (var par in parameters)
                        {
                            cmd.Parameters.Add(par);
                        }
                        cmd.ExecuteNonQuery();
                        transaction.Commit();
                    }
                }
                catch (OdbcException e)
                {

                    transaction.Rollback();
                    throw new ConnectionException(e.Message);
                }

            });
        }

        public async Task InsertTagsValueAsync(params ITag[] tags)
        {
            var groups = from t in tags
                         group t by t.TagArchive;

            var prov = CommandProvider as IExtendedCmdProvider;

            foreach (var item in groups)
            {
                var archive = item.Key;
                if (archive.ArchiveType == ArchiveType.Wide)
                {
                    string cmd = prov.InsertTagValueCmd(archive, archive.Tags);

                    var aTags = archive.Tags.ToList();
                    var cmdPar = new List<OdbcParameter>();


                    for (int i = 0; i < aTags.Count; i++)
                    {

                        if (item.Select(it => it.Name).Contains(aTags[i].Name))
                            cmdPar.Add(new OdbcParameter("@" + aTags[i].Name, aTags[i].CurrentValue)
                            {
                            //    SqlDbType = MapToDbType(aTags[i].GlobalVariable.NetType)
                            });
                        else
                            cmdPar.Add(new OdbcParameter("@" + aTags[i].Name, DBNull.Value));
                    }
                    await ExecutePreparedAsync(cmd, cmdPar.ToArray());
                }
                else
                {
                    string cmd = prov.InsertTagValueCmd(archive, archive.Tags);
                    foreach (var tag in archive.Tags)
                    {
                        var tagId = new OdbcParameter("@TagId", tag.Id);
                        if (tag is DiscreteTag)
                        {
                            var tagValue = new OdbcParameter("@TagValue", (bool)tag.CurrentValue ? 1 : 0);
                            await ExecutePreparedAsync(cmd, tagId, tagValue);
                        }
                        else
                        {
                            var tagValue = new OdbcParameter("@TagValue", tag.CurrentValue);
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
                if (CmdProviderType == DatabaseType.PostgreSql)
                {
                    var euid = new OdbcParameter("peventId", ae.Id);
                    await CallProcedureAsync("Insert_EventHistory", euid);
                }
                else
                {
                    var euid = new OdbcParameter("@EventId", ae.Id);
                    await CallProcedureAsync("Insert_EventHistory", euid);
                }
            }
        }



        public async Task CreateArchivesAsync(params TagArchive[] archives)
        {
            foreach (var archive in archives)
            {
                await CallProcedureAsync("Create_Archive",
                    new OdbcParameter("@ArchiveType", (int)archive.ArchiveType),
                     new OdbcParameter("@Name", archive.Name));

                foreach (var cmd in (CommandProvider as IExtendedCmdProvider).CreateArchiveCmd(archive))
                {
                    await ExecuteNonQueryAsync(cmd);
                }
               
                archive.Id = await GetIdAsync(archive.Name, "Archive");
            }
        }

        public async Task DeleteArchivesAsync(params TagArchive[] archives)
        {
            foreach (var archive in archives)
            {
                await ExecuteNonQueryAsync($"Delete From Archive Where Id={archive.Id};");
                await ExecuteNonQueryAsync("Drop Table " + archive.Name + ";");
            }
        }

        public async Task CreateTagsAsync(params ITag[] tags)
        {
            foreach (var tag in tags)
            {
                List<OdbcParameter> pars = new List<OdbcParameter>();
                if (CmdProviderType == DatabaseType.PostgreSql)
                {
                    pars.Add(new OdbcParameter("parchiveid", tag.TagArchiveId));
                    pars.Add(new OdbcParameter("pglobalvariableId", tag.GlobalVariableId));
                    pars.Add(new OdbcParameter("ptagtype", (int)tag.TagType));
                    pars.Add(new OdbcParameter("pname", tag.Name));
                    pars.Add(new OdbcParameter("prefreshspan", (int)tag.RefreshSpan.TotalMilliseconds));
                    pars.Add(new OdbcParameter("parchivingtype", (int)tag.ArchivingType));


                    if (tag is AnalogTag)
                    {
                        var atag = tag as AnalogTag;
                        pars.Add(new OdbcParameter("peuname", atag.EuName));
                        pars.Add(new OdbcParameter("pdeadbandtype", (int)atag.DeadbandType));
                        pars.Add(new OdbcParameter("pdeadbandvalue", atag.DeadbandValue));
                    }
                    else
                    {
                        pars.Add(new OdbcParameter("peuname", DBNull.Value));
                        pars.Add(new OdbcParameter("pdeadbandtype", DBNull.Value));
                        pars.Add(new OdbcParameter("pdeadbandvalue", DBNull.Value));
                    }

                    var ldt = tag.LastChanged;
                    var dt = new DateTime(ldt.Year, ldt.Month, ldt.Day, ldt.Hour, ldt.Minute, ldt.Second);

                    pars.Add(new OdbcParameter("plastchanged", dt) );

                    if (tag.Comment != null)
                        pars.Add(new OdbcParameter("pcomment", tag.Comment));
                    else
                        pars.Add(new OdbcParameter("pcomment", DBNull.Value));
                }
                else
                {
                    pars.Add(new OdbcParameter("@ArchiveId", tag.TagArchiveId));
                    pars.Add(new OdbcParameter("@GlobalVariableId", tag.GlobalVariableId));
                    pars.Add(new OdbcParameter("@TagType", (int)tag.TagType));
                    pars.Add(new OdbcParameter("@Name", tag.Name));
                    pars.Add(new OdbcParameter("@RefreshSpan", tag.RefreshSpan.TotalMilliseconds));
                    pars.Add(new OdbcParameter("@ArchivingType", (int)tag.ArchivingType));


                    if (tag is AnalogTag)
                    {
                        var atag = tag as AnalogTag;
                        pars.Add(new OdbcParameter("@EuName", atag.EuName));
                        pars.Add(new OdbcParameter("@DeadbandType", (int)atag.DeadbandType));
                        pars.Add(new OdbcParameter("@DeadbandValue", atag.DeadbandValue));
                    }
                    else
                    {
                        pars.Add(new OdbcParameter("@EuName", DBNull.Value));
                        pars.Add(new OdbcParameter("@DeadbandType", DBNull.Value));
                        pars.Add(new OdbcParameter("@DeadbandValue", DBNull.Value));
                    }

                    var ldt = tag.LastChanged;
                    var dt = new DateTime(ldt.Year, ldt.Month, ldt.Day, ldt.Hour, ldt.Minute, ldt.Second);

                    pars.Add(new OdbcParameter("@LastChanged", dt) { OdbcType = OdbcType.DateTime });

                    if (tag.Comment != null)
                        pars.Add(new OdbcParameter("@Comment", tag.Comment));
                    else
                        pars.Add(new OdbcParameter("@Comment", DBNull.Value));
                }
                await CallProcedureAsync("Create_Tag", pars);

                tag.Id = await GetIdAsync(tag.Name, "Tag");
            }
        }

        public async Task DeleteTagsAsync(params ITag[] tags)
        {
            foreach (var tag in tags)
            {
                //  await CallProcedureAsync("Delete_Tag", new OdbcParameter("@Id", tag.Id));
                await ExecutePreparedAsync("Delete From Tag Where Id=@Id",
                     new OdbcParameter("@Id", tag.Id));
            }
        }

        public async Task CreateEventsAsync(params IEvent[] archiveEvents)
        {
            foreach (var archiveEvent in archiveEvents)
            {
                if (CmdProviderType == DatabaseType.PostgreSql)
                {
                    var param = new List<OdbcParameter>();
                    param.Add(new OdbcParameter("peventtype", (int)archiveEvent.EventType));
                    param.Add(new OdbcParameter("peventactiontype", (int)archiveEvent.EventActionType));
                    param.Add(new OdbcParameter("pname", archiveEvent.Name));
                    param.Add(new OdbcParameter("prefreshspan", (int)archiveEvent.RefreshSpan.TotalMilliseconds));
                    param.Add(new OdbcParameter("pactiontext", archiveEvent.ActionText));
                    param.Add(new OdbcParameter("penabled", archiveEvent.Enabled ? 1 : 0));

                    var ldt = archiveEvent.LastChanged;
                    var dt = new DateTime(ldt.Year, ldt.Month, ldt.Day, ldt.Hour, ldt.Minute, ldt.Second);

                    param.Add(new OdbcParameter("plastchanged", dt));

                    //param.Add(new OdbcParameter("@LastChanged", archiveEvent.LastChanged));
                    param.Add(new OdbcParameter("pcomment", archiveEvent.Comment));

                    await CallProcedureAsync("Create_Event", param);
                    archiveEvent.Id = await GetIdAsync(archiveEvent.Name, "Event");

                    if (archiveEvent is DiscreteEvent)
                    {
                        var dEvent = archiveEvent as DiscreteEvent;
                        List<OdbcParameter> dParam = new List<OdbcParameter>();
                        dParam.Add(new OdbcParameter("pglobalvariableid", dEvent.GlobalVariableId));
                        dParam.Add(new OdbcParameter("peventid", dEvent.Id));
                        dParam.Add(new OdbcParameter("pedgetype", (int)dEvent.EdgeType));
                        await CallProcedureAsync("Create_DiscreteEvent", dParam);

                        dEvent.DiscreteEventId = await GetEventIdAsync(archiveEvent.Name, "DiscreteEventInfo",
                            "DiscreteEventId");

                    }
                    else if (archiveEvent is AnalogEvent)
                    {
                        var aEvent = archiveEvent as AnalogEvent;
                        var aParam = new List<OdbcParameter>();
                        aParam.Add(new OdbcParameter("pglobalvariableid", aEvent.GlobalVariableId));
                        aParam.Add(new OdbcParameter("peventid", aEvent.Id));
                        aParam.Add(new OdbcParameter("peventtriggertype",(int) aEvent.EventTriggerType));
                        aParam.Add(new OdbcParameter("ptriggervalue", aEvent.TriggerValue));
                        await CallProcedureAsync("Create_AnalogEvent", aParam);

                        aEvent.AnalogEventId = await GetEventIdAsync(archiveEvent.Name, "AnalogEventInfo", "AnalogEventId");

                    }
                    else if (archiveEvent is CyclicEvent)
                    {
                        var cEvent = archiveEvent as CyclicEvent;
                        var cParam = new List<OdbcParameter>();
                        cParam.Add(new OdbcParameter("peventid", cEvent.Id));
                        cParam.Add(new OdbcParameter("peventcycletype", cEvent.EventCycleType));
                        //cParam.Add(new OdbcParameter("pyears", cEvent.CycleStamp.Year));
                        //cParam.Add(new OdbcParameter("pmonths", cEvent.CycleStamp.Month));
                        cParam.Add(new OdbcParameter("pdays", cEvent.CycleStamp.Days));
                        cParam.Add(new OdbcParameter("phours", cEvent.CycleStamp.Hours));
                        cParam.Add(new OdbcParameter("pminutes", cEvent.CycleStamp.Minutes));
                        cParam.Add(new OdbcParameter("pseconds", cEvent.CycleStamp.Seconds));
                        cParam.Add(new OdbcParameter("pnextevent", cEvent.NextEvent));
                        await CallProcedureAsync("Create_CyclicEvent", cParam);

                        cEvent.CyclicEventId = await GetEventIdAsync(archiveEvent.Name, "CyclicEventInfo", "CyclicEventId");

                    }
                }
                else
                {
                    var param = new List<OdbcParameter>();
                    param.Add(new OdbcParameter("@EventType", archiveEvent.EventType));
                    param.Add(new OdbcParameter("@EventActionType", archiveEvent.EventActionType));
                    param.Add(new OdbcParameter("@Name", archiveEvent.Name));
                    param.Add(new OdbcParameter("@RefreshSpan", archiveEvent.RefreshSpan.TotalMilliseconds));
                    param.Add(new OdbcParameter("@ActionText", archiveEvent.ActionText));
                    param.Add(new OdbcParameter("@Enabled", archiveEvent.Enabled ? 1 : 0));

                    var ldt = archiveEvent.LastChanged;
                    var dt = new DateTime(ldt.Year, ldt.Month, ldt.Day, ldt.Hour, ldt.Minute, ldt.Second);

                    param.Add(new OdbcParameter("@LastChanged", dt));

                    //param.Add(new OdbcParameter("@LastChanged", archiveEvent.LastChanged));
                    param.Add(new OdbcParameter("@Comment", archiveEvent.Comment));

                    await CallProcedureAsync("Create_Event", param);
                    archiveEvent.Id = await GetIdAsync(archiveEvent.Name, "Event");

                    if (archiveEvent is DiscreteEvent)
                    {
                        var dEvent = archiveEvent as DiscreteEvent;
                        List<OdbcParameter> dParam = new List<OdbcParameter>();
                        dParam.Add(new OdbcParameter("@GlobalVariableId", dEvent.GlobalVariableId));
                        dParam.Add(new OdbcParameter("@EventId", dEvent.Id));
                        dParam.Add(new OdbcParameter("@EdgeType", dEvent.EdgeType));
                        await CallProcedureAsync("Create_DiscreteEvent", dParam);

                        dEvent.DiscreteEventId = await GetEventIdAsync(archiveEvent.Name, "DiscreteEventInfo",
                            "DiscreteEventId");

                    }
                    else if (archiveEvent is AnalogEvent)
                    {
                        var aEvent = archiveEvent as AnalogEvent;
                        var aParam = new List<OdbcParameter>();
                        aParam.Add(new OdbcParameter("@GlobalVariableId", aEvent.GlobalVariableId));
                        aParam.Add(new OdbcParameter("@EventId", aEvent.Id));
                        aParam.Add(new OdbcParameter("@EventTriggerType", aEvent.EventTriggerType));
                        aParam.Add(new OdbcParameter("@TriggerValue", aEvent.TriggerValue));
                        await CallProcedureAsync("Create_AnalogEvent", aParam);

                        aEvent.AnalogEventId = await GetEventIdAsync(archiveEvent.Name, "AnalogEventInfo", "AnalogEventId");

                    }
                    else if (archiveEvent is CyclicEvent)
                    {
                        var cEvent = archiveEvent as CyclicEvent;
                        var cParam = new List<OdbcParameter>();
                        cParam.Add(new OdbcParameter("@EventId", cEvent.Id));
                        cParam.Add(new OdbcParameter("@EventCycleType", cEvent.EventCycleType));
                        //cParam.Add(new OdbcParameter("@Years", cEvent.CycleStamp.Year));
                        //cParam.Add(new OdbcParameter("@Months", cEvent.CycleStamp.Month));
                        cParam.Add(new OdbcParameter("@Days", cEvent.CycleStamp.Days));
                        cParam.Add(new OdbcParameter("@Hours", cEvent.CycleStamp.Hours));
                        cParam.Add(new OdbcParameter("@Minutes", cEvent.CycleStamp.Minutes));
                        cParam.Add(new OdbcParameter("@Seconds", cEvent.CycleStamp.Seconds));
                        cParam.Add(new OdbcParameter("@NextEvent", cEvent.NextEvent));
                        await CallProcedureAsync("Create_CyclicEvent", cParam);

                        cEvent.CyclicEventId = await GetEventIdAsync(archiveEvent.Name, "CyclicEventInfo", "CyclicEventId");

                    }
                }
            }
        }

        public async Task DeleteEventsAsync(params IEvent[] events)
        {
            foreach (var ev in events)
            {
                await ExecutePreparedAsync("Delete From Event Where Id=@Id;",
                    new OdbcParameter("@Id", ev.Id));
            }
        }



        public async Task DeleteGlobalVariablesAsync(params GlobalVariable[] gvs)
        {
            foreach (var gv in gvs)
            {
                await ExecutePreparedAsync("Delete From GlobalVariable Where Id=@Id;"
                    , new OdbcParameter("@Id", gv.Id));
            }
        }




        public async Task UpdateEntitiesAsync(params EntityChangesInfo[] changes)
        {
           // var eusChanges = changes.Where(c => c.Entity is EngeneeringUnit &&
           //c.State == EntityState.Modified);
           // var eusToAdd = changes.Where(c => c.Entity is EngeneeringUnit &&
           // c.State == EntityState.Added).Select(c => c.Entity as EngeneeringUnit);
           // var eusToRemvoe = changes.Where(c => c.Entity is EngeneeringUnit &&
           // c.State == EntityState.Removed).Select(c => c.Entity as EngeneeringUnit);

           // await UpdateEusAsync(eusChanges);
           // await CreateEusAsync(eusToAdd.ToArray());
           // await DeleteEusAsync(eusToRemvoe.ToArray());

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
            await CreateArchivesAsync(archivesToAdd.ToArray());
            await DeleteArchivesAsync(archivesToRemove.ToArray());

            var tagsChanges = changes.Where(c => c.Entity is ITag &&
            c.State == EntityState.Modified);
            var tagsToAdd = changes.Where(c => c.Entity is ITag &&
            c.State == EntityState.Added).Select(c => c.Entity as ITag);
            var tagsToRemove = changes.Where(c => c.Entity is ITag &&
            c.State == EntityState.Removed).Select(c => c.Entity as ITag);

            await UpdateTagsAsync(tagsChanges);
            await CreateTagsAsync(tagsToAdd.ToArray());
            await DeleteTagsAsync(tagsToRemove.ToArray());

            var eventsChanges = changes.Where(c => c.Entity is IEvent &&
            c.State == EntityState.Modified);
            var eventsToAdd = changes.Where(c => c.Entity is IEvent &&
            c.State == EntityState.Added).Select(c => c.Entity as IEvent);
            var eventsToRemove = changes.Where(c => c.Entity is IEvent &&
            c.State == EntityState.Removed).Select(c => c.Entity as IEvent);

            await UpdateEventsAsync(eventsChanges);
            await CreateEventsAsync(eventsToAdd.ToArray());
            await DeleteEventsAsync(eventsToRemove.ToArray());

            var wideArchives = await GetWideArchives();
            foreach (var archive in wideArchives)
            {
                var tags = tagsToAdd.Where(t => t.TagArchiveId == archive.Key);
                await AddArchvieTagsAsync(archive.Value, tags);
            }
            foreach (var archive in wideArchives)
            {
                var tags = tagsToRemove.Where(t => t.TagArchiveId == archive.Key);
                await RemoveArchvieTagsAsync(archive.Value, tags);
            }



            var changedNames = changes.Where(c => c.Entity is ITag && c.Changes.Keys.Contains("Name")
               && c.Original != null);
            foreach (var item in changedNames)
            {
                var tag = item.Entity as ITag;
                await ChangeNamesAsync(tag.TagArchive.Name, item.Original["Name"].ToString(),
                    item.Changes["Name"].ToString(),tag.GlobalVariable.NetType);
            }

            var colTypeChanged = changes.Where(c => c.Entity is ITag && c.Changes.Keys
            .Contains("GlobalVariableId") && c.Original != null);

            foreach (var item in colTypeChanged)
            {
                var t = item.Entity as ITag;
                await ChangeDataType(t, (int)item.Original["GlobalVariableId"],
                    (int)item.Changes["GlobalVariableId"]);
            }
        }


        private async Task ChangeNamesAsync(string table, string oldName, string newName, Type t)
        {
            string query = (CommandProvider as IExtendedCmdProvider).ChangeNameCmd(table, oldName,newName, t);
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
                //string typeToChange = CommandProvider.MapToDbType(newType);
                string cmd = (CommandProvider as IExtendedCmdProvider).ChangeDtCmd(tag, eqType);
                await ExecuteNonQueryAsync(cmd);
            }

        }
        private string GetUpdateCmd(IDictionary<string, object> values, Type type)
        {
            return (CommandProvider as IExtendedCmdProvider).GetUpdateCmd(values, type);
        }

        private string GetUpdateCmd(IDictionary<string, object> values, Type baseType, Type derrivedType)
        {
            return (CommandProvider as IExtendedCmdProvider).GetUpdateCmd(values, baseType,derrivedType);
        }


        private async Task UpdateGlobalVariablesAsync(IEnumerable<EntityChangesInfo> gvsChanges)
        {
            foreach (var euChanges in gvsChanges)
            {
                string upEus = "UPDATE GlobalVariable ";
                string euCmdText = (CommandProvider as IExtendedCmdProvider).
                    GetUpdateCmd(euChanges.Changes, typeof(GlobalVariable));
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
                string upd = "UPDATE Tag SET LastChanged = " +
      //          $"{ConvertDateTime((item.Key as ITag).LastChanged)}" +
                    "WHERE Id=" +
                    +(item.Key as ITag).Id + ";";
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
                string upd = "UPDATE Event SET LastChanged = " +
             //   $"{ConvertDateTime((item.Key as IEvent).LastChanged)}" +
                    "WHERE Id=" +
                    +(item.Key as IEvent).Id + ";";
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


        public async Task CallProcedureAsync(string name, IEnumerable<DbParameter> parameters)
        {

            string pName = "{call " + name + "(";
            for (int i = 0; i < parameters.Count(); i++)
            {
                if (i < parameters.Count() - 1 && parameters.Count() > 1)
                {
                    pName += "?,";
                }
                else
                {
                    pName += "?)}";
                }
            }

            try
            {
                using (var conn = GetConnection())
                {
                    await conn.OpenAsync();

                    var cmd = conn.CreateCommand();
                    cmd.CommandText = pName;
                    foreach (var par in parameters)
                    {
                        if (CmdProviderType == DatabaseType.Oracle)
                        {
                            if (par.Value != null)
                                cmd.Parameters.AddWithValue(par.ParameterName, par.Value);
                            else
                                cmd.Parameters.AddWithValue(par.ParameterName, DBNull.Value);
                        }
                        else
                        {

                            if (par.Value != null)
                                cmd.Parameters.Add(par);
                            else
                                cmd.Parameters.Add(new OdbcParameter(par.ParameterName, DBNull.Value));
                        }
                    }

                    await cmd.ExecuteNonQueryAsync();
                }
            }
            catch (OdbcException e)
            {
                throw new ConnectionException(e.Message);
            }
           
        }
        public async Task CallProcedureAsync(string name, IEnumerable<DbParameter> parameters,CancellationToken token)
        {

            string pName = "{call " + name + "(";
            for (int i = 0; i < parameters.Count(); i++)
            {
                if (i < parameters.Count() - 1 && parameters.Count() > 1)
                {
                    pName += "?,";
                }
                else
                {
                    pName += "?)}";
                }
            }

            try
            {
                using (var conn = GetConnection())
                {
                    await conn.OpenAsync(token);

                    var cmd = conn.CreateCommand();
                    cmd.CommandText = pName;
                    foreach (var par in parameters)
                    {
                        if (CmdProviderType == DatabaseType.Oracle)
                        {
                            if (par.Value != null)
                                cmd.Parameters.AddWithValue(par.ParameterName, par.Value);
                            else
                                cmd.Parameters.AddWithValue(par.ParameterName, DBNull.Value);
                        }
                        else
                        {

                            if (par.Value != null)
                                cmd.Parameters.Add(par);
                            else
                                cmd.Parameters.Add(new OdbcParameter(par.ParameterName, DBNull.Value));
                        }
                    }

                    await cmd.ExecuteNonQueryAsync(token);
                }
            }
            catch (OdbcException e)
            {
                throw new ConnectionException(e.Message);
            }
            catch (TaskCanceledException) { }
        }

        public Task CallProcedureAsync(string name, params DbParameter[] parameters)
        {
            return CallProcedureAsync(name, (IEnumerable<DbParameter>)parameters);
        }

        public Task CallProcedureAsync(string name,CancellationToken token, params DbParameter[] parameters)
        {
            return CallProcedureAsync(name, (IEnumerable<DbParameter>)parameters,token);
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

        public async Task InsertEventSnapshotAsync(IEvent archiveEvent, params GlobalVariable[] gvs)
        {
            foreach (var gv in gvs)
            {
                if (CmdProviderType == DatabaseType.PostgreSql)
                {
                    var evid = new OdbcParameter("peventId", archiveEvent.Id);
                    var gvid = new OdbcParameter("pglobalVariableId", gv.Id);
                    var val = new OdbcParameter("pvariableValue", gv.CurrentValue);
                    //   val.SqlDbType = MapToDbType(gv.CurrentValue.GetType());
                    await CallProcedureAsync("Insert_EventSnapshot", evid, gvid, val);
                }
                else
                {
                    var evid = new OdbcParameter("@EventId", archiveEvent.Id);
                    var gvid = new OdbcParameter("@GlobalVariableId", gv.Id);
                    var val = new OdbcParameter("@VariableValue", gv.CurrentValue);
                    //   val.SqlDbType = MapToDbType(gv.CurrentValue.GetType());
                    await CallProcedureAsync("Insert_EventSnapshot", evid, gvid, val);
                }
            }
        }
        public async Task InsertEventSnapshotAsync(IEvent archiveEvent, params ITag[] tags)
        {
            foreach (var tag in tags)
            {
                if (CmdProviderType == DatabaseType.PostgreSql)
                {
                    var evid = new OdbcParameter("peventId", archiveEvent.Id);
                    var gvid = new OdbcParameter("ptagid", tag.Id);
                    var val = new OdbcParameter("pvariableValue", tag.CurrentValue);
                    //   val.SqlDbType = MapToDbType(gv.CurrentValue.GetType());
                    await CallProcedureAsync("Insert_EventSnapshot", evid, gvid, val);
                }
                else
                {
                    var evid = new OdbcParameter("@EventId", archiveEvent.Id);
                    var gvid = new OdbcParameter("@TagId", tag.Id);
                    var val = new OdbcParameter("@VariableValue", tag.CurrentValue);
                    //   val.SqlDbType = MapToDbType(gv.CurrentValue.GetType());
                    await CallProcedureAsync("Insert_EventSnapshot", evid, gvid, val);
                }
            }
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

        public Task<IEnumerable<string>> GetTableNamesAsync()
        {
            return Task.Run(() =>
            {
                var tableNames = new List<string>();

                try
                {
                    using (var conn = new OdbcConnection(ConnectionString))
                    {
                        conn.Open();
                        var dt = conn.GetSchema("Tables");
                        foreach (DataRow dr in dt.Rows)
                        {
                            if(!dr["TABLE_TYPE"].Equals("SYSTEM TABLE"))
                                tableNames.Add(dr["TABLE_NAME"].ToString());
                        }

                    }
                }
                catch (OdbcException e)
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
             //   SqlConnectionStringBuilder csb = new SqlConnectionStringBuilder(ConnectionString);
              //  string db = csb.InitialCatalog;
                try
                {
                    using (var conn = GetConnection())
                    {
                        conn.Open();
                        DataTable dt = conn.GetSchema("Columns", new[] { null, null, tableName });
                        columns = new ColumnSchema[dt.Rows.Count];

                        int index = 0;

                        

                        foreach (DataRow dr in dt.Rows)
                        {
                            columns[index] = new ColumnSchema(dr["COLUMN_NAME"].ToString());
                            columns[index].DataType = MapTypeFromDb(dr["TYPE_NAME"].ToString());
                            columns[index].IsNullable = dr["IS_NULLABLE"].ToString() == "true" ? true : false;
                            //if (columns[index].DataType == typeof(string))
                            //    columns[index].MaxLength = (int)dr["CHARACTER_MAXIMUM_LENGTH"];
                            index++;

                        }

                    }
                }
                catch (OdbcException e)
                {
                    throw new ConnectionException(e.Message);
                }

                return (IEnumerable<ColumnSchema>)columns;
            });
        }

        private static Type MapTypeFromDb(string type)
        {
            var t = type.ToLowerInvariant();

            if (t.Contains("int"))
                return typeof(int);
            else if (t.Contains("char") || t.Contains("text"))
                return typeof(string);
            else if (t.Contains("float") || t.Contains("double"))
                return typeof(double);
            else if (t.Contains("datetime") || t.Contains("timestamp"))
                return typeof(DateTime);
            else
                return typeof(Object);

            //throw new ArgumentException("Unsupported data type!");



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
                t.TagArchive != null && t.TagArchive.Name.Equals(archive))
                    .FirstOrDefault();

                    string cmd = (CommandProvider as IExtendedCmdProvider).
                        InsertSummaryCmd(archiveEvent, tag, action, fd);
                    await ExecuteNonQueryAsync(cmd);
                
            }
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
