using ProcessDataArchiver.DataCore.Database.DbProviders;
using ProcessDataArchiver.DataCore.Database.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ProcessDataArchiver.WinGui.Windows.SourceWizard.Pages
{
    /// <summary>
    /// Interaction logic for DatabasePage.xaml
    /// </summary>
    public partial class DatabasePage : Page
    {
        private bool loaded;

        private SqlServerPage sqlServer;
        private MySqlPage mySql;
        private PostgreSqlPage postgreSql;
        private OraclePage oracle;
        private AccessPage access;
        private FirebirdPage firebird;
        private OdbcPage odbc;

        public IDatabaseProvider DbProvider { get; set; }

        private IDbConfigPage currentPage;
        private string connectionString;

        public bool IsValid
        {
            get
            {
                return !currentPage.HasErrors;
            }
        }


        public event EventHandler<bool> ValidationChanged;
        public event EventHandler<bool> ConnectionStateChanged;

        public DatabasePage()
        {
            InitializeComponent();
            loaded = true;

            InitPage(DatabaseType.SqlServer);
            DbContentFrame.NavigationService.Navigate(sqlServer);
            currentPage = sqlServer;

            AddEventListeners();
        }

        public DatabasePage(DatabaseType dbType, string cs)
        {
            InitializeComponent();
            loaded = true;
            connectionString = cs;

            InitPage(dbType, cs);
            Navigate(dbType);

            AddEventListeners();

            
        }

        private void Navigate(DatabaseType dbType)
        {
            switch (dbType)
            {
                case DatabaseType.Access:
                    DbContentFrame.NavigationService.Navigate(access);
                    currentPage = access;
                    break;
                case DatabaseType.Firebird:
                    DbContentFrame.NavigationService.Navigate(firebird);
                    currentPage = firebird;
                    break;
                case DatabaseType.MySql:
                    DbContentFrame.NavigationService.Navigate(mySql);
                    currentPage = mySql;
                    break;
                case DatabaseType.Oracle:
                    DbContentFrame.NavigationService.Navigate(oracle);
                    currentPage = oracle;
                    break;
                case DatabaseType.PostgreSql:
                    DbContentFrame.NavigationService.Navigate(postgreSql);
                    currentPage = postgreSql;
                    break;
                case DatabaseType.SqlServer:
                    DbContentFrame.NavigationService.Navigate(sqlServer);
                    currentPage = sqlServer;
                    break;
                case DatabaseType.ODBC:
                    DbContentFrame.NavigationService.Navigate(odbc);
                    currentPage = odbc;
                    break;
                default:
                    break;
            }
        }

        private void InitPage(DatabaseType dbType, string cs)
        {
            switch (dbType)
            {
                case DatabaseType.Access:
                    if (access == null)
                        access = new AccessPage(cs);
                    break;
                case DatabaseType.Firebird:
                    if (firebird == null)
                        firebird = new FirebirdPage(cs);
                    break;
                case DatabaseType.MySql:
                    if (mySql == null)
                        mySql = new MySqlPage(cs);
                    break;
                case DatabaseType.Oracle:
                    if (oracle == null)
                        oracle = new OraclePage(cs);
                    break;
                case DatabaseType.PostgreSql:
                    if (postgreSql == null)
                        postgreSql = new PostgreSqlPage(cs);
                    break;
                case DatabaseType.SqlServer:
                    if (sqlServer == null)
                        sqlServer = new SqlServerPage(cs);
                    break;
                case DatabaseType.ODBC:
                    if (odbc == null)
                        odbc = new OdbcPage(cs);
                    break;
                default:
                    break;
            }
        }

        private void InitPage(DatabaseType dbType)
        {
            switch (dbType)
            {
                case DatabaseType.Access:
                    if (access == null)
                        access = new AccessPage();
                    break;
                case DatabaseType.Firebird:
                    if (firebird == null)
                        firebird = new FirebirdPage();
                    break;
                case DatabaseType.MySql:
                    if (mySql == null)
                        mySql = new MySqlPage();
                    break;
                case DatabaseType.Oracle:
                    if (oracle == null)
                        oracle = new OraclePage();
                    break;
                case DatabaseType.PostgreSql:
                    if (postgreSql == null)
                        postgreSql = new PostgreSqlPage();
                    break;
                case DatabaseType.SqlServer:
                    if (sqlServer == null)
                        sqlServer = new SqlServerPage();
                    break;
                case DatabaseType.ODBC:
                    if (odbc == null)
                        odbc = new OdbcPage();
                    break;
                default:
                    break;
            }
        }

        private void DbTypeCb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (loaded)
            {
                var cb = sender as ComboBox;
                var item = cb.SelectedItem as String;

                

                RemoveEventListeners();


                switch (item)
                {
                    case "MySQL":
                        InitPage(DatabaseType.MySql);
                        DbContentFrame.NavigationService.Navigate(mySql);
                        currentPage = mySql;
                        break;

                    case "SQL Server":
                        InitPage(DatabaseType.SqlServer);
                        DbContentFrame.NavigationService.Navigate(sqlServer);
                        currentPage = sqlServer;
                        break;

                    case "PostgreSQL":
                        InitPage(DatabaseType.PostgreSql);
                        DbContentFrame.NavigationService.Navigate(postgreSql);
                        currentPage = postgreSql;
                        break;

                    case "Oracle":
                        InitPage(DatabaseType.Oracle);
                        DbContentFrame.NavigationService.Navigate(oracle);
                        currentPage = oracle;
                        break;

                    case "MS Access":
                        InitPage(DatabaseType.Access);
                        DbContentFrame.NavigationService.Navigate(access);
                        currentPage = access;
                        break;

                    case "Firebird":
                        InitPage(DatabaseType.Firebird);
                        DbContentFrame.NavigationService.Navigate(firebird);
                        currentPage = firebird;
                        break;

                    case "ODBC":
                        InitPage(DatabaseType.ODBC);
                        DbContentFrame.NavigationService.Navigate(odbc);
                        currentPage = odbc;
                        break;
                }
                AddEventListeners();
                currentPage.Validate();
            }
            
        }

        public void ValidatePage()
        {
            currentPage.Validate();
        }


        private void AddEventListeners()
        {
            currentPage.ConnectionStateChanged += CurrentPage_ConnectionStateChanged;
            currentPage.ValidationChanged += CurrentPage_ValidationChanged;
        }

        private void RemoveEventListeners()
        {
            currentPage.ConnectionStateChanged -= CurrentPage_ConnectionStateChanged;
            currentPage.ValidationChanged -= CurrentPage_ValidationChanged;
        }

        private void CurrentPage_ValidationChanged(object sender, bool e)
        {
            ValidationChanged?.Invoke(this, !currentPage.HasErrors);
        }

        private void CurrentPage_ConnectionStateChanged(object sender, bool e)
        {
            if (e)
                Enable();
            else
                Disable();

            ConnectionStateChanged?.Invoke(this, e);
        }

        public void Disable()
        {
            //TypeGroup.Foreground = Brushes.Gray;
            DbTypeCb.IsEnabled = false;
        }

        public void Enable()
        {
        //    TypeGroup.Foreground = Brushes.Black;
            DbTypeCb.IsEnabled = true;
        }

        private bool CheckIfLoaded()
        {
            return DbContentFrame != null;
        }

        public async Task<bool> Finish()
        {
            bool result = await currentPage.Finish();
            if (result)
                DbProvider = currentPage.DataSource;
            return result;
        }
    }
}
