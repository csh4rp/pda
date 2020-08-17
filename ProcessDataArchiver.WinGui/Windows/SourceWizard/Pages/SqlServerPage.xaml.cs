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
using ProcessDataArchiver.WinGui.Dialogs;
using System.Threading;
using System.Collections;
using ProcessDataArchiver.DataCore.Database.DbProviders;
using ProcessDataArchiver.DataCore.Infrastructure;
using ProcessDataArchiver.DataCore.Database.Schema;
using System.IO;

namespace ProcessDataArchiver.WinGui.Windows.SourceWizard.Pages
{
    /// <summary>
    /// Interaction logic for SqlServerPage.xaml
    /// </summary>
    public partial class SqlServerPage : Page,INotifyPropertyChanged,INotifyDataErrorInfo,IDbConfigPage
    {
        private string serverName;
        private string userName;
        private string databaseName;
        private string dbPath;
        private string auth = "Windows";
        private System.Data.SqlClient.SqlConnectionStringBuilder builder;
        private string connectionString;
        private IDatabaseProvider provider;
        private DatabaseProviderFactory DatabaseProviderFactory;
        CancellationTokenSource cts = new CancellationTokenSource();
        private System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog();

        private bool loaded = false;
        private bool changed = true;
        private bool firstRefServers = false;
        private bool firstRefDb = false;

        public event EventHandler<bool> connectEv;

        #region Properties
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
                OnPropertyChanged(nameof(DatabaseName));

            }
        }
        public string DbPath
        {
            get
            {
                return dbPath;
            }

            set
            {
                dbPath = value;
                Validate(nameof(DbPath));
                OnPropertyChanged(nameof(DbPath));
            }
        }




        public IDatabaseProvider DataSource { get; private set; }

        public bool HasErrors
        {
            get
            {
                return ErrorsList.Count > 0;
            }
        }
        public ObservableCollection<ErrorInfo> ErrorsList { get; set; } =
            new ObservableCollection<ErrorInfo>();

        public ObservableCollection<string> Databases { get; private set; } = new ObservableCollection<string>();

        public ObservableCollection<string> Servers { get; private set; } = new ObservableCollection<string>();

        #endregion


        #region Events

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<bool> ValidationChanged;
        public event EventHandler<bool> ConnectionStateChanged;
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        private void OnErrorChanged(string property)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(property));
        }

        private void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
            if(property !=  "DatabaseName")
                changed = true;
        }

        #endregion


        public SqlServerPage()
        {
            InitializeComponent();
            loaded = true;
            DatabaseProviderFactory = new DatabaseProviderFactory();
            this.DataContext = this;
            LoadDefaultSettings();

            dialog.Filter = "Database files *.mdf|*mdf";
            dialog.InitialDirectory= "C:\\";
            
        }

        public SqlServerPage(string connStr)
        {
            InitializeComponent();
            loaded = true;
            DatabaseProviderFactory = new DatabaseProviderFactory();
            this.DataContext = this;
            // LoadDefaultSettings();
            LoadSettings(connStr);

            dialog.Filter = "Database files *.mdf|*mdf";
            dialog.InitialDirectory = "C:\\";
            
        }



        #region Enable


        private void Disable()
        {
            ServerTb.Foreground = Brushes.Gray;
            ServerComboBox.IsEnabled = false;
            AuthCb.IsEnabled = false;
            AuthCb.Foreground = Brushes.Gray;
            DbGroup.Foreground = Brushes.Gray;
            DatabaseCb.IsEnabled = false;

            LoginGroup.Foreground = Brushes.Gray;
            AuthTb.Foreground = Brushes.Gray;
            UserTb.Foreground = Brushes.Gray;
            PassTb.Foreground = Brushes.Gray;
            UserTextBox.IsEnabled = false;
            PassBox.IsEnabled = false;

            DbNameRadio.IsEnabled = false;
            DbNameRadio.Foreground = Brushes.Gray;
            ExistingDbRadio.IsEnabled = false;
            ExistingDbRadio.Foreground = Brushes.Gray;

            ExiDbBrowseButton.IsEnabled = false;
            RefreshServersButton.IsEnabled = false;
            DbListRefreshButton.IsEnabled = false;
            ExiDbPathTextBox.IsEnabled = false;

            TestButton.IsEnabled = false;
 

            ConnectionStateChanged?.Invoke(this,false);
        }

        private void Enable()
        {
            ServerTb.Foreground = Brushes.Black;
            ServerComboBox.IsEnabled = true;
            AuthCb.IsEnabled = true;
            AuthCb.Foreground = Brushes.Black;
            DbGroup.Foreground = Brushes.Black;
            AuthTb.Foreground = Brushes.Black;

            LoginGroup.Foreground = Brushes.Black;

            if ( auth != "Windows")
            {
                UserTb.Foreground = Brushes.Black;
                PassTb.Foreground = Brushes.Black;
                UserTextBox.IsEnabled = true;
                PassBox.IsEnabled = true;
            }

            DbNameRadio.IsEnabled = true;
            DbNameRadio.Foreground = Brushes.Black;
            ExistingDbRadio.IsEnabled = true;
            ExistingDbRadio.Foreground = Brushes.Black;
            RefreshServersButton.IsEnabled = true;

            if (ExistingDbRadio.IsChecked == true)
            {
                ExiDbBrowseButton.IsEnabled = true;
                ExiDbPathTextBox.IsEnabled = true;
            }
            else
            {
                DbListRefreshButton.IsEnabled = true;
                DatabaseCb.IsEnabled = true;
            }

            EnableTestButton();
            ConnectionStateChanged?.Invoke(this, true);
            ValidationChanged?.Invoke(this, !HasErrors);
        }

        private void EnableTestButton()
        {
            if (!HasErrors || (ErrorsList.Count == 1 &&
                (ErrorsList.Where(p => p.Propery == "DatabaseName" && p.Message!="Name").FirstOrDefault() != null)||
                 ErrorsList.Where(p => p.Propery == "DbPath" && p.Message!="Folder" && p.Message!="Extension").
                 FirstOrDefault() != null))
                TestButton.IsEnabled = true;
            else
                TestButton.IsEnabled = false;
        }

        #endregion

        #region GuiEvents

        private void AuthCb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (loaded)
            {

                var cb = sender as ComboBox;
                var item = cb.SelectedItem as ComboBoxItem;
                auth = item.Content.ToString();


                if (cb.SelectedIndex == 0)
                {
                    UserTb.Foreground = Brushes.Gray;
                    PassTb.Foreground = Brushes.Gray;
                    UserTextBox.IsEnabled = false;
                    PassBox.IsEnabled = false;

                    UserTextBox.Text = "";
                    PassBox.Password = "";
                    RemoveError("PasswordBox");
                    PassBorder.BorderBrush = Brushes.Transparent;
                    PassBox.ToolTip = null;
                }
                else
                {
                    var usr = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
                    UserName = usr;
                    UserTb.Foreground = Brushes.Black;
                    PassTb.Foreground = Brushes.Black;
                    UserTextBox.IsEnabled = true;
                    PassBox.IsEnabled = true;
                    if (PassBox.Password.Length < 1)
                    {
                        AddError("PasswordBox", "Należy podać hasło");
                    }
                    else
                    {
                        RemoveError("PasswordBox");
                    }
                }
                Validate(nameof(UserName));
                EnableTestButton();
            }

        }



        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            var pb = sender as PasswordBox;
            if (AuthCb.SelectedIndex == 1)
            {
                
                if (pb.Password.Length < 1)
                {
                    AddError("PasswordBox", "Należy podać hasło");
                    PassBorder.BorderBrush = Brushes.Red;
                    pb.ToolTip = "Nie podano hasła";
                    AddError("PasswordBox","Nie podano hasła");
                    //pb.Style.Resources.Add("SystemColors.HighlightBrushKey", Brushes.Yellow);
                }
                else
                {
                    RemoveError("PasswordBox");
                    PassBorder.BorderBrush = Brushes.Transparent;
                    pb.ToolTip = null;
                    RemoveError("PasswordBox");
                }
            }
            else
            {
                if (ErrorsList.Select(er => er.Propery).Contains("PasswordBox"))
                {
                    RemoveError("PasswordBox");
                    PassBorder.BorderBrush = Brushes.Transparent;
                    pb.ToolTip = null;
                    RemoveError("PasswordBox");
                }
            }
            ValidationChanged?.Invoke(this, !HasErrors);
            EnableTestButton();
        }



        private async void DatabaseCb_DropDownOpened(object sender, EventArgs e)
        {
            if (changed)
            {
                changed = false;
                await LoadDatabases();
                DatabaseCb.IsDropDownOpen = true;
               
            }
        }

        private void Radio_Checked(object sender, RoutedEventArgs e)
        {
            if (loaded)
            {
                var radio = sender as RadioButton;

                if (radio.Equals(DbNameRadio))
                {
                    DatabaseCb.IsEnabled = true;
                    DbListRefreshButton.IsEnabled = true;

                    ExiDbBrowseButton.IsEnabled = false;
                    ExiDbPathTextBox.IsEnabled = false;
                }
                else
                {
                    DatabaseCb.IsEnabled = false;
                    DbListRefreshButton.IsEnabled = false;

                    ExiDbBrowseButton.IsEnabled = true;
                    ExiDbPathTextBox.IsEnabled = true;
                }
                Validate(nameof(DatabaseName));
                Validate(nameof(DbPath));
            }
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
            if (!firstRefServers) 
                firstRefServers = true;
        }



        private void ExiDbBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            if(dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ExiDbPathTextBox.Text = dbPath = dialog.FileName;

            }
        }

        private async void DbListRefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            await LoadDatabases();
            firstRefDb = true;
        }

       


        private async void TestButton_Click(object sender, RoutedEventArgs e)
        {
            Disable();
            //  bool result = false;
            
            cts = new CancellationTokenSource();
            DetailsDialogBox dd = new DetailsDialogBox("Łączenie...", "Łączenie", cts);
            connectEv += dd.SetWindowState;
            bool result = false;
            
            dd.ShowAndInvoke();
            try
            {
              result = await TryConnect();
            }
            catch (ConnectionException ex)
            {
                dd.Details = ex.Message;
                dd.SetWindowState(this, false);
            }
            catch(TaskCanceledException exe)
            {
                dd.Details = exe.Message;
                dd.SetWindowState(this, false);
            }
            finally
            {
                connectEv -= dd.SetWindowState;
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


        #region Provider
        private async void LoadServers()
        {
            Disable();
            IEnumerable<string> devices = null;
            cts = new CancellationTokenSource();
            DetailsDialogBox dd = new DetailsDialogBox("Wyszukiwanie...", "Wyszukiwanie", cts);
            connectEv += dd.SetWindowState;

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
                connectEv -= dd.SetWindowState;
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

        private void EnableAfterCancel(object sender,EventArgs e)
        {
            Enable();
        }

        private void LoadDefaultSettings()
        {
            var set = DatabaseProviderFactory.DefaultSettings[DatabaseType.SqlServer];
            ServerName = set.DataSource;
            DatabaseName = set.Database;
            DbPath = "C:\\NowaBazadanych1.mdf";
        }

        private async Task<bool> TryConnect()
        {
            var settings = GetSettings(false);
            bool result = false;
            var dbs = await DatabaseProviderFactory.GetDatabaseNamesAsync(DatabaseType.SqlServer, settings,cts.Token);
            settings = GetSettings();


                if (DbNameRadio.IsChecked == true)
                {

                    if (!dbs.Select(d => d.ToLowerInvariant())
                        .Contains(DatabaseName.ToLowerInvariant()))
                    {
                        var res = MessageBox.Show("Baza danych o podanej nazwie nie istnieje, utworzyć?", "Baza",
                              MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (res == MessageBoxResult.Yes)
                        {
                            provider = await DatabaseProviderFactory.CreateProviderAsync(DatabaseType.SqlServer, settings,cts.Token);//
                            result = await provider.TryConnectAsync(cts.Token);
                        }
                    }
                    else
                    {
                        provider =await DatabaseProviderFactory.CreateProviderAsync(DatabaseType.SqlServer, settings,cts.Token);//
                        result = await provider.TryConnectAsync(cts.Token);
                    }
                }
                else
                {

                    string fileName = Path.GetFileNameWithoutExtension(DbPath).ToLowerInvariant();
                    if (!dbs.Select(s => s.ToLowerInvariant()).Contains(fileName)
                        && !File.Exists(settings.Database))
                    {
                        var res = MessageBox.Show("Baza danych o podanej nazwie nie istnieje, utworzyć?", "Baza",
                            MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (res == MessageBoxResult.Yes)
                        {
                            provider = DatabaseProviderFactory.CreateProvider(DatabaseType.SqlServer, settings);//
                            result = await provider.TryConnectAsync(cts.Token);
                        }
                    }
                    else if (dbs.Select(s => s.ToLowerInvariant()).Contains(fileName))
                    {
                        var res = MessageBox.Show("Baza danych o podanej nazwie juz istnieje, użyć?", "Baza",
                              MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (res == MessageBoxResult.Yes)
                        {
                            provider = await DatabaseProviderFactory.CreateProviderAsync(DatabaseType.SqlServer, settings, cts.Token);//
                            result = await provider.TryConnectAsync(cts.Token);
                        }
                    }
                    else if (!dbs.Select(s => s.ToLowerInvariant()).Contains(fileName) &&
                        !File.Exists(settings.Database))
                    {
                        provider = await DatabaseProviderFactory.CreateProviderAsync(DatabaseType.SqlServer, settings, cts.Token);//
                        result = await provider.TryConnectAsync(cts.Token);
                    }
                }
            

            connectEv?.Invoke(this, result);
            return result;
        }

        private ConnectionSettings GetSettings(bool includeDb = true)
        {
            var sett = new ConnectionSettings
            {
                DataSource = ServerName,
                User = UserName,
                Password = PassBox.Password
            };
            if (includeDb)
            {
                if (DbNameRadio.IsChecked == true)
                    sett.Database = DatabaseName;
                else
                    sett.Database = DbPath;
            }
            return sett;
        }



        private async Task LoadDatabases()
        {
            if (!HasErrors || (ErrorsList.Count == 1 && ErrorsList
                .Where(p => p.Propery == "DatabaseName").FirstOrDefault() != null))
            {
                Disable();
                var sett = GetSettings(false);
                Databases.Clear();
                cts = new CancellationTokenSource();
                DetailsDialogBox dd = new DetailsDialogBox("Wyszukiwanie baz danych...", "Wyszukiwanie", cts);
                connectEv += dd.SetWindowState;
                IEnumerable<string> databases = null;

                dd.ShowAndInvoke();

                try
                {
                    databases = await DatabaseProviderFactory.GetDatabaseNamesAsync(DatabaseType.SqlServer, sett, cts.Token);
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
                    connectEv -= dd.SetWindowState;
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

        #endregion


        #region Validate

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
                    if (auth == "SQL Server" &&
                        string.IsNullOrEmpty(UserName))
                        error = "Nazwa użytkownika nie moze być pusta";
                    break;

                case nameof(DatabaseName):
                    if (DbNameRadio.IsChecked == true)
                    {
                        if (string.IsNullOrEmpty(DatabaseName))
                            error = "Name";
                        else
                        {
                            var match = DatabaseName.IndexOfAny
                                (new char[] { '*', '&', '/', '\\', '#', '(', ')', '^', '<', '>' }) != -1;
                            if (match)
                                error = "Name";
                        }
                    }
                    break;
                case nameof(DbPath):
                    if (ExistingDbRadio.IsChecked == true)
                    {
                        if (!FileHelper.IsValidPath(DbPath))
                            error = "Folder";
                        if (Path.GetExtension(DbPath) != ".mdf")
                            error = "Extension";
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



        public IEnumerable GetErrors(string propertyName)
        {
            return ErrorsList.Where(e => e.Propery == propertyName)
                .Select(e => e.Message).ToList();
        }

        #endregion

        public async Task<bool> Finish()
        {
            Disable();
            bool result = false;
      //      provider = GetProvider();
            //  result = await provider.TryConnectAsync(cts.Token);
            cts = new CancellationTokenSource();
            DetailsDialogBox dd = new DetailsDialogBox("Łączenie...", "Łączenie", cts);
      //      ConnectEvent += dd.SetWindowState;

            dd.ShowAndInvoke();
            try
            {
                result = await TryConnect();
             //   ConnectEvent?.Invoke(this, result);
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
             //   ConnectEvent -= dd.SetWindowState;
            }
            Enable();
            DataSource = provider;
            return result;
        }

        private void LoadSettings(string connStr)
        {
            var sett = DatabaseProviderFactory.GetSettings(DatabaseType.SqlServer, connStr);

            ServerName = sett.DataSource;

            if(!string.IsNullOrEmpty(sett.User))
            {
                AuthCb.SelectedIndex = 1;
                UserName = sett.User;
                PassBox.Password = sett.Password;
            }

            if (File.Exists(sett.Database))
            {
                ExistingDbRadio.IsChecked = true;
                DbPath = sett.Database;
            }
            else
            {
                DbNameRadio.IsChecked = true;
                DatabaseName = sett.Database;
            }

        }

        public void Validate()
        {
            Validate(nameof(ServerName));
            Validate(nameof(DatabaseName));
            Validate(nameof(DbPath));
            Validate(nameof(UserName));
        }


    }
}
