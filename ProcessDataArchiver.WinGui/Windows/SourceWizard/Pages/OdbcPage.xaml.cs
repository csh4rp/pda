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
using ProcessDataArchiver.DataCore.Database.DbProviders;
using System.Collections.ObjectModel;
using ProcessDataArchiver.DataCore.Database.Schema;
using System.ComponentModel;
using ProcessDataArchiver.WinGui.Dialogs;
using System.Collections;
using System.Data.Odbc;
using System.Threading;

namespace ProcessDataArchiver.WinGui.Windows.SourceWizard.Pages
{
    /// <summary>
    /// Interaction logic for OdbcPage.xaml
    /// </summary>
    public partial class OdbcPage : Page,INotifyPropertyChanged,INotifyDataErrorInfo,IDbConfigPage
    {

        private IDatabaseProvider provider;
        private OdbcDataSourceInfo selectedOdbc;
        private CancellationTokenSource cts = new CancellationTokenSource();
        public event EventHandler<bool> ConnectEvent;
        public IDatabaseProvider DataSource { get; private set; }



        public OdbcDataSourceInfo SelectedOdbc
        {
            set
            {
                selectedOdbc = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(selectedOdbc)));
            }
            get
            {
                return selectedOdbc;
            }
        }

        public ObservableCollection<OdbcDataSourceInfo> OdbcList { get; set; } =
        new ObservableCollection<OdbcDataSourceInfo>();

        public bool HasErrors
        {
            get
            {
                return false;
            }
        }

        public OdbcPage()
        {
            InitializeComponent();
            PopulateDsnList();
            this.DataContext = this;
        }
        public OdbcPage(string cs)
        {
            InitializeComponent();
            PopulateDsnList();
            this.DataContext = this;

            OdbcConnectionStringBuilder odbc = new OdbcConnectionStringBuilder(cs);
            string dsn = odbc.Dsn;
            SelectedOdbc = OdbcList.Where(o => o.Dsn.Equals(dsn)).FirstOrDefault();
        }

        #region Events

        public event EventHandler<bool> ConnectionStateChanged;
        public event EventHandler<bool> ValidationChanged;
        public event PropertyChangedEventHandler PropertyChanged;
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

        private IDatabaseProvider GetProvider()
        {
            return DatabaseProviderFactory.CreateOdbcProvider(SelectedOdbc.Dsn, SelectedOdbc.Driver,
                UserTextBox.Text, PasswordBox.Password);
        }

        public async Task<bool> Finish()
        {
            Disable();
            provider = GetProvider();
            bool result = false;
            //  result = await provider.TryConnectAsync(cts.Token);
            cts = new CancellationTokenSource();
            DetailsDialogBox dd = new DetailsDialogBox("Łączenie...", "Łączenie", cts);
            ConnectEvent += dd.SetWindowState;

            dd.ShowAndInvoke();
            try
            {
                result = await provider.TryConnectAsync(cts.Token);
                ConnectEvent?.Invoke(this, result);
                //ConnectEvent?
                // dd.Close();
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
            Enable();
            DataSource = GetProvider();
            return result;
        }

        private void PopulateDsnList()
        {
            var comp = StringComparer.InvariantCultureIgnoreCase;
            var dsn = DatabaseProviderFactory.GetOdbcDataSources();

            var filteredDsn = dsn.Where(FindSources);

            OdbcList.Clear();
            foreach (var item in filteredDsn)
            {
                OdbcList.Add(item);
            }
            SelectedOdbc = OdbcList[0];
        }

        private bool FindSources(OdbcDataSourceInfo info)
        {
            var driver = info.Driver.ToLower();
            if (driver.Contains("firebird") || driver.Contains("mysql") || driver.Contains("postgres")
                || driver.Contains("oracle") || driver.Contains("sql server"))
                return true;
            return false;
        }

        private void RefreshDsnBtn_Click(object sender, RoutedEventArgs e)
        {
            PopulateDsnList();
        }

        private async void TestButton_Click(object sender, RoutedEventArgs e)
        {
            Disable();
            provider = GetProvider();
            bool result = false;
          //  result = await provider.TryConnectAsync(cts.Token);
            cts = new CancellationTokenSource();
            DetailsDialogBox dd = new DetailsDialogBox("Łączenie...", "Łączenie", cts);
            ConnectEvent += dd.SetWindowState;

            dd.ShowAndInvoke();
            try
            {
                result = await provider.TryConnectAsync(cts.Token);
                ConnectEvent?.Invoke(this, result);
                //ConnectEvent?
               // dd.Close();
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
            //bool res = await TryConnect();
            //if (res)
            //    MessageBox.Show("Próba nawiązania połączenia przebiegła pomyślnie", "Połączono",
            //        MessageBoxButton.OK, MessageBoxImage.Information); 
            Enable();
        }

        private async Task<bool> TryConnect()
        {
            provider = GetProvider();
            bool res = false;
            try
            {
                res = await provider.TryConnectAsync();
            }
            catch (ConnectionException ex)
            {
                DetailsDialogBox dialog = new DetailsDialogBox();
                dialog.Message = "Wystąpił błąd podczas próby nawiązania połączenia";
                dialog.Details = ex.Message;
                dialog.Show();
            }

            return res;
        }


        private void Disable()
        {
            NameTb.Foreground = Brushes.Gray;
            DsnComboBox.IsEnabled = false;
            UserTb.Foreground = Brushes.Gray;
            UserTextBox.IsEnabled = false;
            PassTb.Foreground = Brushes.Gray;
            PasswordBox.IsEnabled = false;
            TestButton.IsEnabled = false;
            RefreshDsnBtn.IsEnabled = false;
            ConnectionStateChanged?.Invoke(this, false);
        }

        private void Enable()
        {
            NameTb.Foreground = Brushes.Black;
            DsnComboBox.IsEnabled = true;
            UserTb.Foreground = Brushes.Black;
            UserTextBox.IsEnabled = true;
            PassTb.Foreground = Brushes.Black;
            PasswordBox.IsEnabled = true;
            TestButton.IsEnabled = true;
            RefreshDsnBtn.IsEnabled = true;
            ConnectionStateChanged?.Invoke(this, true);
        }

        public IEnumerable GetErrors(string propertyName)
        {
            return string.Empty;
        }
        public void Validate()
        {

        }
    }
}
