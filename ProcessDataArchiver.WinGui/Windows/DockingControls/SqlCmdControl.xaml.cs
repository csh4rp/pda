using ProcessDataArchiver.DataCore.DbEntities;
using ProcessDataArchiver.DataCore.Infrastructure;
using ProcessDataArchiver.WinGui.Windows.Commands;
using ProcessDataArchiver.WinGui.Windows.Dialogs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Transactions;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Data.Common;
using System.Text.RegularExpressions;
using WinFormsSyntaxHighlighter;
using ProcessDataArchiver.DataCore.Database.DbProviders;
using System.Timers;
using System.Diagnostics;
using FastColoredTextBoxNS;
using System.Reflection;
using System.Drawing;
using System.Threading;

namespace ProcessDataArchiver.WinGui.Windows.DockingControls
{
    /// <summary>
    /// Interaction logic for SqlCmdControl.xaml
    /// </summary>
    public partial class SqlCmdControl : UserControl, INotifyPropertyChanged, ISqlQueryCommands
    {
        private bool undoFlag = false;
        private EntityContext context = EntityContext.GetContext();
        private string fileName;

        private DataTable qData;
        private DataView qResult;

        private TimeSpan elapsedTime;

        public event PropertyChangedEventHandler PropertyChanged;
        private List<TextRange> text = new List<TextRange>();
        private List<TextRange> newtext = new List<TextRange>();


        private CommittableTransaction transaction;
        private DbConnection dbConnection;
        private FastColoredTextBox CmdTextBox;

        private CancellationTokenSource cts = new CancellationTokenSource();

        private static FastColoredTextBoxNS.Style KeyWordStyle = 
            new TextStyle(System.Drawing.Brushes.Blue, null, System.Drawing.FontStyle.Bold);
        private static FastColoredTextBoxNS.Style TextStyle =
            new TextStyle(System.Drawing.Brushes.DarkRed, null, System.Drawing.FontStyle.Regular);
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

        public event EventHandler StateChanged;
        private LoadingWindow loadWindow;
        public SqlCmdControl()
        {
            InitializeComponent();
            CmdTextBox = new FastColoredTextBox();
            CmdTextBox.ContextMenuStrip = GetMenu();
            CmdTextBox.TextChanged += CmdTextBox_TextChanged;
            CmdTextBox.KeyDown += CmdTextBox_KeyDown;
            WinFormsHost.Child = CmdTextBox;
            CmdTextBox.BeginAutoUndo();
           
            
                         
            this.DataContext = this;
            ElapsedTime = new TimeSpan(0);
            Setup();

        }

        private async void Setup()
        {
            var db = EntityContext.GetContext().DbProvider;
            try
            {
                var res = await db.TryConnectAsync();
            }
            catch(ConnectionException ex)
            {
                StatusTb.Text = "Wystąpił błąd podczas próby połączenia z bazą";
                DbConImg.Visibility = Visibility.Hidden;
                DbErrImg.Visibility = Visibility.Visible;
            }
        }

        private System.Windows.Forms.ContextMenuStrip GetMenu()
        {

            var menu = new System.Windows.Forms.ContextMenuStrip();

           var cut = new System.Windows.Forms.ToolStripMenuItem("Wytnij");
            cut.Click += (s, e) => { Cut(); };
            cut.ShowShortcutKeys = true;
            cut.ShortcutKeyDisplayString = "Ctrl+X";
            cut.Image = new Bitmap(
                (System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly()
                .Location))+@"/Resources/Icons/cut.png");
            menu.Items.Add(cut);

            var copy = new System.Windows.Forms.ToolStripMenuItem("Kopiuj");
            copy.Click += (s, e) => { Copy(); };
            copy.ShowShortcutKeys = true;
            copy.ShortcutKeyDisplayString = "Ctrl+C";
            copy.Image = new Bitmap(
                (System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly()
                .Location)) + @"/Resources/Icons/copy.png");
            menu.Items.Add(copy);

            var paste = new System.Windows.Forms.ToolStripMenuItem("Wklej");
            paste.Click += (s, e) => { Paste(); };
            paste.ShowShortcutKeys = true;
            paste.ShortcutKeyDisplayString = "Ctrl+V";
            paste.Image = new Bitmap(
                (System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly()
                .Location)) + @"/Resources/Icons/paste.png");
            menu.Items.Add(paste);


            menu.Opening += (s, e) => { SetCnxMenuEnable(); };
            return menu;
        }

        private void SetCnxMenuEnable()
        {
            if (this.CanCut)
            {
                CmdTextBox.ContextMenuStrip.Items[0].Enabled = true;
            }
            else
            {
                CmdTextBox.ContextMenuStrip.Items[0].Enabled = false;
            }
            if (this.CanCopy)
            {
                CmdTextBox.ContextMenuStrip.Items[1].Enabled = true;
            }
            else
            {
                CmdTextBox.ContextMenuStrip.Items[1].Enabled = false;
            }
            if (this.CanPaste)
            {
                CmdTextBox.ContextMenuStrip.Items[2].Enabled = true;
            }
            else
            {
                CmdTextBox.ContextMenuStrip.Items[2].Enabled = false;
            }
        }

        private void CmdTextBox_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode == System.Windows.Forms.Keys.Space ||
                e.KeyCode == System.Windows.Forms.Keys.Return ||
                e.KeyCode == System.Windows.Forms.Keys.Enter)
            {
                CmdTextBox.EndAutoUndo();
            }
            else if (e.KeyCode == System.Windows.Forms.Keys.Back ||
                e.KeyCode == System.Windows.Forms.Keys.Delete)
            {
                CmdTextBox.EndAutoUndo();
                CmdTextBox.BeginAutoUndo();
            }
            undoFlag = false;
        }

        private void CmdTextBox_TextChanged(object sender, FastColoredTextBoxNS.TextChangedEventArgs e)
        {
            if ((CmdTextBox.Text == "" || CmdTextBox.Text.EndsWith(" ") 
                || CmdTextBox.Text.EndsWith(Environment.NewLine)) && !undoFlag)
            {
                CmdTextBox.BeginAutoUndo();
              //  undoFlag = false;
            }
            undoFlag = false;


            e.ChangedRange.ClearStyle(KeyWordStyle);
            e.ChangedRange.SetStyle(KeyWordStyle,
                "select|from|where|having|by|order|group|join|left|right|union|intersect|distinct|inner|outer",
                RegexOptions.IgnoreCase);
            e.ChangedRange.ClearStyle(TextStyle);
            e.ChangedRange.SetStyle(TextStyle,
                "'(.*?)'",
                RegexOptions.IgnoreCase);
        }

        public SqlCmdControl(string queryString) : this()
        {
            CmdTextBox.AppendText(queryString);
            ExeQuery(queryString);
        }

        

        private async void ExeQuery(string cmd)
        {
            try
            {
                
                Stopwatch sw = new Stopwatch();
                DataTable dt = new DataTable();
                int i = 0;
                       
                sw.Start();
                i = 0;
                //await Task.Run(() =>
                //{
                   i= await  context.DbProvider.FillDataSetAsync(cmd, dt, cts.Token);
                 Task.Delay(5000);
                //});
                sw.Stop();
                if (dt.Rows != null && dt.Rows.Count > 0)
                    QueryData = dt;

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


        private async Task ExeQueryAsync(string cmd)
        {
            try
            {

                Stopwatch sw = new Stopwatch();
                DataTable dt = new DataTable();
                int i = 0;

                sw.Start();
                i = 0;

                i = await context.DbProvider.FillDataSetAsync(cmd, dt, cts.Token);

                sw.Stop();
                if (dt.Rows != null && dt.Rows.Count > 0)
                    QueryData = dt;

                RowCountTb.Text = $"{i} wierszy";

                ElapsedTime = sw.Elapsed;
                StatusTb.Text = "Polecenie wykonano pomyślnie";
                OkImg.Visibility = Visibility.Visible;
                WarnImg.Visibility = Visibility.Hidden;
                DbConImg.Visibility = Visibility.Hidden;


            }
            catch (Exception ex)
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



        private string GetRtbText()
        {

            return CmdTextBox.Text;
        }


        private TextRange FindWordFromPosition(TextPointer position, string word)
        {
            while (position != null)
            {
                if (position.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
                {
                    string textRun = position.GetTextInRun(LogicalDirection.Forward);

                    // Find the starting index of any substring that matches "word".
                    int indexInRun = textRun.IndexOf(word);
                    if (indexInRun >= 0)
                    {
                        TextPointer start = position.GetPositionAtOffset(indexInRun);
                        TextPointer end = start.GetPositionAtOffset(word.Length);
                        return new TextRange(start, end);
                    }
                }

                position = position.GetNextContextPosition(LogicalDirection.Forward);
            }

            // position will be null if "word" is not found.
            return null;
        }





        public bool CanUndo
        {
            get { return CmdTextBox.UndoEnabled; }
        }

        public bool CanRedo
        {
            get { return CmdTextBox.RedoEnabled; }
        }

        public bool CanCopy
        {
            get
            {
                return !string.IsNullOrEmpty(CmdTextBox.SelectedText);
            }
        }

        public bool CanPaste
        {
            get { return Clipboard.ContainsText(); }
        }
        public bool CanCut
        {
            get { return !string.IsNullOrEmpty(CmdTextBox.SelectedText); }
        }

        public bool CanDelete
        {
            get { return !string.IsNullOrEmpty(CmdTextBox.Text); }
        }

        public bool CanSelect
        {
            get { return !string.IsNullOrEmpty(GetRtbText()); }
        }

        public void Redo()
        {
            CmdTextBox.Redo();
        }

        public void Undo()
        {
            undoFlag = true;
            CmdTextBox.Undo();
        }

        public void Copy()
        {
            CmdTextBox.Copy();
        }

        public void Paste()
        {
            CmdTextBox.EndAutoUndo();
            CmdTextBox.BeginAutoUndo();
            CmdTextBox.Paste();
            CmdTextBox.EndAutoUndo();
            CmdTextBox.BeginAutoUndo();
        }

        public void Cut()
        {
            CmdTextBox.EndAutoUndo();
            CmdTextBox.BeginAutoUndo();
            CmdTextBox.Cut();
            CmdTextBox.EndAutoUndo();
            CmdTextBox.BeginAutoUndo();
        }



        public void Delete()
        {
           
            CmdTextBox.Text = "";
        }

        public void Select()
        {
            CmdTextBox.SelectAll();
        }

        public bool CanExport
        {
            get { return QueryData != null; }
        }

        public bool CanRunQuery
        {
            get
            {
                return !string.IsNullOrEmpty(GetRtbText());
            }
        }

        public bool CanSaveQuery
        {
            get
            {
                return !string.IsNullOrEmpty(GetRtbText());
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

        public async void OpenQuery()
        {
            var ofd = new System.Windows.Forms.OpenFileDialog();
            ofd.Filter = "SQL Script File|*.sql";
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                using (var stream = ofd.OpenFile())
                {
                    using (var sr = new StreamReader(stream))
                    {
                        //TextRange tr = new TextRange(CmdTextBox.Document.ContentStart,
                        //    CmdTextBox.Document.ContentEnd);

                        string txt = await sr.ReadToEndAsync();
                        CmdTextBox.Text = txt;

                    }
                }
            }
        }

        private void LoadScriptButton_Click(object sender, RoutedEventArgs e)
        {
            OpenQuery();
        }

        private void SaveScriptButton_Click(object sender, RoutedEventArgs e)
        {
            SaveQuery();
        }

        private void RunQueryButton_Click(object sender, RoutedEventArgs e)
        {
            RunQuery();
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            Export();
        }

        public async void RunQuery()
        {
            cts = new CancellationTokenSource();

            Disable();

            string cmd = GetRtbText();

            await ExeQueryAsync(cmd);
            Enable();

            if (QueryData != null && QueryData.Rows.Count > 0)
                ExportButton.IsEnabled = true;
            else
                ExportButton.IsEnabled = false;
        }

        public async void SaveQuery()
        {
            if (string.IsNullOrEmpty(fileName))
            {
                var sfd = new System.Windows.Forms.SaveFileDialog();
                sfd.Filter = "SQL Script File|*.sql";
                sfd.ShowDialog();

                if (sfd.FileName != "")
                {
                    Disable();
                    using (var stream = (FileStream)sfd.OpenFile())
                    {
                        using (var sw = new StreamWriter(stream))
                        {
                            await sw.WriteAsync(GetRtbText());
                            sw.Close();
                        }
                    }
                    Enable();
                }
                fileName = sfd.FileName;
            }
            else if (!File.Exists(fileName))
            {
                Disable();
                using (var fs = File.Create(fileName))
                {
                    using (var sw = new StreamWriter(fs))
                    {
                        sw.Write(GetRtbText());
                    }
                }
                Enable();
            }
            else
            {
                Disable();
                using (var fs = File.OpenWrite(fileName))
                {
                    using (var sw = new StreamWriter(fs))
                    {
                        await sw.WriteAsync(GetRtbText());
                    }
                }
                Enable();
            }
        }

        private void StopQueryButton_Click(object sender, RoutedEventArgs e)
        {
            cts.Cancel();
        }

        private void Enable()
        {
            RunSqlQueryButton.IsEnabled = true;
            StopSqlQueryButton.IsEnabled = false;
        }
        private void Disable()
        {
            RunSqlQueryButton.IsEnabled = false;
            StopSqlQueryButton.IsEnabled = true;
            StatusTb.Text = "Wykonywanie polecenia...";
            OkImg.Visibility = Visibility.Hidden;
            WarnImg.Visibility = Visibility.Hidden;
            DbConImg.Visibility = Visibility.Hidden;

        }

    }





}
