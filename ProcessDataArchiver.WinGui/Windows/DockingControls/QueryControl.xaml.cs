using ProcessDataArchiver.DataCore.Database.CommandProviders;
using ProcessDataArchiver.DataCore.Database.DbProviders;
using ProcessDataArchiver.DataCore.Database.Schema;
using ProcessDataArchiver.DataCore.DbEntities;
using ProcessDataArchiver.DataCore.Infrastructure;
using ProcessDataArchiver.WinGui.ViewModels;
using ProcessDataArchiver.WinGui.Windows.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
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
using System.Xml.Serialization;

namespace ProcessDataArchiver.WinGui.Windows.DockingControls
{
    /// <summary>
    /// Interaction logic for QueryControl.xaml
    /// </summary>
    public partial class QueryControl : UserControl,INotifyPropertyChanged,IQueryCreatorCommands
    {
        private EntityContext cnx = EntityContext.GetContext();

        private string selectedTable;
        private bool loaded;
        private string fileName;
        private DatabaseSchema dbSchema;

        private DataTable qData;
        private DataView qResult;
        private QueryOptions options;
        private EntityContext context;
        private TimeSpan elapsedTime;


        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler StateChanged;

        public ObservableCollection<string> ColumnNames { get; set; } 
            = new ObservableCollection<string>();

        public ObservableCollection<QueryInfoViewModel> QueryParameters { get; set; } 
            = new ObservableCollection<QueryInfoViewModel>();

        public ObservableCollection<string> AggregateTypes { get; set; } =
            new ObservableCollection<string>();

        public ObservableCollection<string> SortTypes { get; set; } =
            new ObservableCollection<string>();

        private CancellationTokenSource cts = new CancellationTokenSource();

        public DataView QueryResult
        {
            get { return qResult; }

            set
            {
                qResult = value;
                PropertyChanged?.Invoke(this,
                    new PropertyChangedEventArgs("QueryResult"));
            }
        }

        public DataTable QueryData
        {
            get { return qData; }
            set
            {
                qData = value;
                QueryResult = qData.DefaultView;
            }
        }
        public TimeSpan ElapsedTime
        {
            get { return elapsedTime; }
            set
            {
                elapsedTime = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ElapsedTime)));
            }
        }

        public QueryControl()
        {
            InitializeComponent();
            context = EntityContext.GetContext();
            this.DataContext = this;
            loaded = true;
            Prepare();


        }


        private void OpenCmdButton_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new System.Windows.Forms.OpenFileDialog();
            ofd.Filter = "XML Files|*.xml";
            ofd.ShowDialog();

            var serializer = new XmlSerializer(typeof(QueryOptions));
            options = serializer.Deserialize(ofd.OpenFile()) as QueryOptions;           

        }


        private async void Prepare()
        {


            var db = EntityContext.GetContext().DbProvider;
            try
            {
                var res = await db.TryConnectAsync();
            }
            catch (ConnectionException ex)
            {
                StatusTb.Text = "Wystąpił błąd podczas próby połączenia z bazą";
                DbConImg.Visibility = Visibility.Hidden;
                DbErrImg.Visibility = Visibility.Visible;
            }


            dbSchema = await cnx.DbProvider.GetDatabaseSchemaAsync();

            AggregateTypes.Add("-");
            AggregateTypes.Add("SUM");
            AggregateTypes.Add("MIN");
            AggregateTypes.Add("MAX");
            AggregateTypes.Add("AVG");
            AggregateTypes.Add("COUNT");

            SortTypes.Add("-");
            SortTypes.Add("Rosnąco");
            SortTypes.Add("Malejąco");

            var schema = await EntityContext.GetContext().DbProvider.GetDatabaseSchemaAsync();
            TableSelectCb.ItemsSource = schema.Tables.Select(s => s.TableName);
            TableSelectCb.SelectedIndex = 0;


        }

        




        private void CreateQueryOptions()
        {
            var qOption = new QueryOptions();
            qOption.ColumnNames = QueryParameters.Where(p=>p.Selected).Select(p => p.ColumnName).ToList();
            qOption.TableName = selectedTable;

            qOption.Aggregation= QueryParameters.Where(p =>!p.AggregateType.Equals("-"))
                .Select(p => new { ColumnName = p.ColumnName, AggregateType = p.AggregateType }).
                ToDictionary(k=>k.ColumnName,e=>e.AggregateType);

            qOption.Conditions = QueryParameters.Where(p => { return !string.IsNullOrEmpty(p.AndCriteria) ||
                !string.IsNullOrEmpty(p.OrCriteria); }).
                Select(p => new SqlCondition
                {
                    ColumnName = p.ColumnName,
                    AndCondition = p.AndCriteria,
                    OrCondition = p.OrCriteria
                }).ToList();

            qOption.OrderBy = QueryParameters.Where(p => !p.SortType.Equals("-"))
                .ToDictionary(k => k.ColumnName, (e) => { return e.SortType.Equals("Rosnąco") ? "Asc" : "Desc"; });

            options = qOption;
        }

        private void DeserializeQueryOptions()
        {
            var columnNames = options.ColumnNames
            .Union(options.Aggregation.Keys)
            .Union(options.Conditions.Select(o => o.ColumnName))
            .Union(options.OrderBy.Keys).Distinct();

            QueryParameters.Clear();

            foreach (var cName in columnNames)
            {
                var par = new QueryInfoViewModel { ColumnName = cName };
                if (options.Aggregation.ContainsKey(cName))
                {
                    par.AggregateType = options.Aggregation[cName];
                }
                else
                {
                    par.AggregateType = "-";
                }

                if (options.OrderBy.ContainsKey(cName))
                {
                    par.SortType = options.OrderBy[cName] == "Asc" ? "Rosnąco" : "Malejąco";
                }
                else
                {
                    par.SortType = "-";
                }

                foreach (var con in options.Conditions)
                {
                    if (con.ColumnName.Equals(cName))
                    {
                        par.AndCriteria = con.AndCondition;
                        par.OrCriteria = con.OrCondition;
                    }
                }

                if (options.ColumnNames.Contains(cName))
                    par.Selected = true;

                QueryParameters.Add(par);
            }
        }


        private void FillWithData()
        {
            var columnNames = options.ColumnNames
                .Union(options.Aggregation.Keys)
                .Union(options.Conditions.Select(o => o.ColumnName))
                .Union(options.OrderBy.Keys).Distinct();

            foreach (var cn in columnNames)
            {
                var par = new QueryInfoViewModel { ColumnName = cn };
                par.Selected = options.ColumnNames.Contains(cn);
                par.SortType = options.OrderBy[cn];
                par.AggregateType = options.Aggregation[cn];

                if (options.Conditions.Select(c => c.ColumnName).Contains(cn))
                {
                    par.AndCriteria = options.Conditions.Where(c => c.ColumnName.Equals(cn)).
                        Select(p => p.AndCondition).FirstOrDefault();

                    par.OrCriteria = options.Conditions.Where(c => c.ColumnName.Equals(cn)).
                    Select(p => p.OrCondition).FirstOrDefault();
                }
                QueryParameters.Add(par);
            }
        }

        public bool CanExport
        {
            get { return QueryData != null; }
        }

        public bool CanRunQuery
        {
            get
            {
                return QueryListView.Items.Count > 0;

            }
        }

        public bool CanSaveQuery
        {
            get
            {
                return QueryListView.Items.Count > 0;
            }
        }

        public bool CanAdd
        {
            get
            {
                return true;
            }
        }

        public bool CanRemove
        {
            get
            {
              return  QueryListView.SelectedItem != null;
            }
        }

        public async void Export()
        {
            var saveFile = new System.Windows.Forms.SaveFileDialog();
            saveFile.DefaultExt = "csv";
            saveFile.Filter = "CSV file (*.csv)|*csv | Excel file (*.xls)|*xls | XML file (*.xml)|*.xml";
            if (saveFile.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                await DataExportProvider.ExportDataAsync(QueryData, saveFile.FileName);
            }
        }

        public async void ChangeTable(string tableName)
        {
            selectedTable = tableName;

            
            ColumnNames.Clear();
            dbSchema = await EntityContext.GetContext().DbProvider.GetDatabaseSchemaAsync();
            var table = dbSchema.Tables.Where(t => t.TableName.Equals(tableName)).FirstOrDefault();
            foreach (var item in table.Columns)
            {
                ColumnNames.Add(item.ColumnName);
            }
        }

        public async void RunQuery()
        {


            CreateQueryOptions();

            if (options.ColumnNames == null || options.ColumnNames.Count() == 0)
            {
                MessageBox.Show("Należy wybrać przynajmniej jedną kolumnę do wyświetlenia", "Błąd",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                try
                {
                    Stopwatch sw = new Stopwatch();
                    DataTable ds = new DataTable();
                    Disable();
                    sw.Start();
                    int i = 0;
                    await Task.Run(async() =>
                    {
                       i = await context.DbProvider.FillDataSetAsync(options, ds);
                    });
                    sw.Stop();
                    Enable();
                    if (ds.Rows != null && ds.Rows.Count > 0)
                        QueryData = ds;

                    RowCountTb.Text = $"{i} wierszy";

                    ElapsedTime = sw.Elapsed;
                    StatusTb.Text = "Polecenie wykonano pomyślnie";
                    OkImg.Visibility = Visibility.Visible;
                    WarnImg.Visibility = Visibility.Hidden;
                    DbConImg.Visibility = Visibility.Hidden;


                }
                catch (ConnectionException ex)
                {
                    Enable();
                    RowCountTb.Text = $"0 wierszy";
                    ElapsedTime = new TimeSpan(0);
                    StatusTb.Text = "Polecenie SQL zawiera błędy";
                    DbConImg.Visibility = Visibility.Hidden;
                    OkImg.Visibility = Visibility.Hidden;
                    WarnImg.Visibility = Visibility.Visible;
                }

            }

            if (QueryData != null && QueryData.Rows.Count > 0)
                ExportButton.IsEnabled = true;
            else
                ExportButton.IsEnabled = false;

        }




        public async Task RunQueryAsync()
        {


            CreateQueryOptions();

            if (options.ColumnNames == null || options.ColumnNames.Count() == 0)
            {
                MessageBox.Show("Należy wybrać przynajmniej jedną kolumnę do wyświetlenia", "Błąd",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                try
                {
                    Stopwatch sw = new Stopwatch();
                    DataTable ds = new DataTable();
                    Disable();
                    sw.Start();
                    int i = 0;

                        i = await context.DbProvider.FillDataSetAsync(options, ds);

                    sw.Stop();
                    Enable();
                    if (ds.Rows != null && ds.Rows.Count > 0)
                        QueryData = ds;

                    RowCountTb.Text = $"{i} wierszy";

                    ElapsedTime = sw.Elapsed;
                    StatusTb.Text = "Polecenie wykonano pomyślnie";
                    OkImg.Visibility = Visibility.Visible;
                    WarnImg.Visibility = Visibility.Hidden;
                    DbConImg.Visibility = Visibility.Hidden;


                }
                catch (ConnectionException ex)
                {
                    Enable();
                    RowCountTb.Text = $"0 wierszy";
                    ElapsedTime = new TimeSpan(0);
                    StatusTb.Text = "Polecenie SQL zawiera błędy";
                    DbConImg.Visibility = Visibility.Hidden;
                    OkImg.Visibility = Visibility.Hidden;
                    WarnImg.Visibility = Visibility.Visible;
                }

            }

            if (QueryData != null && QueryData.Rows.Count > 0)
                ExportButton.IsEnabled = true;
            else
                ExportButton.IsEnabled = false;

        }









        public void SaveQuery()
        {

            CreateQueryOptions();

            if (string.IsNullOrEmpty(fileName))
            {
                var sfd = new System.Windows.Forms.SaveFileDialog();
                sfd.Filter = "Query File (*.q)|*.q";
                if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {

                    using (var stream = File.Create(sfd.FileName))
                    {
                        BinaryFormatter bf = new BinaryFormatter();
                        bf.Serialize(stream, options);
                    }
                    fileName = sfd.FileName;

                }
                
            }
            else
            {

                XmlSerializer xml = new XmlSerializer(options.GetType());
                xml.Serialize(File.Create(fileName), options);

            }
        }

        public void OpenQuery()
        {
            var ofd = new System.Windows.Forms.OpenFileDialog();
            ofd.Filter = "Query files (*.q)|*.q";

            if(ofd.ShowDialog()== System.Windows.Forms.DialogResult.OK)
            {
                using (var stream = File.OpenRead(ofd.FileName))
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    options = (QueryOptions)bf.Deserialize(stream);
                }
                selectedTable = options.TableName;
                DeserializeQueryOptions();
            }
        }

        public void Add()
        {
            QueryParameters.Add(new QueryInfoViewModel()
            {
                AggregateType = AggregateTypes[0],
                AndCriteria = "",
                OrCriteria = "",
                SortType = SortTypes[0]
            });
        }

        private void QueryListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = QueryListView.SelectedItem;
            StateChanged?.Invoke(this, EventArgs.Empty);
            if (selected != null)
            {
                RemoveBtn.IsEnabled = true;
            }
            else
            {
                RemoveBtn.IsEnabled = false;
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            Export();
        }

        private void LoadQueryButton_Click(object sender, RoutedEventArgs e)
        {
            OpenQuery();
        }

        private void SaveQueryButton_Click(object sender, RoutedEventArgs e)
        {
            SaveQuery();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            Add();
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            Remove();
        }

        private void Cb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string table = TableSelectCb.SelectedItem.ToString();

            ChangeTable(table);
        }

        private async void RunQueryButton_Click(object sender, RoutedEventArgs e)
        {
            await RunQueryAsync();
        }

        public void Remove()
        {
            var selected = QueryListView.SelectedItem as QueryInfoViewModel;
            if (selected != null)
            {
                QueryParameters.Remove(selected);
            }
        }

        private void StopQueryButton_Click(object sender, RoutedEventArgs e)
        {
            cts.Cancel();
        }
        private void Enable()
        {
            RunQueryButton.IsEnabled = true;
            StopQueryButton.IsEnabled = false;
        }
        private void Disable()
        {
            RunQueryButton.IsEnabled = false;
            StopQueryButton.IsEnabled = true;
            StatusTb.Text = "Wykonywanie polecenia...";
            OkImg.Visibility = Visibility.Hidden;
            WarnImg.Visibility = Visibility.Hidden;
            DbConImg.Visibility = Visibility.Hidden;

        }

    }
}
