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
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.IO;
using ProcessDataArchiver.WinGui.Dialogs;
using ProcessDataArchiver.DataCore.Database.DbProviders;
using ProcessDataArchiver.DataCore.Database.Schema;
using System.Collections;
using ProcessDataArchiver.DataCore.Infrastructure;
using System.Threading;

namespace ProcessDataArchiver.WinGui.Windows.SourceWizard.Pages
{
    /// <summary>
    /// Interaction logic for FirebirdPage.xaml
    /// </summary>
    public partial class FirebirdPage : Page,INotifyPropertyChanged,INotifyDataErrorInfo,IDbConfigPage
    {
        private string serverName;
        private string dbName;
        private string databasePath;
        private string folderPath;
        private string userName;
        private string port;

        private bool firstRefServers;
        private DatabaseProviderFactory factory;
        private IDatabaseProvider provider;

        public event EventHandler<bool> ConnectEvent;
        private CancellationTokenSource cts = new CancellationTokenSource();
        private bool loaded;
        private System.Windows.Forms.OpenFileDialog dialog =
            new System.Windows.Forms.OpenFileDialog();

        private System.Windows.Forms.FolderBrowserDialog folderDialog =
            new System.Windows.Forms.FolderBrowserDialog();

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
                OnPropertyChanged(nameof(ServerName));
                Validate(nameof(ServerName));
            }
        }

        public string DatabaseName
        {
            get
            {
                return dbName;
            }

            set
            {
                dbName = value;
                OnPropertyChanged(nameof(DatabaseName));
                Validate(nameof(DatabaseName));
            }
        }

        public string DatabasePath
        {
            get
            {
                return databasePath;
            }

            set
            {
                databasePath = value;
                OnPropertyChanged(nameof(DatabasePath));
                Validate(nameof(DatabasePath));
            }
        }

        public string FolderPath
        {
            get
            {
                return folderPath;
            }

            set
            {
                folderPath = value;
                OnPropertyChanged(nameof(FolderPath));
                Validate(nameof(FolderPath));
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
                OnPropertyChanged(nameof(UserName));
                Validate(nameof(UserName));
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
                OnPropertyChanged(nameof(Port));
                Validate(nameof(Port));
            }
        }



        public ObservableCollection<ErrorInfo> ErrorsList { get; set; } =
            new ObservableCollection<ErrorInfo>();


        public ObservableCollection<string> Servers { get; set; }
            = new ObservableCollection<string>();



        public IDatabaseProvider DataSource { get; private set; }



        public bool HasErrors
        {
            get
            {
                return ErrorsList.Count > 0;
            }
        }


        #endregion

        public FirebirdPage()
        {
            InitializeComponent();
            this.DataContext = this;
            loaded = CheckIfLoaded();            
            dialog.Filter = "fdb files *.fdb|*.fdb";
            factory = new DatabaseProviderFactory();
            LoadDefaultSettings();
        }

        public FirebirdPage(string connStr)
        {
            InitializeComponent();
            this.DataContext = this;
            loaded = CheckIfLoaded();
            dialog.Filter = "fdb files *.fdb|*.fdb";
            factory = new DatabaseProviderFactory();
            LoadSettings(connStr);
        }

        #region __GuiOperations___

        private bool CheckIfLoaded()
        {
            return PasswordBox != null;
        }
        private void EnableDatabaseGroup()
        {
            if (NewDbRadio.IsChecked == true)
            {
                ExiDbPathTb.Foreground = Brushes.Gray;
                ExiDbPathTextBox.IsEnabled = false;
                ExiDbBrowseButton.IsEnabled = false;

                NewDbNameTb.Foreground = Brushes.Black;
                NewDbPathTb.Foreground = Brushes.Black;
                NewDbNameTextBox.IsEnabled = true;
                NewDbPathTextBox.IsEnabled = true;
                NewDbBrowseButton.IsEnabled = true;
            }
            else
            {
                ExiDbPathTb.Foreground = Brushes.Black;
                ExiDbPathTextBox.IsEnabled = true;
                ExiDbBrowseButton.IsEnabled = true;

                NewDbNameTb.Foreground = Brushes.Gray;
                NewDbPathTb.Foreground = Brushes.Gray;
                NewDbNameTextBox.IsEnabled = false;
                NewDbPathTextBox.IsEnabled = false;
                NewDbBrowseButton.IsEnabled = false;
            }
        }

        private void EnableTestButton()
        {
            if (!HasErrors)
                TestButton.IsEnabled = true;
            else
                TestButton.IsEnabled = false;
        }

        private void Enable()
        {
            ConnectGroup.Foreground = Brushes.Black;
            ServerCb.IsEnabled = true;
            PortTextBox.IsEnabled = true;
            RefreshServerButton.IsEnabled = true;


            LoginGroup.Foreground = Brushes.Black;
            UserTextBox.IsEnabled = true;
            PasswordBox.IsEnabled = true;

            NewDbRadio.IsEnabled = true;
            NewDbRadio.Foreground = Brushes.Black;
            ExiDbRadio.IsEnabled = true;
            ExiDbRadio.Foreground = Brushes.Black;

            DbGroup.Foreground = Brushes.Black;

            EnableTestButton();
            EnableDatabaseGroup();

            ConnectionStateChanged?.Invoke(this, true);
        }

        private void Disable()
        {
            ConnectGroup.Foreground = Brushes.Gray;
            ServerCb.IsEnabled = false;
            PortTextBox.IsEnabled = false;

            RefreshServerButton.IsEnabled = false;
            NewDbBrowseButton.IsEnabled = false;
            ExiDbBrowseButton.IsEnabled = false;

            LoginGroup.Foreground = Brushes.Gray;
            UserTextBox.IsEnabled = false;
            PasswordBox.IsEnabled = false;

            ExiDbPathTb.Foreground = Brushes.Gray;
            ExiDbPathTextBox.IsEnabled = false;
            ExiDbBrowseButton.IsEnabled = false;

            NewDbRadio.IsEnabled = false;
            NewDbRadio.Foreground = Brushes.Gray;

            ExiDbRadio.IsEnabled = false;
            ExiDbRadio.Foreground = Brushes.Gray;

            NewDbNameTb.Foreground = Brushes.Gray;
            NewDbPathTb.Foreground = Brushes.Gray;
            NewDbNameTextBox.IsEnabled = false;
            NewDbPathTextBox.IsEnabled = false;
            NewDbBrowseButton.IsEnabled = false;

            TestButton.IsEnabled = false;
            DbGroup.Foreground = Brushes.Gray;

            ConnectionStateChanged?.Invoke(this, false);
        }

        #endregion


        #region __Provider__

        private IDatabaseProvider GetProvider()
        {
            var sett = new ConnectionSettings
            {
                DataSource = ServerName,
                Port = int.Parse(Port),
                User = UserName,
                Password = PasswordBox.Password
            };
            if (NewDbRadio.IsChecked == true)
                sett.Database = Path.Combine(FolderPath, DatabaseName) + ".fdb";
            else
                sett.Database = DatabasePath;

            return DatabaseProviderFactory.CreateProvider(DatabaseType.Firebird, sett);
        }




        private async void LoadServers()
        {
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

        private void LoadDefaultSettings()
        {
            var sett = DatabaseProviderFactory.DefaultSettings[DatabaseType.Firebird];

            ServerName = sett.DataSource;
            Port = sett.Port.ToString();
            DatabasePath = sett.Database;
            DatabaseName = Path.GetFileNameWithoutExtension(sett.Database);
            FolderPath = Path.GetDirectoryName(sett.Database);
            UserName = "SYSDBA";
        }


        #endregion


        #region __Interface__

        public IEnumerable GetErrors(string propertyName)
        {
            return ErrorsList.Where(p => p.Propery == propertyName)
                .Select(p => p.Message).ToList();
        }

        public async Task<bool> Finish()
        {
            bool result = false;
            Disable();
            try
            {
                if (NewDbRadio.IsChecked == true)
                {
                    var res = MessageBox.Show("Utworzyć bazę danych?", "Baza", MessageBoxButton.YesNo,
                        MessageBoxImage.Question);
                    if (res == MessageBoxResult.Yes)
                    {
                        provider = GetProvider();
                        result = await provider.TryConnectAsync(cts.Token);
                        cts = new CancellationTokenSource();
                        DetailsDialogBox dd = new DetailsDialogBox("Łączenie...", "Łączenie", cts);
                        ConnectEvent += dd.SetWindowState;

                        dd.ShowAndInvoke();
                        try
                        {
                            result = await provider.TryConnectAsync(cts.Token);
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
                    }
                }
                else
                {
                    provider = GetProvider();
                    result = await provider.TryConnectAsync(cts.Token);
                    cts = new CancellationTokenSource();
                    DetailsDialogBox dd = new DetailsDialogBox("Łączenie...", "Łączenie", cts);
                    ConnectEvent += dd.SetWindowState;

                    dd.ShowAndInvoke();
                    try
                    {
                        result = await provider.TryConnectAsync(cts.Token);
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
                }
            }
            catch (ConnectionException ex)
            {
                DetailsDialogBox dialog = new DetailsDialogBox();
                dialog.Message = "Wystąpił błąd podczas próby nawiązania połączenia";
                dialog.Details = ex.Message;
                dialog.Show();
            }

            if (result)
                DataSource = provider;
            Enable();
            return result;
        }
        #endregion


        #region __Events__

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<bool> ValidationChanged;
        public event EventHandler<bool> ConnectionStateChanged;
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
        private void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        private void OnErrorChanged(string property)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(property));
        }

        #endregion


        #region __GuiEvents__
        private void Radio_Checked(object sender, RoutedEventArgs e)
        {
            if (loaded)
            {
                EnableDatabaseGroup();
                Validate(nameof(DatabasePath));
                Validate(nameof(DatabaseName));
                Validate(nameof(FolderPath));
            }

        }


        private void ExiDbBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            if(dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                DatabasePath = dialog.FileName;
            }
        }

        private void NewDbBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            if(folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                FolderPath = folderDialog.SelectedPath;
            }
        }

        private async void TestButton_Click(object sender, RoutedEventArgs e)
        {
            Disable();
            bool connected = false;
            DetailsDialogBox dialog = new DetailsDialogBox("Łączenie...","Łączenie",cts);
            ConnectEvent += dialog.SetWindowState;
            dialog.OperationCanceled += EnableAfterCancel;
            dialog.ShowAndInvoke();
            try
            {
                provider = GetProvider();
                connected = await provider.TryConnectAsync(cts.Token);
                ConnectEvent.Invoke(this, connected);
            }
            catch (ConnectionException ex)
            {
                dialog.Details = ex.Message;
                dialog.SetWindowState(this, false);
            }
            catch(TaskCanceledException exe)
            {
                dialog.Details = exe.Message;
                dialog.SetWindowState(this, false);
            }
            finally
            {
                ConnectEvent -= dialog.SetWindowState;
                dialog.OperationCanceled -= EnableAfterCancel;
            }
            if (connected)
            {
                //MessageBox.Show("Próba nawiązania połaczenia przebiegła pomyślnie", "Połączono",
                //    MessageBoxButton.OK, MessageBoxImage.Information);
            }

            Enable();
        }

        private void EnableAfterCancel(object sender,EventArgs e)
        {
            Enable();
        }

        private void ServerComboBox_DropDownOpened(object sender, EventArgs e)
        {
            if (!firstRefServers)
            {
                LoadServers();
                firstRefServers = true;
            }
        }

        private void RefreshServersBtn_Click(object sender, RoutedEventArgs e)
        {
            LoadServers();
        }

        #endregion


        #region __Validation__

        private void Validate(string property)
        {
            string error = null;

            switch (property)
            {
                case nameof(ServerName):
                    if (string.IsNullOrEmpty(ServerName))
                        error = "Nazwa serwera nie może być pusta";
                    break;

                case nameof(Port):
                    int val;
                    if (!int.TryParse(Port, out val))
                        error = "Numer portu musi byc liczbą";
                    break;

                case nameof(UserName):
                    if (string.IsNullOrEmpty(UserName))
                        error = "Nazwa użytkownika nie może być pusta";
                    break;

                case nameof(DatabaseName):
                    if (NewDbRadio.IsChecked == true &&
                        string.IsNullOrEmpty(DatabaseName))
                        error = "Nazwa bazy nie może byc pusta";
                    break;

                case nameof(DatabasePath):
                    if (ExiDbRadio.IsChecked == true)
                    {
                        if (!File.Exists(DatabasePath))
                            error = "Baza danych o podanej ścieżce nie istnieje";
                        else if (!string.Equals(Path.GetExtension(DatabasePath), ".fdb"
                                , StringComparison.InvariantCultureIgnoreCase))
                            error = "Plik zawiera nieprawidłowe rozszerzenie";
                    }
                    break;

                case nameof(FolderPath):
                    if (NewDbRadio.IsChecked == true)
                    {
                        if (!Directory.Exists(FolderPath))
                            error = "Folder o podanej nazwie nie istnieje";
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

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            var pb = sender as PasswordBox;

            if (pb.Password.Length < 1)
            {
                AddError("PasswordBox", "Należy podać hasło");
                PassBor.BorderBrush = Brushes.Red;
                PasswordBox.ToolTip = "Nie podano hasła";
            }
            else
            {
                RemoveError("PasswordBox");
                PassBor.BorderBrush = Brushes.Transparent;
                PasswordBox.ToolTip = null;
            }
            ValidationChanged?.Invoke(this, !HasErrors);
            EnableTestButton();
        }

        private void LoadSettings(string connStr)
        {
            var sett = DatabaseProviderFactory.GetSettings(DatabaseType.Firebird, connStr);

            this.ServerName = sett.DataSource;
            this.Port = sett.Port.ToString();

            this.UserName = sett.User;
            this.PasswordBox.Password = sett.Password;
            this.DatabasePath = sett.Database;

        }

        public void Validate()
        {
            Validate(nameof(DatabasePath));
            Validate(nameof(ServerName));
            Validate(nameof(Port));
            Validate(nameof(UserName));
            Validate(nameof(DatabaseName));
            Validate(nameof(FolderPath));

            if (PasswordBox.Password.Length < 1)
                AddError("PasswordBox", "Nie podano hasła");
            else
                RemoveError("PasswordBox");
            EnableTestButton();
            ValidationChanged?.Invoke(this, !HasErrors);
        }
    }
}
