using ProcessDataArchiver.DataCore.Database.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.OleDb;
using System.IO;
using ProcessDataArchiver.DataCore.Database.CommandProviders;
using FirebirdSql.Data.Client;
using FirebirdSql.Data.FirebirdClient;
using System.Data.Common;
using MySql.Data.MySqlClient;
using Oracle.ManagedDataAccess.Client;
using Npgsql;
using System.Data.SqlClient;
using System.Data;
using ProcessDataArchiver.DataCore.Infrastructure;
using Microsoft.Win32;
using System.Threading;

namespace ProcessDataArchiver.DataCore.Database.DbProviders
{
    public sealed class DatabaseProviderFactory
    {
        public static Dictionary<DatabaseType, ConnectionSettings> DefaultSettings { get; set; }


    
        #region Creating



        public static IDatabaseProvider CreateProvider(DatabaseType type, string connStr)
        {
            switch (type)
            {
                case DatabaseType.Access:
                    return new AccessDatabaseProvider(connStr, new AccessCommandProvider());
                case DatabaseType.Firebird:
                    return new FirebirdDatabaseProvider(connStr, new FirebirdCommandProvider());
                case DatabaseType.MySql:
                    return new MySqlDatabaseProvider(connStr, new MySqlCommandProvider());
                case DatabaseType.Oracle:
                    return new OracleDatabaseProvider(connStr, new OracleCommandProvider());
                case DatabaseType.PostgreSql:
                    return new PostgresDatabaseProvider(connStr, new PostgresCommandProvider());
                case DatabaseType.SqlServer:
                    return new SqlServerDatabaseProvider(connStr, new SqlServerCommandProvider());
                case DatabaseType.ODBC:
                    return null;
                default:
                    return null;
            }
        }



        public static IDatabaseProvider CreateProvider(DatabaseType type, string connStr,DatabaseType cmdProvType)
        {
            switch (type)
            {
                case DatabaseType.Access:
                    return new AccessDatabaseProvider(connStr, new AccessCommandProvider());
                case DatabaseType.Firebird:
                    return new FirebirdDatabaseProvider(connStr, new FirebirdCommandProvider());
                case DatabaseType.MySql:
                    return new MySqlDatabaseProvider(connStr, new MySqlCommandProvider());
                case DatabaseType.Oracle:
                    return new OracleDatabaseProvider(connStr, new OracleCommandProvider());
                case DatabaseType.PostgreSql:
                    return new PostgresDatabaseProvider(connStr, new PostgresCommandProvider());
                case DatabaseType.SqlServer:
                    return new SqlServerDatabaseProvider(connStr, new SqlServerCommandProvider());
                case DatabaseType.ODBC:

                    switch (cmdProvType)
                    {
                        case DatabaseType.Access:
                            return new OdbcProvider(DatabaseType.ODBC, new AccessCommandProvider(), 
                                null, connStr);
                        case DatabaseType.Firebird:
                            return new OdbcProvider(DatabaseType.ODBC, new FirebirdCommandProvider(),
                                null, connStr);
                        case DatabaseType.MySql:
                            return new OdbcProvider(DatabaseType.ODBC, new MySqlCommandProvider(),
                                null, connStr);
                        case DatabaseType.Oracle:
                            return new OdbcProvider(DatabaseType.ODBC, new OracleCommandProvider(),
                                null, connStr);
                        case DatabaseType.PostgreSql:
                            return new OdbcProvider(DatabaseType.ODBC, new PostgresCommandProvider(),
                                null, connStr);
                        case DatabaseType.SqlServer:
                            return new OdbcProvider(DatabaseType.ODBC, new SqlServerCommandProvider(),
                                null, connStr);

                    }

                    return null;
                default:
                    return null;
            }
        }


        private static  IDatabaseProvider CreateAccessProvider(ConnectionSettings settings)
        {
            bool fileExists = false;

            fileExists = File.Exists(settings.Database);

            var cs = GetAccessConnecionString(settings);

            if (!fileExists)
            {
                ADOX.Catalog cat = new ADOX.Catalog();

                cat.Create(cs);

                ADODB.Connection con =
                        cat.ActiveConnection as ADODB.Connection;
                if (con != null)
                    con.Close();
            }
            return new AccessDatabaseProvider(cs, new AccessCommandProvider());
            
        }

        private static  Task<IDatabaseProvider> CreateAccessProviderAsync(ConnectionSettings settings,CancellationToken ct)
        {
            bool fileExists = false;

            return Task.Run(() =>
            {
                fileExists = File.Exists(settings.Database);
                var cs = GetAccessConnecionString(settings);

                if (!fileExists)
                {
                    ADOX.Catalog cat = new ADOX.Catalog();
                    if(!ct.IsCancellationRequested)
                        cat.Create(cs);

                    ADODB.Connection con =
                            cat.ActiveConnection as ADODB.Connection;
                    if (con != null)
                        con.Close();
                }
                var prov = new AccessDatabaseProvider(cs, new AccessCommandProvider());
                return (IDatabaseProvider)prov;
            });
        }


        private static IDatabaseProvider CreateFirebirdProvider(ConnectionSettings settings)
        {
            bool fileExists = File.Exists(settings.Database);
            var cs = GetFirebirdConnectionString(settings);

            if (!fileExists)
            {
                try
                {
                    FbConnection.CreateDatabase(cs);
                }
                catch(FbException ex)
                {
                    throw new ConnectionException(ex.Message);
                }
            }
           
            return new FirebirdDatabaseProvider(cs, new FirebirdCommandProvider());

        }

        private static  Task<IDatabaseProvider> CreateFirebirdProviderAsync(ConnectionSettings settings,CancellationToken ct)
        {
            return Task.Run(() =>
            {
                bool fileExists = File.Exists(settings.Database);
                var cs = GetFirebirdConnectionString(settings);

                if (!fileExists)
                {
                    try
                    {
                        if(!ct.IsCancellationRequested)
                            FbConnection.CreateDatabase(cs);
                    }
                    catch (FbException ex)
                    {
                        throw new ConnectionException(ex.Message);
                    }
                }
                var prov = new FirebirdDatabaseProvider(cs, new FirebirdCommandProvider());
                return (IDatabaseProvider)prov;
            });
        }


        private static IDatabaseProvider CreateMySqlProvider(ConnectionSettings settings)
        {
            string cs = GetMySqlConnectionString(settings,false);

            IEnumerable<string> dbNames = GetMySqlDatabaseNames(settings)
                .Select(s=>s.ToLowerInvariant());
            
            bool exists = dbNames.Contains(settings.Database.ToLowerInvariant());
            settings.Database = settings.Database.ToLower();
            if (!exists)
            {
                try
                {
                    using(var conn = new MySqlConnection(cs))
                    {
                        conn.Open();
                        var cmdCreate = conn.CreateCommand();
                        cmdCreate.CommandText = "CREATE DATABASE " + settings.Database + ";";
                        cmdCreate.ExecuteNonQuery();

                        var cmdUse = conn.CreateCommand();
                        cmdUse.CommandText = "USE " + settings.Database + ";";
                        cmdUse.ExecuteNonQuery();
                    }
                }
                catch(MySqlException e)
                {
                    throw new ConnectionException(e.Message);
                }
            }

            var builder = new MySqlConnectionStringBuilder(cs);
            builder.Database = settings.Database;

            return new MySqlDatabaseProvider(builder.ConnectionString, new MySqlCommandProvider());
        }

        private static async Task<IDatabaseProvider> CreateMySqlProviderAsync(ConnectionSettings settings,CancellationToken ct)
        {
            
            string cs = GetMySqlConnectionString(settings, false);

            IEnumerable<string> dbNames = GetMySqlDatabaseNames(settings)
                .Select(s => s.ToLowerInvariant());

            bool exists = dbNames.Contains(settings.Database.ToLowerInvariant());
            settings.Database = settings.Database.ToLower();
            if (!exists)
            {
                try
                {
                    using (var conn = new MySqlConnection(cs))
                    {
                        await conn.OpenAsync(ct);
                        var cmdCreate = conn.CreateCommand();
                        cmdCreate.CommandText = "CREATE DATABASE " + settings.Database + ";";
                        await cmdCreate.ExecuteNonQueryAsync(ct);

                        var cmdUse = conn.CreateCommand();
                        cmdUse.CommandText = "USE " + settings.Database + ";";
                        await cmdUse.ExecuteNonQueryAsync(ct);
                    }
                }
                catch (MySqlException e)
                {
                    throw new ConnectionException(e.Message);
                }
            }

            var builder = new MySqlConnectionStringBuilder(cs);
            builder.Database = settings.Database;

            return new MySqlDatabaseProvider(builder.ConnectionString, new MySqlCommandProvider());
        }


        private static IDatabaseProvider CreateOracleProvider(ConnectionSettings settings)
        {
            string cs = GetOracleConnectionString(settings);
            return new OracleDatabaseProvider(cs, new OracleCommandProvider());
        }

        private static async Task<IDatabaseProvider> CreateOracleProviderAsync(ConnectionSettings settings,CancellationToken ct)
        {
            string cs = GetOracleConnectionString(settings);
            return await Task.Run(() => {
                var prov = new OracleDatabaseProvider(cs, new OracleCommandProvider());
                return (IDatabaseProvider)prov;
            }) ;
        }

        private static IDatabaseProvider CreatePostgresProvider(ConnectionSettings settings)
        {
            string cs = GetPostgresConnectionString(settings, false);
            IEnumerable<string> dbNames = GetPostgresDatabaseNames(settings)
                .Select(s=>s.ToLowerInvariant());

            bool exists = dbNames.Contains(settings.Database.ToLowerInvariant());

            if (!exists)
            {
                try
                {
                    using(var conn = new NpgsqlConnection(cs))
                    {
                        conn.Open();
                        var cmdCreate = conn.CreateCommand();
                        cmdCreate.CommandText = "CREATE DATABASE " + settings.Database + ";";
                        cmdCreate.ExecuteNonQuery();

                    }
                }
                catch(NpgsqlException e)
                {
                    throw new ConnectionException(e.Message);
                }
            }
            return new PostgresDatabaseProvider(GetPostgresConnectionString(settings), 
                new PostgresCommandProvider());
        }

        private static async Task<IDatabaseProvider> CreatePostgresProviderAsync(ConnectionSettings settings,CancellationToken ct)
        {
            string cs = GetPostgresConnectionString(settings, false);
            IEnumerable<string> dbNames = GetPostgresDatabaseNames(settings)
                .Select(s => s.ToLowerInvariant());

            bool exists = dbNames.Contains(settings.Database.ToLowerInvariant());

            if (!exists)
            {
                try
                {
                    using (var conn = new NpgsqlConnection(cs))
                    {
                        await conn.OpenAsync(ct);
                        var cmdCreate = conn.CreateCommand();
                        cmdCreate.CommandText = "CREATE DATABASE " + settings.Database + ";";
                        await cmdCreate.ExecuteNonQueryAsync(ct);

                    }
                }
                catch (NpgsqlException e)
                {
                    throw new ConnectionException(e.Message);
                }
            }
            return new PostgresDatabaseProvider(GetPostgresConnectionString(settings),
                new PostgresCommandProvider());
        }



        private static IDatabaseProvider CreateSqlServerProvider(ConnectionSettings settings)
        {
            string cs = GetSqlServerConnectionString(settings,false);
            ConnectionSettings sett = (ConnectionSettings)settings.Clone();
            sett.Database = null;
            try
            {
                var dbs = GetSqlServerDatabaseNames(sett).Select(d => d.ToLowerInvariant()).ToList();
                if (FileHelper.IsValidPath(settings.Database) && Path.GetExtension(settings.Database) == ".mdf")
                {
                    string dbPath = settings.Database;
                    string dbName = Path.GetFileNameWithoutExtension(dbPath);

                    if (!File.Exists(dbPath) && !dbs.Contains(dbName.ToLowerInvariant()))
                    {

                        using (var conn = new SqlConnection(cs))
                        {
                            conn.Open();

                            var cmdCreate = conn.CreateCommand();
                            cmdCreate.CommandText = "CREATE DATABASE [" + dbName + "] ON PRIMARY " +
                                $"(NAME = '{dbName}', FILENAME = '{dbPath}')";//+
                                                                              //   " LOG ON " +
                                                                              //     $"(NAME = N'{dbName}_Log', FILENAME = N'{logPath}')";
                            cmdCreate.ExecuteNonQuery();
                            sett.Database = dbName;
                        }
                    }
                    else if (dbs.Contains(dbName.ToLowerInvariant()))
                    {
                        sett.Database = dbName;
                    }

                }
                else
                {
                    bool exists = dbs.Contains(settings.Database.ToLowerInvariant());

                    if (!exists)
                    {
                            using (var conn = new SqlConnection(cs))
                            {
                                conn.Open();
                                var cmdCreate = conn.CreateCommand();
                                cmdCreate.CommandText = $"CREATE DATABASE [{settings.Database}];";
                                cmdCreate.ExecuteNonQuery();
                            }
                    }
                    sett.Database = settings.Database;
                }
            }
            catch(SqlException ex)
            {
                throw new ConnectionException(ex.Message);
            }
            catch (IOException exx)
            {
                throw new ConnectionException(exx.Message);
            }

            return new SqlServerDatabaseProvider(GetSqlServerConnectionString(sett),
                new SqlServerCommandProvider());
        }


        private static async Task<IDatabaseProvider> CreateSqlServerProviderAsync(ConnectionSettings settings,CancellationToken ct)
        {
            string cs = GetSqlServerConnectionString(settings, false);
            ConnectionSettings sett = (ConnectionSettings)settings.Clone();
            sett.Database = null;
            try
            {
                var dbs = GetSqlServerDatabaseNames(sett).Select(d => d.ToLowerInvariant()).ToList();
                if (FileHelper.IsValidPath(settings.Database) && Path.GetExtension(settings.Database) == ".mdf")
                {
                    string dbPath = settings.Database;
                    string dbName = Path.GetFileNameWithoutExtension(dbPath);

                    if (!File.Exists(dbPath) && !dbs.Contains(dbName.ToLowerInvariant()))
                    {

                        using (var conn = new SqlConnection(cs))
                        {
                            await conn.OpenAsync(ct);

                            var cmdCreate = conn.CreateCommand();
                            cmdCreate.CommandText = "CREATE DATABASE [" + dbName + "] ON PRIMARY " +
                                $"(NAME = '{dbName}', FILENAME = '{dbPath}')";//+
                                                                              //   " LOG ON " +
                                                                              //     $"(NAME = N'{dbName}_Log', FILENAME = N'{logPath}')";
                            await cmdCreate.ExecuteNonQueryAsync(ct);
                            sett.Database = dbName;
                        }
                    }
                    else if (dbs.Contains(dbName.ToLowerInvariant()))
                    {
                        sett.Database = dbName;
                    }

                }
                else
                {
                    bool exists = dbs.Contains(settings.Database.ToLowerInvariant());

                    if (!exists)
                    {
                        using (var conn = new SqlConnection(cs))
                        {
                            await conn.OpenAsync(ct);
                            var cmdCreate = conn.CreateCommand();
                            cmdCreate.CommandText = $"CREATE DATABASE [{settings.Database}];";
                            await cmdCreate.ExecuteNonQueryAsync(ct);
                        }
                    }
                    sett.Database = settings.Database;
                }
            }
            catch (SqlException ex)
            {
                throw new ConnectionException(ex.Message);
            }
            catch (IOException exx)
            {
                throw new ConnectionException(exx.Message);
            }

            return new SqlServerDatabaseProvider(GetSqlServerConnectionString(sett),
                new SqlServerCommandProvider());
        }

        #endregion

        #region  ConnectionStrings
        private static string GetAccessConnecionString(ConnectionSettings settings)
        {
            var builder = new OleDbConnectionStringBuilder();
            string extension = Path.GetExtension(settings.Database);

            if (extension == ".mdb")
                builder.Provider = "Microsoft.Jet.OLEDB.4.0";
            else if (extension == ".accdb")
                builder.Provider = "Microsoft.ACE.OLEDB.12.0";
            else
                throw new System.IO.FileFormatException("Unsupported file type!");

            builder.DataSource = settings.Database;

            if (!string.IsNullOrEmpty(settings.Password))
                builder.Add("Jet OLEDB:Database Password", settings.Password);

            return builder.ConnectionString;
        }

        private static string GetFirebirdConnectionString(ConnectionSettings settings)
        {
            string ext = Path.GetExtension(settings.Database).ToLowerInvariant();
            if (!FileHelper.IsValidPath(settings.Database) || ext != ".fdb")
                throw new FileFormatException("Unsupported file type");
            var builder = new FbConnectionStringBuilder();
            builder.Database = settings.Database;
            builder.DataSource = settings.DataSource ?? "localhost";
            builder.UserID = settings.User;
            builder.Password = settings.Password;
            builder.Port = settings.Port == 0 ? 3050 : settings.Port;

            return builder.ConnectionString;
        }

        private static string GetMySqlConnectionString(ConnectionSettings settings, bool includeDb = true)
        {
            var builder = new MySqlConnectionStringBuilder();
            if(includeDb)
                builder.Database = settings.Database;
            builder.Server = settings.DataSource ?? "localhost";
            builder.UserID = settings.User;
            builder.Password = settings.Password;
            builder.Port = settings.Port == 0 ? 3306 : (uint)settings.Port;

            return builder.ConnectionString;
        }

        private static string GetOracleConnectionString(ConnectionSettings settings)
        {
            var builder = new OracleConnectionStringBuilder();

            if (!string.IsNullOrEmpty(settings.Service))
            {
                string server = settings.DataSource ?? "localhost";
                int port = settings.Port == 0 ? 1521 : settings.Port;
                
                builder.DataSource = $"(DESCRIPTION = (ADDRESS = (PROTOCOL = TCP)(HOST = {settings.DataSource})" +
                $"(PORT = {port}))(CONNECT_DATA = (SERVICE_NAME ={settings.Service})))";
            }
            else
            {
                builder.DataSource = settings.Database;
            }
            builder.UserID = settings.User;
            builder.Password = settings.Password;

            return builder.ConnectionString;
        }

        private static string GetPostgresConnectionString(ConnectionSettings settings, bool includeDb = true)
        {
            var builder = new NpgsqlConnectionStringBuilder();
            builder.Host = settings.DataSource ?? "localhost";
            if(includeDb)
                builder.Database = settings.Database;
            builder.Port = settings.Port == 0 ? 5432 : settings.Port;
            builder.Username = settings.User;
            builder.Password = settings.Password;

            return builder.ConnectionString;
        }

        private static string GetSqlServerConnectionString(ConnectionSettings settings, bool includeDb = true)
        {
            var builder = new SqlConnectionStringBuilder();
            builder.DataSource = settings.DataSource ?? "localhost";
            if(string.IsNullOrEmpty(settings.User) || string.IsNullOrEmpty(settings.Password))
            {
                builder.IntegratedSecurity = true;            
            }
            else
            {
                builder.UserID = settings.User;
                builder.Password = settings.Password;
            }
            if (settings.Database != null && File.Exists(settings.Database) && includeDb)
                builder.AttachDBFilename = settings.Database;
            
            else if(settings.Database != null && includeDb)
                builder.InitialCatalog = settings.Database;           
            
            return builder.ConnectionString;
        }

        #endregion

        #region DefaultSettings

        static DatabaseProviderFactory()
        {
            DefaultSettings = new Dictionary<DatabaseType, ConnectionSettings>();

            //Access
            DefaultSettings.Add(DatabaseType.Access, new ConnectionSettings
            {
                Database = "C:\\NowaBazaDanych1.mdb"
            });

            //Firebird
            DefaultSettings.Add(DatabaseType.Firebird, new ConnectionSettings
            {
                DataSource = "localhost",
                Database = "C:\\NowaBazaDanych1.fdb",
                Port = 3050,
            });

            //MySql
            DefaultSettings.Add(DatabaseType.MySql, new ConnectionSettings
            {
                DataSource = "localhost",
                Database = "NowaBazaDanych1",
                Port = 3306
            });

            //Oracle

            var enumerator = new OracleDataSourceEnumerator();
            DataTable dt = enumerator.GetDataSources();
            DataRow dr = dt.Rows[0];
            if (dr != null)
            {
               DefaultSettings.Add(DatabaseType.Oracle,new ConnectionSettings
                {
                   User = "SYSDBA",
                    DataSource = dr["ServerName"].ToString(),
                    Database = dr["InstanceName"].ToString(),
                    Service = dr["ServiceName"].ToString(),
                    Port = Int32.Parse((dr["Port"].ToString()))
                });
            }
            else
            {
                DefaultSettings.Add(DatabaseType.Oracle, new ConnectionSettings
                {
                    DataSource = "localhost",
                    Database = "OracleDB",
                    Service = "ORCL",
                    Port = 1521
                });
            }

            //Postgres
            DefaultSettings.Add(DatabaseType.PostgreSql, new ConnectionSettings
            {
                User = "postgres",
                DataSource = "localhost",
                Port = 5432,
                Database = "postgres"
            });

            //SqlServer
            DefaultSettings.Add(DatabaseType.SqlServer, new ConnectionSettings
            {
                DataSource = "localhost",
                Database = "NowaBazaDanych1"
            });
        }


        #endregion

        public static IDatabaseProvider CreateProvider(DatabaseType type, DbConnectionStringBuilder builder)
        {
            string cs = builder.ConnectionString;
            switch (type)
            {
                case DatabaseType.Access:
                    return new AccessDatabaseProvider
                    {
                        CommandProvider = new AccessCommandProvider(),
                        ConnectionString = cs
                    };
                case DatabaseType.Firebird:
                    return new FirebirdDatabaseProvider
                    {
                        CommandProvider = new FirebirdCommandProvider(),
                        ConnectionString = cs
                    };
                case DatabaseType.MySql:
                    return new MySqlDatabaseProvider
                    {
                        CommandProvider = new MySqlCommandProvider(),
                        ConnectionString = cs
                    };
                case DatabaseType.Oracle:
                    return new OracleDatabaseProvider
                    {
                        CommandProvider = new OracleCommandProvider(),
                        ConnectionString = cs
                    };
                case DatabaseType.PostgreSql:
                    return new PostgresDatabaseProvider
                    {
                        CommandProvider = new PostgresCommandProvider(),
                        ConnectionString = cs
                    };
                case DatabaseType.SqlServer:
                    return new SqlServerDatabaseProvider
                    {
                        CommandProvider = new SqlServerCommandProvider(),
                        ConnectionString = cs
                    };
                default:
                    throw new ArgumentException("Unsupported database type!");
            }
        }

        public static IDatabaseProvider CreateProvider(DatabaseType type, ConnectionSettings settings)
        {
            switch (type)
            {
                case DatabaseType.Access:
                    return CreateAccessProvider(settings);                   
                case DatabaseType.Firebird:
                    return CreateFirebirdProvider(settings);
                case DatabaseType.MySql:
                    return CreateMySqlProvider(settings);
                case DatabaseType.Oracle:
                    return CreateOracleProvider(settings);
                case DatabaseType.PostgreSql:
                    return CreatePostgresProvider(settings);
                case DatabaseType.SqlServer:
                    return CreateSqlServerProvider(settings);
                default:
                    throw new ArgumentException("Unsupported database type!");                  
            }          
        }

        public static Task<IDatabaseProvider> CreateProviderAsync(DatabaseType type, ConnectionSettings settings,CancellationToken ct)
        {
            switch (type)
            {
                case DatabaseType.Access:
                    return CreateAccessProviderAsync(settings,ct);
                case DatabaseType.Firebird:
                    return CreateFirebirdProviderAsync(settings,ct);
                case DatabaseType.MySql:
                    return CreateMySqlProviderAsync(settings,ct);
                case DatabaseType.Oracle:
                    return CreateOracleProviderAsync(settings,ct);
                case DatabaseType.PostgreSql:
                    return CreatePostgresProviderAsync(settings,ct);
                case DatabaseType.SqlServer:
                    return CreateSqlServerProviderAsync(settings,ct);
                default:
                    throw new ArgumentException("Unsupported database type!");
            }
        }

        public static IDatabaseProvider CreateOdbcProvider(string dsn,string driver, string user, string password)
        {
            var type = MapDriverToType(driver);
            string connStr = GetOdbcConnectionString(dsn, user, password);
            return CreateOdbcProvider(type, connStr, driver);
        }

        public static IDatabaseProvider CreateOdbcProvider(DatabaseType type,string connStr, string driver)
        {
            switch (type)
            {
                case DatabaseType.Access:
                    return new OdbcProvider(DatabaseType.ODBC,
                        new AccessCommandProvider(), driver, connStr)
                    { CmdProviderType = DatabaseType.Access };
                case DatabaseType.Firebird:
                    return new OdbcProvider(DatabaseType.ODBC,
                        new FirebirdCommandProvider(), driver, connStr)
                    { CmdProviderType = DatabaseType.Firebird };
                case DatabaseType.MySql:
                    return new OdbcProvider(DatabaseType.ODBC,
                        new MySqlCommandProvider(), driver, connStr)
                    { CmdProviderType = DatabaseType.MySql };
                case DatabaseType.Oracle:
                    return new OdbcProvider(DatabaseType.ODBC,
                         new OracleCommandProvider(), driver, connStr)
                    { CmdProviderType = DatabaseType.Oracle };
                case DatabaseType.PostgreSql:
                    return new OdbcProvider(DatabaseType.ODBC,
                        new PostgresCommandProvider(), driver, connStr)
                    {   CmdProviderType = DatabaseType.PostgreSql };

                case DatabaseType.SqlServer:
                    return new OdbcProvider(DatabaseType.ODBC,
                        new SqlServerCommandProvider(), driver, connStr)
                    { CmdProviderType = DatabaseType.SqlServer };
                default:
                    throw new ArgumentException("Unsupported database type!");
            }
        }

         


        public static IDatabaseProvider CreateOdbcProvider(DatabaseType type, OdbcDataSourceInfo info)
        {
            string connStr = GetOdbcConnectionString(info.Dsn);
            return CreateOdbcProvider(type, connStr, info.Driver);
        }


        private static string GetOdbcConnectionString(string dsn, string user ="",
            string password = "")
        {
            if (!string.IsNullOrEmpty(password))           
                return $"Dsn={dsn};Uid={user};Pwd={password};";
            else
                return $"Dsn={dsn}";            
        }
        



        private static DatabaseType MapDriverToType(string driver)
        {
            string drv = driver.ToLowerInvariant();

            if (drv.Contains("access"))
                return DatabaseType.Access;
            else if (drv.Contains("firebird"))
                return DatabaseType.Firebird;
            else if (drv.Contains("mysql"))
                return DatabaseType.MySql;
            else if (drv.Contains("oracle"))
                return DatabaseType.Oracle;
            else if (drv.Contains("postgres"))
                return DatabaseType.PostgreSql;
            else if (drv.Contains("sql server"))
                return DatabaseType.SqlServer;
            else
                throw new ArgumentException("Unsupported driver type!");
        }




        public static string GetConnectionString(DatabaseType type, ConnectionSettings settings)
        {
            switch (type)
            {
                case DatabaseType.Access:
                    return GetAccessConnecionString(settings);
                case DatabaseType.Firebird:
                    return GetFirebirdConnectionString(settings);
                case DatabaseType.MySql:
                    return GetMySqlConnectionString(settings);
                case DatabaseType.Oracle:
                    return GetOracleConnectionString(settings);
                case DatabaseType.PostgreSql:
                    return GetPostgresConnectionString(settings);
                case DatabaseType.SqlServer:
                    return GetSqlServerConnectionString(settings);
                case DatabaseType.ODBC:
                    return null;
                default:
                    return null;
            }
        }

        public static IEnumerable<string> GetDatabaseNames(DatabaseType type, ConnectionSettings settings)
        {

            switch (type)
            {
                case DatabaseType.Access:
                    return GetAccessDatabaseNames(settings);
                case DatabaseType.Firebird:
                    return GetFirebirdDatabaseNames(settings);
                case DatabaseType.MySql:
                    return GetMySqlDatabaseNames(settings);
                case DatabaseType.Oracle:
                    return GetOracleDatabaseNames();
                case DatabaseType.PostgreSql:
                    return GetPostgresDatabaseNames(settings);
                case DatabaseType.SqlServer:
                    return GetSqlServerDatabaseNames(settings);
                default:
                    throw new ArgumentException("Unsupported database type!");
            }

            throw new ArgumentException("Unsupported database type!");
        }

        public static IEnumerable<string> GetDatabaseNames(DatabaseType type, ConnectionSettings settings,CancellationToken token)
        {

            switch (type)
            {
                case DatabaseType.Access:
                    return GetAccessDatabaseNames(settings);
                case DatabaseType.Firebird:
                    return GetFirebirdDatabaseNames(settings);
                case DatabaseType.MySql:
                    return GetMySqlDatabaseNames(settings);
                case DatabaseType.Oracle:
                    return GetOracleDatabaseNames();
                case DatabaseType.PostgreSql:
                    return GetPostgresDatabaseNames(settings);
                case DatabaseType.SqlServer:
                    return GetSqlServerDatabaseNames(settings);
                default:
                    throw new ArgumentException("Unsupported database type!");
            }

            throw new ArgumentException("Unsupported database type!");
        }




        public static Task<IEnumerable<string>> GetDatabaseNamesAsync(DatabaseType type, ConnectionSettings settings)
        {
            return Task.Run(() => GetDatabaseNames(type, settings));
        }

        public static Task<IEnumerable<string>> GetDatabaseNamesAsync(DatabaseType type, ConnectionSettings settings,CancellationToken token)
        {
            switch (type)
            {
                case DatabaseType.Access:
                    return Task.Run(() => {return GetAccessDatabaseNames(settings); });
                case DatabaseType.Firebird:
                    return Task.Run(() => { return GetFirebirdDatabaseNames(settings); });
                case DatabaseType.MySql:
                    return GetMySqlDatabaseNamesAsync(settings,token);
                case DatabaseType.Oracle:
                    return Task.Run(() => { return GetOracleDatabaseNames(); });
                case DatabaseType.PostgreSql:
                    return GetPostgresDatabaseNamesAsync(settings,token);
                case DatabaseType.SqlServer:
                    return GetSqlServerDatabaseNamesAsync(settings,token);
                default:
                    throw new ArgumentException("Unsupported database type!");
            }

            throw new ArgumentException("Unsupported database type!");
        }

        #region DatabaseNames

        private static IEnumerable<string> GetAccessDatabaseNames(ConnectionSettings settings)
        {

            string dbPath = Path.GetDirectoryName(settings.Database);

            foreach (string filePath in Directory.GetFiles(dbPath))
            {
                string extension = Path.GetExtension(filePath);
                if (extension == ".mdb" || extension == ".accdb")
                    yield return Path.GetFileName(filePath);
            }

        }

        private static IEnumerable<string> GetFirebirdDatabaseNames(ConnectionSettings settings)
        {
            string dbPath = Path.GetDirectoryName(settings.Database);

            foreach (string filePath in Directory.GetFiles(dbPath))
            {
                string extension = Path.GetExtension(filePath);
                if (extension == ".fdb")
                    yield return Path.GetFileName(filePath);
            }
        }


        private static IEnumerable<string> GetMySqlDatabaseNames(ConnectionSettings settings)
        {

            string connectionString = GetMySqlConnectionString(settings,false);
            List<string> dbNames = new List<string>();

            try
            {
                using(var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = "SHOW DATABASES";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            dbNames.Add(reader[0].ToString());
                        }
                    }
                }
            }
            catch(MySqlException e)
            {
                throw new ConnectionException(e.Message);
            }

            return dbNames;

        }

        private async static Task<IEnumerable<string>> GetMySqlDatabaseNamesAsync(ConnectionSettings settings,
            CancellationToken ct)
        {

            string connectionString = GetMySqlConnectionString(settings, false);
            List<string> dbNames = new List<string>();

            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    await Task.Run(async() => {await conn.OpenAsync(ct); });
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = "SHOW DATABASES";
                    using (var reader = await cmd.ExecuteReaderAsync(ct))
                    {
                        while (await reader.ReadAsync(ct))
                        {
                            dbNames.Add(reader[0].ToString());
                        }
                    }
                }
            }
            catch (MySqlException e)
            {
                throw new ConnectionException(e.Message);
            }

            return dbNames;
        }



        private static IEnumerable<string> GetOracleDatabaseNames()
        {
            var oraEnum = new OracleDataSourceEnumerator();
            var dt = oraEnum.GetDataSources();

            foreach (DataRow item in dt.Rows)
            {
                yield return item["InstanceName"].ToString();
            }
        }

        private static IEnumerable<string> GetPostgresDatabaseNames(ConnectionSettings settings)
        {
            string connectionString = GetPostgresConnectionString(settings,false);
            IList<string> dbNames = new List<string>();
            try
            {
                using(var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    NpgsqlCommand cmd = new NpgsqlCommand("SELECT DATNAME FROM PG_DATABASE", conn);

                    using (var  reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            dbNames.Add(reader[0].ToString());
                        }
                    }
                }
            }
            catch(NpgsqlException e)
            {
                throw new ConnectionException(e.Message);
            }

            return dbNames;
        }


        private async static Task<IEnumerable<string>> GetPostgresDatabaseNamesAsync(ConnectionSettings settings,
            CancellationToken ct)
        {
            string connectionString = GetPostgresConnectionString(settings, false);
            IList<string> dbNames = new List<string>();
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync(ct);
                    NpgsqlCommand cmd = new NpgsqlCommand("SELECT DATNAME FROM PG_DATABASE", conn);

                    using (var reader = await cmd.ExecuteReaderAsync(ct))
                    {
                        while (await reader.ReadAsync(ct))
                        {
                            dbNames.Add(reader[0].ToString());
                        }
                    }
                }
            }
            catch (NpgsqlException e)
            {
                throw new ConnectionException(e.Message);
            }

            return dbNames;
        }

        private static IEnumerable<string> GetSqlServerDatabaseNames(ConnectionSettings settings)
        {
            string connectionString = GetSqlServerConnectionString(settings,false);
            IList<string> dbNames = new List<string>();

            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT NAME FROM SYS.DATABASES";

                    using(var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            dbNames.Add(reader[0].ToString());
                        }
                    }
                }
            }
            catch(SqlException e)
            {
                throw new ConnectionException(e.Message);
            }
            return dbNames;
        }

        private async static Task<IEnumerable<string>> GetSqlServerDatabaseNamesAsync(ConnectionSettings settings,CancellationToken token)
        {
            string connectionString = GetSqlServerConnectionString(settings, false);
            IList<string> dbNames = new List<string>();

            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync(token);

                    var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT NAME FROM SYS.DATABASES";

                    using (var reader = await cmd.ExecuteReaderAsync(token))
                    {
                        while (await reader.ReadAsync(token))
                        {
                            dbNames.Add(reader[0].ToString());
                        }
                    }
                }
            }
            catch (SqlException e)
            {
                throw new ConnectionException(e.Message);
            }
            return dbNames;
        }


        #endregion

        public static IEnumerable<OdbcDataSourceInfo> GetOdbcDataSources()
        {
            
            List<OdbcDataSourceInfo> list = new List<OdbcDataSourceInfo>();
            list.AddRange(EnumOdbc(Registry.CurrentUser));
            list.AddRange(EnumOdbc(Registry.LocalMachine));
            
            return list;
        }

        private static IEnumerable<OdbcDataSourceInfo> EnumOdbc(RegistryKey rootKey)
        {
            RegistryKey regKey = rootKey.OpenSubKey(@"Software\ODBC\ODBC.INI\ODBC Data Sources");
            if (regKey != null)
            {
                foreach (string name in regKey.GetValueNames())
                {
                    string value = regKey.GetValue(name, "").ToString();
                    yield return new OdbcDataSourceInfo { Dsn = name, Driver = value };
                }
            }
        }


        public static ConnectionSettings GetSettings(DatabaseType dbType, string connStr)
        {
            switch (dbType)
            {
                case DatabaseType.Access:
                    return GetAccessSettings(connStr);
                    
                case DatabaseType.Firebird:
                    return GetFirebirdSettings(connStr);
                case DatabaseType.MySql:
                    return GetMySqlSettings(connStr);
                case DatabaseType.Oracle:
                    return GetOracleSettings(connStr);
                case DatabaseType.PostgreSql:
                    return GetPostgresSettings(connStr);
                case DatabaseType.SqlServer:
                    return GetSqlServerSettings(connStr);
                case DatabaseType.ODBC:
                    break;

            }
            return null;


        }

        private static ConnectionSettings GetAccessSettings(string cs)
        {
            var builder = new OleDbConnectionStringBuilder(cs);
            var settings = new ConnectionSettings();

            settings.Database = builder.Provider;
            string db = builder.DataSource;
            object pass;
            bool res = builder.TryGetValue("Jet OLEDB:Database Password", out pass);

            if (res)
                settings.Password = pass.ToString();

            return settings;
        }

        private static ConnectionSettings GetFirebirdSettings(string cs)
        {
            var builder = new FbConnectionStringBuilder(cs);
            var settings = new ConnectionSettings();

            settings.Database = builder.Database;
            settings.DataSource = builder.DataSource;
            settings.User = builder.UserID;
            settings.Password = builder.Password;
            settings.Port = builder.Port;

            return settings;
        }

        private static ConnectionSettings GetMySqlSettings(string cs)
        {
            var builder = new MySqlConnectionStringBuilder(cs);
            var settings = new ConnectionSettings();

            settings.Database = builder.Database;
            settings.DataSource = builder.Server;
            settings.User = builder.UserID;
            settings.Password = builder.Password;
            settings.Port = (int)builder.Port;

            return settings;

        }

        private static ConnectionSettings GetOracleSettings(string cs)
        {
            var builder = new OracleConnectionStringBuilder(cs);
            var settings = new ConnectionSettings();

            string dataSource = builder.DataSource;

            if(dataSource.Contains("HOST") && dataSource.Contains("PORT"))
            {
                object host;
                bool tryHost = builder.TryGetValue("HOST",out host);
                object port;
                bool tryPort = builder.TryGetValue("PORT", out port);
                object service;
                bool tryService = builder.TryGetValue("SERVICE_NAME", out service);

                settings.DataSource = host.ToString();
                settings.Port = (int)port;
                settings.Service = service.ToString();
            }
            else
            {
                settings.Database = builder.DataSource;
            }
            settings.User = builder.UserID;
            settings.Password = builder.Password;

            return settings;

        }

        private static ConnectionSettings GetPostgresSettings(string cs)
        {
            var builder = new NpgsqlConnectionStringBuilder(cs);
            var settings = new ConnectionSettings();

            settings.DataSource = builder.Host;
            settings.Database = builder.Database;
            settings.User = builder.Username;
            settings.Password = builder.Password;
            settings.Port = builder.Port;

            return settings;
        }

        private static ConnectionSettings GetSqlServerSettings(string cs)
        {
            var builder = new SqlConnectionStringBuilder(cs);
            var settings = new ConnectionSettings();


            settings.DataSource = builder.DataSource;

            if(!string.IsNullOrEmpty(builder.AttachDBFilename) 
                && File.Exists(builder.AttachDBFilename))
            {
                settings.Database = builder.AttachDBFilename;
            }
            else
            {
                settings.Database = builder.InitialCatalog;
            }
            if (!builder.IntegratedSecurity)
            {
                settings.User = builder.UserID;
                settings.Password = builder.Password;
            }

            return settings;
        }

    }







}
