
using ProcessDataArchiver.WinGui.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using ProcessDataArchiver.DataCore.Infrastructure;
using System.Collections;
using ProcessDataArchiver.DataCore.Database.DbProviders;
using ProcessDataArchiver.DataCore.Database.Schema;
using System.Threading;

namespace ProcessDataArchiver.WinGui.Windows.SourceWizard.Pages
{
    /// <summary>
    /// Interaction logic for MySqlPage.xaml
    /// </summary>
    public partial class MySqlPage : Page,INotifyPropertyChanged,INotifyDataErrorInfo,IDbConfigPage
    {
        private string serverName;
        private string userName;
        private string databaseName;
        private string port;
        private IDatabaseProvider provider;
        private DatabaseProviderFactory DatabaseProviderFactory;
        private bool loaded = false;
        
        private bool firstRefServers = false;
        private bool firstRefDatabases = false;
        private CancellationTokenSource cts = new CancellationTokenSource();
        public event EventHandler<bool> ConnectEvent;


        #region __Properties__
        public string ServerName
        {
            get
            {
                return serverName;
            }

            set
            {
                serverName = value;
                Validate(nameof(ServerName));
                OnPropertyChaned(nameof(ServerName));
            }
        }

        public string UserName
        {
            get
            {
                return userName;
            }

            set
            {
                userName = value;
                Validate(nameof(UserName));
                OnPropertyChaned(nameof(UserName));
            }
        }


        public string DatabaseName
        {
            get
            {
                return databaseName;
            }

            set
            {
                databaseName = value;
                Validate(nameof(DatabaseName));
                OnPropertyChaned(nameof(DatabaseName));

            }
        }


        public string Port
        {
            get
            {
                return port;
            }

            set
            {
                port = value;
                Validate(nameof(Port));
                OnPropertyChaned(nameof(Port));
            }
        }

        public bool HasErrors
        {
            get
            {
                return ErrorsList.Count > 0;
            }
        }


        public IDatabaseProvider DataSource { get; private set; }

        public ObservableCollection<ErrorInfo> ErrorsList { get; set; } =
            new ObservableCollection<ErrorInfo>();

        public ObservableCollection<string> Databases { get; set; } = 
            new ObservableCollection<string>();

        public ObservableCollection<string> Servers { get; set; } = 
            new ObservableCollection<string>();

        #endregion


        #region __Validation__

        private void Validate(string property)
        {
            string error = null;
            switch (property)
            {
                case nameof(ServerName):
                    if (string.IsNullOrEmpty(ServerName))
                        error = "Nazwa serwera nie może byc pusta";
                    break;

                case nameof(UserName):
                    if (string.IsNullOrEmpty(UserName))
                        error = "Nazwa użytkownika nie moze być pusta";
                    break;
                case nameof(Port):
                    int val;
                    if (!int.TryParse(Port, out val))
                    {
                        error = "Numer portu musi być liczbą";
                    }
                    break;

                case nameof(DatabaseName):
                    if (string.IsNullOrEmpty(DatabaseName))
                        error = "Nazwa bazy danych nie może być pusta";
                    else
                    {
                        var match = DatabaseName.IndexOfAny
                            (new char[] { '*', '&', '/', '\\', '#', '(', ')', '^', '<', '>' }) != -1;
                        if (match)
                            error = "Nazwa bazy danych nie może zawierać znaków specjalnych";
                    }
                    break;

            }
            if (error != null)
                AddError(property, error);
            else
                RemoveError(property);
            EnableTestButton();
            OnErrorChanged(property);
            ValidationChanged?.Invoke(this, ErrorsList.Count == 0);
            
        }

        private void AddError(string property, string error)
        {
            var err = ErrorsList.Where(e => e.Propery == property).FirstOrDefault();
            if (err != null)
            {
                ErrorsList.Remove(err);
            }
            ErrorsList.Add(new ErrorInfo { Propery = property, Message = error });


        }

        private void RemoveError(string property)
        {
            var errors = ErrorsList.Where(e => e.Propery == property).ToList();
            foreach (var item in errors)
            {
                ErrorsList.Remove(item);
            }

        }

        #endregion


        #region __Events__

        public event PropertyChangedEventHandler PropertyChanged;

        public event EventHandler<bool> ValidationChanged;

        public event EventHandler<bool> ConnectionStateChanged;
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        private void OnErrorChanged(string property)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(property));
        }

        private void OnPropertyChaned(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        #endregion


        public MySqlPage()
        {
            InitializeComponent();

            this.DataContext = this;
            loaded = CheckIfLoaded();
            DatabaseProviderFactory = new DatabaseProviderFactory();
            LoadDefaultSettings();

        }

        public MySqlPage(string connStr)
        {
            InitializeComponent();

            this.DataContext = this;
            loaded = CheckIfLoaded();
            DatabaseProviderFactory = new DatabaseProviderFactory();
            LoadSettings(connStr);

        }

        #region __GuiEvents__


        private void DatabaseCb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            var pb = sender as PasswordBox;

            if (pb.Password.Length < 1)
            {
                AddError("PasswordBox", "Należy podać hasło");
                PassBor.BorderBrush = Brushes.Red;
                pb.ToolTip = "Nie podano hasła";

            }
            else
            {
                RemoveError("PasswordBox");
                PassBor.BorderBrush = Brushes.Transparent;
                pb.ToolTip = null;
            }
            ValidationChanged?.Invoke(this, !HasErrors);
            EnableTestButton();
        }

        private void DatabaseCb_DropDownOpened(object sender, EventArgs e)
        {
            if (!firstRefDatabases)
            {
                LoadDatabases();
                firstRefDatabases = true;
                DatabaseCb.IsDropDownOpen = true;
            }
        }

        private void RefreshDbBtn_Click(object sender, RoutedEventArgs e)
        {
            LoadDatabases();
        }

        private void RefreshServersBtn_Click(object sender, RoutedEventArgs e)
        {
            LoadServers();
        }

        private void ServerComboBox_DropDownOpened(object sender, EventArgs e)
        {
            if (!firstRefServers)
            {
                LoadServers();
                firstRefServers = true;

            }
        }

        private async void TestButton_Click(object sender, RoutedEventArgs e)
        {
            //Disable();

            //bool connected = await TryConnect();

            //if (connected)
            //{
            //    MessageBox.Show("Próba nawiązania połaczenia przebiegła pomyślnie", "Połączono",
            //        MessageBoxButton.OK, MessageBoxImage.Information);
            //}

            //Enable();
            Disable();
            //  bool result = false;

            cts = new CancellationTokenSource();
            DetailsDialogBox dd = new DetailsDialogBox("Łączenie...", "Łączenie", cts);
            ConnectEvent += dd.SetWindowState;
            bool result = false;

            dd.ShowAndInvoke();
            try
            {
                result = await TryConnect(cts.Token);
            }
            catch (ConnectionException ex)
            {
                dd.Details = ex.Message;
                dd.SetWindowState(this, false);
            }
            catch (TaskCanceledException exe)
            {
                dd.Details = exe.Message;
                dd.SetWindowState(this, false);
            }
            finally
            {
                ConnectEvent -= dd.SetWindowState;
            }
            //     bool result = await TryConnect();
            if (result)
            {
                //MessageBox.Show("Próba nawiązania połączenia przebiegła pomyślnie", "Połaczono",
                //    MessageBoxButton.OK, MessageBoxImage.Information);
                ValidationChanged?.Invoke(this, true);
            }
            Enable();
        }

        #endregion


        #region __GuiOperations__

        private bool CheckIfLoaded()
        {
            return PasswordBox != null;
        }
        private void Disable()
        {
            ConnectGroup.Foreground = Brushes.Gray;
            ServerCb.IsEnabled = false;
            ServerCb.Foreground = Brushes.Gray;
            //DbGroup.Foreground = Brushes.Gray;
            DatabaseCb.IsEnabled = false;
            PortTextBox.IsEnabled = false;

            LoginGroup.Foreground = Brushes.Gray;
            UserTextBox.IsEnabled = false;
            PasswordBox.IsEnabled = false;

            RefreshDbButton.IsEnabled = false;
            RefreshServerButton.IsEnabled = false;

            TestButton.IsEnabled = false;

            ConnectionStateChanged?.Invoke(this, false);
        }

        private void Enable()
        {
            ConnectGroup.Foreground = Brushes.Black;
         //   DbGroup.Foreground = Brushes.Black;
            DatabaseCb.IsEnabled = true;
            ServerCb.IsEnabled = true;
            ServerCb.Foreground = Brushes.Black;
            PortTextBox.IsEnabled = true;

            LoginGroup.Foreground = Brushes.Black;
            UserTextBox.IsEnabled = true;
            PasswordBox.IsEnabled = true;

            RefreshDbButton.IsEnabled = true;
            RefreshServerButton.IsEnabled = true;

            EnableTestButton();

            ConnectionStateChanged?.Invoke(this, true);
            ValidationChanged?.Invoke(this, !HasErrors);
        }

        private void EnableTestButton()
        {
            if (!HasErrors || (ErrorsList.Count == 1 &&
                ErrorsList.Where(p => p.Propery == "DatabaseName").FirstOrDefault() != null))
                TestButton.IsEnabled = true;
            else
                TestButton.IsEnabled = false;
        }
        #endregion


        #region __Interface__

        public async Task<bool> Finish()
        {
            bool result = false;
            Disable();

            //  bool result = false;

            cts = new CancellationTokenSource();
            DetailsDialogBox dd = new DetailsDialogBox("Łączenie...", "Łączenie", cts);
            ConnectEvent += dd.SetWindowState;

            dd.ShowAndInvoke();
            try
            {
                result = await TryConnect(cts.Token);
                dd.Close();
            }
            catch (ConnectionException ex)
            {
                dd.Details = ex.Message;
                dd.SetWindowState(this, false);
            }
            catch (TaskCanceledException exe)
            {
                dd.Details = exe.Message;
                dd.SetWindowState(this, false);
            }
            finally
            {
                ConnectEvent -= dd.SetWindowState;
             //   dd.Close();
            }
            if (result)
                DataSource = provider;
            Enable();
            return result;
        }

        public IEnumerable GetErrors(string propertyName)
        {
            return ErrorsList.Where(p => p.Propery == propertyName)
                .Select(p => p.Message).ToList();
        }

        #endregion


        #region __Provider__

        private void LoadDefaultSettings()
        {
            var sett = DatabaseProviderFactory.DefaultSettings[DatabaseType.MySql];
            ServerName = sett.DataSource;
            DatabaseName = sett.Database;
            Port = sett.Port.ToString();
            UserName = "root";
        }
        private async void LoadDatabases()
        {

            //if(ErrorsList.Count == 1 && ErrorsList
            //    .Where(p => p.Propery == "DatabaseName").FirstOrDefault() != null)
            //{
            //    var sett = new ConnectionSettings
            //    {
            //        DataSource = ServerName,
            //        Port = int.Parse(Port),
            //        User = UserName,
            //        Password = PasswordBox.Password
            //    };

            //    Databases.Clear();
            //    var databases = await DatabaseProviderFactory.GetDatabaseNamesAsync(DatabaseType.MySql, sett,cts.Token);
            //    foreach (var item in databases)
            //    {
            //        Databases.Add(item);
            //    }
            //}
            if (!HasErrors || (ErrorsList.Count == 1 && ErrorsList
                .Where(p => p.Propery == "DatabaseName").FirstOrDefault() != null))
            {
                Disable();
                var sett = GetSettings(false);
                Databases.Clear();
                cts = new CancellationTokenSource();
                DetailsDialogBox dd = new DetailsDialogBox("Wyszukiwanie baz danych...", "Wyszukiwanie", cts);
                ConnectEvent += dd.SetWindowState;
                IEnumerable<string> databases = null;

                dd.ShowAndInvoke();

                try
                {
                    databases = await DatabaseProviderFactory.GetDatabaseNamesAsync(DatabaseType.MySql, sett, cts.Token);
                    dd.Close();
                }
                catch (ConnectionException ex)
                {
                    dd.Details = ex.Message;
                    dd.SetWindowState(this, false);
                }
                catch (TaskCanceledException exe)
                {
                    dd.Details = exe.Message;
                    dd.SetWindowState(this, false);
                }
                finally
                {
                    ConnectEvent -= dd.SetWindowState;
                }
                if (databases != null)
                {
                    foreach (var item in databases)
                    {
                        Databases.Add(item);
                    }
                    DatabaseName = Databases.FirstOrDefault();
                }

                Enable();
            }

        }
        

       
        private async void LoadServers()
        {
            //Disable();
            //var devices = await NetworkScanner.GetNetworkDevicesAsync();
            //Servers.Clear();
            //foreach (var item in devices)
            //{
            //    Servers.Add(item);
            //}
            //if (Servers.Count > 0)
            //    ServerName = Servers[0];
            //Enable();
            Disable();
            IEnumerable<string> devices = null;
            cts = new CancellationTokenSource();
            DetailsDialogBox dd = new DetailsDialogBox("Wyszukiwanie...", "Wyszukiwanie", cts);
            ConnectEvent += dd.SetWindowState;

            dd.OperationCanceled += EnableAfterCancel;
            dd.ShowAndInvoke();
            try
            {
                devices = await NetworkScanner.GetNetworkDevicesAsync(cts.Token);
                dd.Close();

            }
            catch (ConnectionException ex)
            {
                dd.Details = ex.Message;
                dd.SetWindowState(this, false);
            }
            catch (TaskCanceledException exe)
            {
                dd.Details = exe.Message;
                dd.SetWindowState(this, false);
            }
            finally
            {
                ConnectEvent -= dd.SetWindowState;
                dd.OperationCanceled -= EnableAfterCancel;
            }

            Servers.Clear();
            if (devices != null)
                foreach (var item in devices)
                {
                    Servers.Add(item);
                }
            else
                Servers.Add("localhost");
            if (Servers.Count > 0)
                ServerName = Servers[0];
            Enable();
        }

        private void EnableAfterCancel(object sender, EventArgs e)
        {
            Enable();
        }
        private ConnectionSettings GetSettings(bool includeDbName = true)
        {
            var settings = new ConnectionSettings
            {
                DataSource = ServerName,
                Database = DatabaseName,
                User = UserName,
                Password = PasswordBox.Password,
                Port = int.Parse(Port)
            };
            if (includeDbName)
                settings.Database = DatabaseName;

            return settings;
        }


        private async Task<bool> TryConnect()
        {
            bool res = false;
            //try
            //{
                var sett = GetSettings(false);

                var dbs = await DatabaseProviderFactory.GetDatabaseNamesAsync(DatabaseType.MySql, sett);
                Func<string, string, bool> compare = StringComparer.InvariantCultureIgnoreCase.Equals;
                if (!dbs.Select(d => d.ToLowerInvariant()).Contains(DatabaseName.ToLowerInvariant()))
                {
                    var result = MessageBox.Show("Baza danych o podanej nazwie nie istnieje, utworzyć?", "Baza",
                            MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {

                            sett.Database = DatabaseName;
                            provider = await DatabaseProviderFactory.CreateProviderAsync(DatabaseType.MySql, sett,cts.Token);

                            res = await provider.TryConnectAsync();                     
                    }
                }
                else
                {
                    sett.Database = DatabaseName;
                    provider = await DatabaseProviderFactory.CreateProviderAsync(DatabaseType.MySql, sett, cts.Token);

                    res = await provider.TryConnectAsync();
                }
            //}
            //catch (ConnectionException e)
            //{
            //    DetailsDialogBox dialog = new DetailsDialogBox();
            //    dialog.Message = "Wystąpił błąd podczas próby nawiązania połączenia";
            //    dialog.Details = e.Message;
            //    dialog.Show();
            //}
            return res;
        }


        private async Task<bool> TryConnect(CancellationToken token)
        {
            bool res = false;
            //try
            //{
            var sett = GetSettings(false);

            var dbs = await DatabaseProviderFactory.GetDatabaseNamesAsync(DatabaseType.MySql, sett,token);
            Func<string, string, bool> compare = StringComparer.InvariantCultureIgnoreCase.Equals;
            if (!dbs.Select(d => d.ToLowerInvariant()).Contains(DatabaseName.ToLowerInvariant()))
            {
                var result = MessageBox.Show("Baza danych o podanej nazwie nie istnieje, utworzyć?", "Baza",
                        MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {

                    sett.Database = DatabaseName;
                    provider = DatabaseProviderFactory.CreateProvider(DatabaseType.MySql, sett);

                    res = await provider.TryConnectAsync(token);
                }
            }
            else
            {
                sett.Database = DatabaseName;
                provider = DatabaseProviderFactory.CreateProvider(DatabaseType.MySql, sett);

                res = await provider.TryConnectAsync(token);
            }
            //}
            //catch (ConnectionException e)
            //{
            //    DetailsDialogBox dialog = new DetailsDialogBox();
            //    dialog.Message = "Wystąpił błąd podczas próby nawiązania połączenia";
            //    dialog.Details = e.Message;
            //    dialog.Show();
            //}
            return res;
        }

        #endregion

        private void LoadSettings(string connStr)
        {
            var sett = DatabaseProviderFactory.GetSettings(DatabaseType.MySql, connStr);

            this.ServerName = sett.DataSource;
            this.Port = sett.Port.ToString();
            this.UserName = sett.User;
            this.PasswordBox.Password = sett.Password;
            this.DatabaseName = sett.Database;
        }

        public void Validate()
        {
            Validate(nameof(ServerName));
            Validate(nameof(Port));
            Validate(nameof(UserName));
            Validate(nameof(DatabaseName));
            if (PasswordBox.Password.Length < 1)
                AddError("PasswordBox", "Nie podano hasla");
            else
                RemoveError("PasswordBox");

            ValidationChanged?.Invoke(this, !HasErrors);
            EnableTestButton();
        }

    }
}
