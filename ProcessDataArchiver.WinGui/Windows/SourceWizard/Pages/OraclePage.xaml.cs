
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
using System.Data;
using ProcessDataArchiver.DataCore.Infrastructure;
using System.Collections;
using ProcessDataArchiver.DataCore.Database.DbProviders;
using ProcessDataArchiver.DataCore.Database.Schema;
using Oracle.ManagedDataAccess.Client;
using System.Threading;

namespace ProcessDataArchiver.WinGui.Windows.SourceWizard.Pages
{
    /// <summary>
    /// Interaction logic for OraclePage.xaml
    /// </summary>
    public partial class OraclePage : Page,INotifyPropertyChanged,INotifyDataErrorInfo, IDbConfigPage
    {
        private string serverName;
        private string serviceName;
        private string instance;
        private string userName;
        private string port;
        private string connectionType;
        private Oracle.ManagedDataAccess.Client.OracleConnectionStringBuilder builder;
        private IDatabaseProvider provider;
        private DatabaseProviderFactory DatabaseProviderFactory;
        private CancellationTokenSource cts;
        private bool loaded = false;
        private bool firstLoadaedAliases = false;
        private bool firstLoadedServers = false;

        public event EventHandler<bool> ConnectEvent;

        #region Properites

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
                OnPropertyChanged(nameof(ServerName));
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
                OnPropertyChanged(nameof(UserName));
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
                OnPropertyChanged(nameof(Port));
            }
        }

        public string Instance
        {
            get
            {
                return instance;
            }
            set
            {
                instance = value;
                Validate(nameof(Instance));
                OnPropertyChanged(nameof(Instance));
            }
        }

        public string ServiceName
        {
            get
            {
                return serviceName;
            }

            set
            {
                serviceName = value;
                Validate(nameof(ServiceName));
                OnPropertyChanged(nameof(ServiceName));
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

        public ObservableCollection<string> Servers { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> Instances { get; set; } = new ObservableCollection<string>();

        public ObservableCollection<ErrorInfo> ErrorsList { get; set; } =
            new ObservableCollection<ErrorInfo>();


        #endregion


        #region Events

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<bool> ValidationChanged;
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
        public event EventHandler<bool> ConnectionStateChanged;

        private void OnErrorChanged(string property)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(property));
        }

        private void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        #endregion


        #region Validate

        private void Validate(string property)
        {
            string error = null;
            switch (property)
            {

                case nameof(Instance):
                    if (connectionType == "TNS" && string.IsNullOrEmpty(Instance))
                        error = "Alias połączenia nie może być pusty";
                    break;

                case nameof(ServerName):
                    if (connectionType == "Standardowe" && string.IsNullOrEmpty(ServerName))
                        error = "Nazwa serwera nie może byc pusta";
                    break;

                case nameof(UserName):
                    if (string.IsNullOrEmpty(UserName))
                        error = "Nazwa użytkownika nie moze być pusta";
                    break;

                case nameof(Port):
                    if (connectionType == "Standardowe")
                    {
                        int val;
                        if (!int.TryParse(Port, out val))
                        {
                            error = "Numer portu musi być liczbą";
                        }
                    }
                    break;

                case nameof(ServiceName):
                    if (connectionType == "Standardowe" && string.IsNullOrEmpty(ServiceName))
                    {
                        error = "Nazwa usługi nie może być pusta";
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

        public OraclePage()
        {
            InitializeComponent();
            loaded = true;
            this.DataContext = this;
            
            connectionType = (ConnTypeCb.SelectedItem as ComboBoxItem).Content.ToString();        
            DatabaseProviderFactory = new DatabaseProviderFactory();
            LoadDefaultSettings();

        }

        public OraclePage(string connStr)
        {
            InitializeComponent();
            loaded = true;
            this.DataContext = this;

            connectionType = (ConnTypeCb.SelectedItem as ComboBoxItem).Content.ToString();
            DatabaseProviderFactory = new DatabaseProviderFactory();
            LoadSettings(connStr);

        }

        #region GuiEvents

        private async void TestButton_Click(object sender, RoutedEventArgs e)
        {
            //Disable();
            //bool result = await TryConnect();
            //if (result)
            //    MessageBox.Show("Próba nawiązania połaczenia przebiegła pomyślnie", "Połączono",
            //        MessageBoxButton.OK, MessageBoxImage.Information);
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
                ConnectEvent?.Invoke(this, result);
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

        private void ServerComboBox_DropDownOpened(object sender, EventArgs e)
        {
            if (!firstLoadedServers)
            {
                RefreshServers();
                firstLoadedServers = true;
            }
        }

        private void RefreshServersBtn_Click(object sender, RoutedEventArgs e)
        {
            RefreshServers();
        }

        private void InstanceComboBox_DropDownOpened(object sender, EventArgs e)
        {
            if (!firstLoadaedAliases)
            {
                RefreshInstances();
                firstLoadaedAliases = true;
            }
        }

        private void RefreshInstanceBtn_Click(object sender, RoutedEventArgs e)
        {
            RefreshInstances();
        }

        private void RefreshAliasBtn_Click(object sender, RoutedEventArgs e)
        {
            RefreshInstances();
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

        private void ConnTypeCb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (loaded)
            {
                connectionType = (ConnTypeCb.SelectedItem as ComboBoxItem).Content.ToString();
                EnableConnectBox();
                Validate(nameof(Instance));
                Validate(nameof(ServerName));
                Validate(nameof(Port));
                Validate(nameof(ServiceName));
            }
        }

        #endregion


        #region GuiOperations

        private void EnableTestButton()
        {
            if (HasErrors)
                TestButton.IsEnabled = false;
            else
                TestButton.IsEnabled = true;
        }

        private void EnableConnectBox()
        {
            ConnectGroup.Foreground = Brushes.Black;
            ConnTypeCb.IsEnabled = true;
            ConnTypeCb.Foreground = Brushes.Black;
            ConnTypeTb.Foreground = Brushes.Black;
            if (connectionType.Equals("TNS"))
            {
                InstanceTb.Foreground = Brushes.Black;
                InstanceCb.IsEnabled = true;
                RefreshInstanceButton.IsEnabled = true;

                ServerTb.Foreground = Brushes.Gray;
                ServerCb.IsEnabled = false;
                RefreshServersButton.IsEnabled = false;
                PortTb.Foreground = Brushes.Gray;
                PortTextBox.IsEnabled = false;
                ServiceTb.Foreground = Brushes.Gray;
                ServiceTextBox.IsEnabled = false;
            }
            else
            {
                InstanceTb.Foreground = Brushes.Gray;
                InstanceCb.IsEnabled = false;
                RefreshInstanceButton.IsEnabled = false;

                ServerTb.Foreground = Brushes.Black;
                ServerCb.IsEnabled = true;
                RefreshServersButton.IsEnabled = true;
                PortTb.Foreground = Brushes.Black;
                PortTextBox.IsEnabled = true;
                ServiceTb.Foreground = Brushes.Black;
                ServiceTextBox.IsEnabled = true;
            }
        }

        private void EnableLoginBox()
        {
            LoginGroup.Foreground = Brushes.Black;

            UserTb.Foreground = Brushes.Black;
            UserTextBox.IsEnabled = true;
            PassTb.Foreground = Brushes.Black;
            PasswordBox.IsEnabled = true;
                    
        }



        public void Disable()
        {
            ConnectGroup.Foreground = Brushes.Gray;
            ConnTypeTb.Foreground = Brushes.Gray;
            ConnTypeCb.IsEnabled = false;
            ConnTypeCb.Foreground = Brushes.Gray;
            InstanceTb.Foreground = Brushes.Gray;
            InstanceCb.IsEnabled = false;
            RefreshInstanceButton.IsEnabled = false;
            ServerTb.Foreground = Brushes.Gray;
            ServerCb.IsEnabled = false;
            RefreshServersButton.IsEnabled = false;
            PortTb.Foreground = Brushes.Gray;
            PortTextBox.IsEnabled = false;
            ServiceTb.Foreground = Brushes.Gray;
            ServiceTextBox.IsEnabled = false;

            LoginGroup.Foreground = Brushes.Gray;
            UserTb.Foreground = Brushes.Gray;
            UserTextBox.IsEnabled = false;
            PassTb.Foreground = Brushes.Gray;
            PasswordBox.IsEnabled = false;

            TestButton.IsEnabled = false;

            ConnectionStateChanged?.Invoke(this, false);
        }

        private void Enable()
        {

            EnableConnectBox();
            EnableLoginBox();
            EnableTestButton();
            ConnectionStateChanged?.Invoke(this, true);
        }

        #endregion


        #region Provider

        private async Task<bool> TryConnect()
        {
            bool conn = false;
            string connType = (ConnTypeCb.SelectedItem as ComboBoxItem).Content.ToString();
            ConnectionSettings settings = null;

                if (connType == "TNS")
                {
                    settings = new ConnectionSettings
                    {
                        Database = Instance,
                        User = UserName,
                        Password = PasswordBox.Password                       
                    };
                    provider = DatabaseProviderFactory.CreateProvider(DatabaseType.Oracle, settings);
                    conn = await provider.TryConnectAsync();
                }
                else
                {
                    settings = new ConnectionSettings
                    {
                        DataSource = ServerName,
                        Port = int.Parse(Port),
                        Service = ServiceName,
                        User = UserName,
                        Password = PasswordBox.Password
                    };
                    provider = DatabaseProviderFactory.CreateProvider(DatabaseType.Oracle, settings);
                    conn = await provider.TryConnectAsync();
                }
           
            return conn;
        }

        private async Task<bool> TryConnect(CancellationToken token)
        {
            bool conn = false;
            string connType = (ConnTypeCb.SelectedItem as ComboBoxItem).Content.ToString();
            ConnectionSettings settings = null;

            if (connType == "TNS")
            {
                settings = new ConnectionSettings
                {
                    Database = Instance,
                    User = UserName,
                    Password = PasswordBox.Password
                };
                provider = DatabaseProviderFactory.CreateProvider(DatabaseType.Oracle, settings);
                conn = await provider.TryConnectAsync(token);
            }
            else
            {
                settings = new ConnectionSettings
                {
                    DataSource = ServerName,
                    Port = int.Parse(Port),
                    Service = ServiceName,
                    User = UserName,
                    Password = PasswordBox.Password
                };
                provider = DatabaseProviderFactory.CreateProvider(DatabaseType.Oracle, settings);
                conn = await provider.TryConnectAsync(token);
            }

            return conn;
        }


        private  void LoadDefaultSettings()
        {
            var sett = DatabaseProviderFactory.DefaultSettings[DatabaseType.Oracle];
            Instance = sett.Database;
            ServerName = sett.DataSource;
            Port = sett.Port.ToString();
            ServiceName = sett.Service;
            UserName = "SYSDBA";
        }


        private async void RefreshServers()
        {
            //Disable();
            //IEnumerable<string> devices = await NetworkScanner.GetNetworkDevicesAsync();
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

        private async void RefreshInstances()
        {
            Disable();
            var instances = await DatabaseProviderFactory.GetDatabaseNamesAsync(DatabaseType.Oracle, null);
            Instances.Clear();
            foreach (var item in instances)
            {
                Instances.Add(item);
            }
            if (Instances.Count > 0)
                Instance = Instances[0];
            Enable();

        }

        #endregion

        public async Task<bool> Finish()
        {
            Disable();
         //   provider = GetProvider();
            bool result = false;
            //  result = await provider.TryConnectAsync(cts.Token);
            cts = new CancellationTokenSource();
            DetailsDialogBox dd = new DetailsDialogBox("Łączenie...", "Łączenie", cts);
            ConnectEvent += dd.SetWindowState;

            dd.ShowAndInvoke();
            try
            {
                result = await TryConnect(cts.Token);
                //ConnectEvent?.Invoke(this, result);
                //ConnectEvent?
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
            if (result)
                DataSource = provider;
            Enable();
            return result;
        }

        public IEnumerable GetErrors(string propertyName)
        {
            return ErrorsList.Where(e => e.Propery == propertyName)
                .Select(e => e.Message).ToList();
        }

        private void LoadSettings(string connStr)
        {
            var sett = DatabaseProviderFactory.GetSettings(DatabaseType.Oracle, connStr);

            if (string.IsNullOrEmpty(sett.Service))
            {
                this.Instance = sett.DataSource;
            }
            else
            {
                this.ConnTypeCb.SelectedIndex = 1;
                this.ServerName = sett.DataSource;
                this.Port = sett.Port.ToString();
                this.ServiceName = sett.Service;
            }

            this.UserName = sett.User;
            this.PasswordBox.Password = sett.Password;
        }

        public void Validate()
        {
            Validate(nameof(ServerName));
            Validate(nameof(Port));
            Validate(nameof(UserName));
            Validate(nameof(Instance));
            Validate(nameof(ServiceName));
            if (PasswordBox.Password.Length < 1)
                AddError("PasswordBox", "Nie podano hasla");
            else
                RemoveError("PasswordBox");

            ValidationChanged?.Invoke(this, !HasErrors);
            EnableTestButton();

            
        }
        public delegate int calc(int x);
        public event calc calc1;
    }
}
