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
using System.Data.OleDb;
using ProcessDataArchiver.WinGui.Windows.Dialogs;
using System.Threading;

namespace ProcessDataArchiver.WinGui.Windows.SourceWizard.Pages
{
    /// <summary>
    /// Interaction logic for AccessPage.xaml
    /// </summary>
    public partial class AccessPage : Page,INotifyPropertyChanged,INotifyDataErrorInfo,IDbConfigPage
    {
        private DatabaseProviderFactory factory;
        private IDatabaseProvider provider;
        private OleDbConnectionStringBuilder builder;
        private bool loaded;
        private string databasePath;
        private string dbName;
        private string folderPath;

        private System.Windows.Forms.OpenFileDialog dialog = 
            new System.Windows.Forms.OpenFileDialog();

        private System.Windows.Forms.FolderBrowserDialog folderDialog = 
            new System.Windows.Forms.FolderBrowserDialog();
        public event EventHandler<bool> ConnectEvent;
        private string connectionString;
        public event EventHandler<bool> connectEv;
        private CancellationTokenSource cts = new CancellationTokenSource();
        #region __Properties__

        public string DatabasePath
        {
            get
            {
                return databasePath;
            }

            set
            {
                databasePath = value;                
                Validate(nameof(DatabasePath));
                OnPropertyChanged(nameof(DatabasePath));
                
            }
        }

        public string DbName
        {
            get
            {
                return dbName;
            }

            set
            {

                dbName = value;
                Validate(nameof(DbName));
                OnPropertyChanged(nameof(DbName));
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
                Validate(nameof(FolderPath));
                OnPropertyChanged(nameof(FolderPath));
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

        private void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        #endregion

       
        public AccessPage()
        {
            InitializeComponent();
            this.DataContext = this;           
            dialog.Filter = "Access files (*.accdb, *.mdb)|*.accdb;*.mdb|All files (*.*)|*.*";           
            loaded = CheckIfLoaded();
            factory = new DatabaseProviderFactory();
            LoadDefaultSettings();

            NewDbRadio.IsChecked = true;
            Enable();
            ProtectCheck.IsChecked = false;
            Validate();
        }

        public AccessPage(string cs)
        {
            InitializeComponent();
            this.DataContext = this;
            ExistingDbRadio.IsChecked = true;
            dialog.Filter = "Access files (*.accdb, *.mdb)|*.accdb;*.mdb|All files (*.*)|*.*";
            loaded = CheckIfLoaded();
            factory = new DatabaseProviderFactory();

            connectionString = cs;
            LoadLayout(cs);
          //  LoadDefaultSettings();
        }


        #region __Interfaces__
        public IEnumerable GetErrors(string propertyName)
        {
            return ErrorsList.Where(p => p.Propery == propertyName).Select(p => p.Message).ToList();
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
            if(result)
                DataSource = provider;
            Enable();
            return result;
        }


        #endregion


        #region __GuiOperations__

        private bool CheckIfLoaded()
        {
            return VersionCb != null;
        }



        private void Disable()
        {
            NewDbRadio.IsEnabled = false;
            ExistingDbRadio.IsEnabled = false;

            DbPathTb.Foreground = Brushes.Gray;
            DbPathTextBox.IsEnabled = false;
            PathDbButton.IsEnabled = false;
            PassTb.Foreground = Brushes.Gray;
            PassBox.IsEnabled = false;

            ProtectCheck.IsEnabled = false;
            PassTb.Foreground = Brushes.Gray;
            PassBox.IsEnabled = false;
            ConfirmPassTb.Foreground = Brushes.Gray;
            ConfirmPassBox.IsEnabled = false;

            ConnectionStateChanged?.Invoke(this, false);
        }

        private void Enable()
        {
            NewDbRadio.IsEnabled = true;
            ExistingDbRadio.IsEnabled = true;

            if (NewDbRadio.IsChecked == true)
            {
                DbPathTb.Foreground = Brushes.Gray;
                DbPathTextBox.IsEnabled = false;
                PathDbButton.IsEnabled = false;

                NameTb.Foreground = Brushes.Black;
                PathTb.Foreground = Brushes.Black;
                VersionTb.Foreground = Brushes.Black;

                NameTextBox.IsEnabled = true;
                NewDbPathTextBox.IsEnabled = true;
                VersionCb.IsEnabled = true;
                VersionCb.Foreground = Brushes.Black;
                NewDbPathButton.IsEnabled = true;

                ProtectCheck.Visibility = Visibility.Visible;
                ProtectCheck.IsEnabled = true;

                Grid.SetRow(PassTb, 3);
                Grid.SetRow(PassBox, 3);
                Grid.SetRow(PassBor1, 3);
          //      Grid.SetRow(PassBox, 3);
                ConfirmPassTb.Visibility = Visibility.Visible;
                ConfirmPassBox.Visibility = Visibility.Visible;

            }
            else
            {
                DbPathTb.Foreground = Brushes.Black;
                DbPathTextBox.IsEnabled = true;
                PathDbButton.IsEnabled = true;


                NameTb.Foreground = Brushes.Gray;
                PathTb.Foreground = Brushes.Gray;

                VersionTb.Foreground = Brushes.Gray;

                NameTextBox.IsEnabled = false;
                NewDbPathTextBox.IsEnabled = false;

                VersionCb.IsEnabled = false;
                VersionCb.Foreground = Brushes.Gray;
                NewDbPathButton.IsEnabled = false;

                ProtectCheck.Visibility = Visibility.Hidden;
                ProtectCheck.IsEnabled = false;

                Grid.SetRow(PassTb, 1);
                Grid.SetRow(PassBox, 1);
                Grid.SetRow(PassBor1, 1);
                ConfirmPassTb.Visibility = Visibility.Hidden;
                ConfirmPassBox.Visibility = Visibility.Hidden;


            }

            EnableProtect();
            EnableTestButton();
            ResizeGroupBox();
            ConnectionStateChanged?.Invoke(this, true);
            ValidationChanged?.Invoke(this, !HasErrors);
        }

        private void EnableTestButton()
        {
            if (ErrorsList.Count > 0)
            {
                TestButton.IsEnabled = false;
            }
            else
            {
                TestButton.IsEnabled = true;
            }
        }

        private void EnableProtect()
        {
            if (ExistingDbRadio.IsChecked == true)
            {
                PassTb.Foreground = Brushes.Black;
                PassBox.IsEnabled = true;
            }
            else if (NewDbRadio.IsChecked == true)
            {
                if (ProtectCheck.IsChecked == true)
                {
                    PassTb.Foreground = Brushes.Black;
                    PassBox.IsEnabled = true;
                    ConfirmPassTb.Foreground = Brushes.Black;
                    ConfirmPassBox.IsEnabled = true;
                }
                else
                {
                    PassTb.Foreground = Brushes.Gray;
                    PassBox.IsEnabled = false;
                    ConfirmPassTb.Foreground = Brushes.Gray;
                    ConfirmPassBox.IsEnabled = false;
                }
            }
        }

        private void ResizeGroupBox()
        {
            if (ExistingDbRadio.IsChecked == true)
            {
                MainGrid.RowDefinitions[1].Height = new GridLength(76);
                for (int i = 3; i < 6; i++)
                {
                    ProtectGrid.RowDefinitions[i].Height = new GridLength(0);
                }
            }
            else
            {
                MainGrid.RowDefinitions[1].Height = new GridLength(133);

                ProtectGrid.RowDefinitions[3].Height = new GridLength(28);
                ProtectGrid.RowDefinitions[5].Height = new GridLength(28);
                ProtectGrid.RowDefinitions[4].Height = new GridLength(5);

            }
        }
        #endregion


        #region __Provider__


        private  IDatabaseProvider GetProvider()
        {
            ConnectionSettings sett = new ConnectionSettings();
            if (ExistingDbRadio.IsChecked == true)
            {
                sett.Database = DatabasePath;
                if (!string.IsNullOrEmpty(PassBox.Password))
                    sett.Password = PassBox.Password;

            }
            else
            {
                string fullPath = GetFullDbPath();

                sett.Database = fullPath;
                if (ProtectCheck.IsEnabled == true)
                    sett.Password = PassBox.Password;
            }

            return DatabaseProviderFactory.CreateProvider(DatabaseType.Access, sett);
        }

        private string GetFullDbPath()
        {
            if (!string.IsNullOrEmpty(FolderPath) && (!string.IsNullOrEmpty(DbName)))
            {
                string fullPath = Path.Combine(FolderPath, DbName);
                var content = (VersionCb.SelectedItem as ComboBoxItem).Content;
                if (content.ToString().Equals("Microsoft Office 2007"))
                    fullPath += ".accdb";
                else
                    fullPath += ".mdb";

                return fullPath;
            }
            return "";
        }



        private void LoadDefaultSettings()
        {
            var sett = DatabaseProviderFactory.DefaultSettings[DatabaseType.Access];
            DatabasePath = sett.Database;
            DbName = Path.GetFileNameWithoutExtension(DatabasePath);
            FolderPath = Path.GetDirectoryName(DatabasePath);
        }
        #endregion


        #region __GuiEvents___

        private void NewDbPathButton_Click(object sender, RoutedEventArgs e)
        {
            if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                FolderPath = folderDialog.SelectedPath;
            }
        }

        private void ProtectCheck_Checked(object sender, RoutedEventArgs e)
        {
            if (loaded)
            {
                ValidatePassword();
                EnableProtect();
            }
        }

        private void PathDbButton_Click(object sender, RoutedEventArgs e)
        {
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                DatabasePath = dialog.FileName;
            }
        }

        private void Radio_Checked(object sender, RoutedEventArgs e)
        {
            if (loaded)
            {
                Enable();
                PassBox.Password = "";
                ConfirmPassBox.Password = "";
                Validate(nameof(DatabasePath));
                Validate(nameof(DbName));
                Validate(nameof(FolderPath));
                ValidatePassword();

            }
        }


        private async void TestButton_Click(object sender, RoutedEventArgs e)
        {
            Disable();

                provider = GetProvider();
                bool connected = false;

                DetailsDialogBox dd = new DetailsDialogBox("Łączenie...", "Łączenie", cts);
                connectEv += dd.SetWindowState;
                bool result = false;
                dd.OperationCanceled += EnableAfterCancel;
                dd.ShowAndInvoke();
                try
                {
                    result = await GetProvider().TryConnectAsync(cts.Token);
                    connectEv?.Invoke(this, result);
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
                catch(Exception ae)
                {
                dd.Details = ae.Message;
                dd.SetWindowState(this, false);
                }
                finally
                {
                    connectEv -= dd.SetWindowState;
                    dd.OperationCanceled -= EnableAfterCancel;
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

        private void VersionCb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (loaded)
            {
                Validate(nameof(DbName));
                Validate(nameof(FolderPath));
            }

        }

        private void PassBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            ValidatePassword();
        }
        #endregion


        #region __Validation___
        private void Validate(string property)
        {
            string error = null;
            switch (property)
            {

                case nameof(DatabasePath):
                    if (ExistingDbRadio.IsChecked == true)
                    {
                        if (!File.Exists(DatabasePath))
                            error = "Plik nie istnieje";
                        else
                        {
                            string ext = Path.GetExtension(DatabasePath);
                            if (ext != ".mdb" && ext != ".accdb")
                                error = "Błędne rozszerzenie pliku";
                        }
                    }
                    break;

                case nameof(DbName):
                    if (NewDbRadio.IsChecked == true && string.IsNullOrEmpty(DbName))
                        error = "Nazwa bazy nie może być pusta";
                    else if (File.Exists(GetFullDbPath()))
                        error = "Baza o podanej nazwie już istnieje";
                    break;

                case nameof(FolderPath):
                    if (NewDbRadio.IsChecked == true && string.IsNullOrEmpty(FolderPath))
                        error = "Nazwa folderu nie może być pusta";
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

        private void ValidatePassword()
        {
            if (NewDbRadio.IsChecked == true && ProtectCheck.IsChecked == true)
            {
                if (string.IsNullOrEmpty(PassBox.Password) && string.IsNullOrEmpty(ConfirmPassBox.Password))
                {
                    AddError("Password", "Nie podano hasła");
                    PassBor1.BorderBrush = Brushes.Red;
                    PassBor2.BorderBrush = Brushes.Red;
                    PassBox.ToolTip = "Nie podano hasła";
                    ConfirmPassBox.ToolTip = "Nie podano hasła";
                }
                else if (!PassBox.Password.Equals(ConfirmPassBox.Password))
                {
                    AddError("Password", "Hasła się nie zgadzają");
               //     PassBor1.BorderBrush = Brushes.Red;
                    PassBor2.BorderBrush = Brushes.Red;
                    ConfirmPassBox.ToolTip = "Hasła się nie zgadzają";
                }
                else
                {
                    RemoveError("Password");
                    PassBor1.BorderBrush = Brushes.Transparent;
                    PassBor2.BorderBrush = Brushes.Transparent;
                    PassBox.ToolTip = null;
                    ConfirmPassBox.ToolTip = null;
                }
            }
            else
            {
                RemoveError("Password");
                PassBor1.BorderBrush = Brushes.Transparent;
                PassBor2.BorderBrush = Brushes.Transparent;
                PassBox.ToolTip = null;
                ConfirmPassBox.ToolTip = null;
            }
            ValidationChanged?.Invoke(this, !HasErrors);
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

        private void LoadLayout(string cs)
        {
            var sett = DatabaseProviderFactory.GetSettings(DatabaseType.Access, cs);

            DatabasePath = sett.Database;
        //    Enable();
            if (!string.IsNullOrEmpty(sett.Password))
                PassBox.Password = sett.Password;

        }

        public void Validate()
        {
            Validate(nameof(DbName));
            Validate(nameof(DatabasePath));
            Validate(nameof(FolderPath));
            if (NewDbRadio.IsChecked == true && ProtectCheck.IsChecked == true)
            {
                if (string.IsNullOrEmpty(PassBox.Password) && string.IsNullOrEmpty(ConfirmPassBox.Password))
                {
                    AddError("Password", "Nie podano hasła");
                }
                else if (!PassBox.Password.Equals(ConfirmPassBox.Password))
                {
                    AddError("Password", "Hasła się nie zgadzają");
                }
                else
                {
                    RemoveError("Password");
                }
            }
            else
            {
                RemoveError("Password");
            }
            ValidationChanged?.Invoke(this, !HasErrors);
        }

    }

    public class ErrorInfo
    {
        public string Propery { get; set; }
        public string Message { get; set; }
    }
}
