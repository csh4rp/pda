using ProcessDataArchiver.DataCore.Acquisition;
using ProcessDataArchiver.DataCore.DbEntities;
using ProcessDataArchiver.DataCore.DbEntities.Tags;
using ProcessDataArchiver.WinGui.Windows.Commands;
using ProcessDataArchiver.WinGui.Windows.Dialogs;
using ProcessDataArchiver.WinGui.Windows.DockingControls;
using ProcessDataArchiver.WinGui.Windows.SourceWizard;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
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
using Xceed.Wpf.AvalonDock.Layout;


namespace ProcessDataArchiver.WinGui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ArchvieProjectInfo project;
        private EntityContext context;
        private IoServer server;

        private bool hasArchives, hasProject;
        public static double TopPos { get; private set; }
        public  static double LeftPos { get; private set; }
        public static double WinHeight { get; private set; }
        public static double WinWidth{ get; private set; }
        public ObservableCollection<string> TableNames { get; set; } = new
            ObservableCollection<string>();

        private ISqlQueryCommands currentSqlQuery;
        private LayoutDocumentPane _pane;
        private LayoutDocumentPaneGroup Group;

        public static string ProjectName { get; set; }
        public List<LayoutDocument> Documents { get; set; } = new List<LayoutDocument>();

        public static RoutedCommand SqlQueryCmd { get; set; } = new RoutedCommand();
        public static RoutedCommand QueryCmd { get; set; } = new RoutedCommand();
        public static RoutedCommand TrendCmd { get; set; } = new RoutedCommand();
        public static RoutedCommand DataSourceCmd { get; set; } = new RoutedCommand();
        public static RoutedCommand DatabaseCmd { get; set; } = new RoutedCommand();
        public static RoutedCommand ProgramCmd { get; set; } = new RoutedCommand();
        public static RoutedCommand StartCmd { get; set; } = new RoutedCommand();
        public static RoutedCommand StopCmd { get; set; } = new RoutedCommand();
        public static RoutedCommand AddArchiveCmd { get; set; } = new RoutedCommand();
        public static RoutedCommand EventsCmd { get; set; } = new RoutedCommand();
        public static RoutedCommand GlobalVariablesCmd { get; set; } = new RoutedCommand();
        public static RoutedCommand LookupTableCmd { get; set; } = new RoutedCommand();
        public static RoutedCommand LoadScriptCmd { get; set; } = new RoutedCommand();
        public static RoutedCommand CloseAllWindows { get; set; } = new RoutedCommand();
        public static RoutedCommand FloatWindow { get; set; } = new RoutedCommand();
        public static RoutedCommand FloatAllWindows { get; set; } = new RoutedCommand();
        public static RoutedCommand DockWindow { get; set; } = new RoutedCommand();

        private List<UserControl> commandCotrols = new List<UserControl>();

        public MainWindow()
        {
            CPDev.SADlg.ConfigManager.DefaultUsr.InitializeConfigManager();
            InitializeComponent();
            if(Group == null)
            {
                Group = new LayoutDocumentPaneGroup();
                Panel.Children.Add(Group);
            }
            
            this.Closing += MainWindow_Closing;
            this.LocationChanged += (s, e) => {
                TopPos = this.Top;
                LeftPos = this.Left;
                WinWidth = this.ActualWidth;
                WinHeight = this.ActualHeight;
            };
            this.SizeChanged += (s, e) =>
            {
                TopPos = this.Top;
                LeftPos = this.Left;
                WinWidth = this.ActualWidth;
                WinHeight = this.ActualHeight;
            };
            TopPos = this.Top;
            LeftPos = this.Left;
            WinWidth = this.ActualWidth;
            WinHeight = this.ActualHeight;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (project != null)
            {

                var res = System.Windows.MessageBox.Show("Czy na pewno chcesz wyjść z programu?", "Zamykanie",
                MessageBoxButton.YesNo, MessageBoxImage.Asterisk);

                if (res == MessageBoxResult.No)
                {
                    e.Cancel = true;
                }
                else
                {
                    CPDev.SADlg.ConfigManager.DefaultUsr.FinishConfigManager();
                }
            }
        }
        





        private void NewArchive_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            AddArchive();
        }



        private void AddArchive()
        {
            var newArchvieDialog = new ArchiveDialog();
            newArchvieDialog.ShowDialog();
            if (newArchvieDialog.Applied)
            {
                var cnx = EntityContext.GetContext();
                var archive = newArchvieDialog.Archive;
                cnx.TagArchives.Add(archive);
                Archives.Items.Add(archive);

                hasArchives = true;
                SetEnabled();
            }
        }


        private void ArchiveTextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed && e.ClickCount == 2)
            {
                
                var tb = (sender as TextBlock);
                if (tb != null)
                {
                    string arName = tb.Text;
                    var archive = EntityContext.GetContext().TagArchives
                        .Where(t => t.Name.Equals(arName)).FirstOrDefault();

                    EditArchive(archive);
                    
                }
            }
        }

        private void EditArchive(TagArchive archive)
        {
            if (!Panel.Children.Contains(Group))
            {
                Group = new LayoutDocumentPaneGroup();
                Panel.Children.Add(Group);
            }
            if (_pane==null || !Group.Children.Contains(_pane))
            {
                _pane = new LayoutDocumentPane();
                Group.Children.Add(_pane);
            }
            var document = new LayoutDocument();
            var control = new TagsControl(archive);

            control.GotFocus += Control_GotFocus;
            control.LostFocus += Control_LostFocus;
            document.Closing += Document_Closing;


            document.Content = control;
            document.Title = archive.Name;
            _pane.Children.Add(document);

            Documents.Add(document);
            _pane.SelectedContentIndex = _pane.Children.Count - 1;
        }


        private void Events_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed && e.ClickCount == 2)
            {
                ShowEvents();
            }
        }

        private void ShowEvents()
        {
            if (!Panel.Children.Contains(Group))
            {
                Group = new LayoutDocumentPaneGroup();
                Panel.Children.Add(Group);
            }
            if (_pane == null || !Group.Children.Contains(_pane))
            {
                _pane = new LayoutDocumentPane();
                Group.Children.Add(_pane);
            }
            var document = new LayoutDocument();
            var control = new EventsControl();

            control.GotFocus += Control_GotFocus;
            control.LostFocus += Control_LostFocus;
            document.Closing += Document_Closing;

            document.Content = control;
            document.Title = "Zdarzenia";
            _pane.Children.Add(document);
            Documents.Add(document);
            _pane.SelectedContentIndex = _pane.Children.Count - 1;
        }


        private void LoadArchvies()
        {
            var archives = context.TagArchives;

            foreach (var arch in archives)
            {
                Archives.Items.Add(arch);
            }
        }

        private void GvItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ShowGvs();
        }

        private void ShowGvs()
        {
            if (!Panel.Children.Contains(Group))
            {
                Group = new LayoutDocumentPaneGroup();
                Panel.Children.Add(Group);
            }
            if (_pane == null || !Group.Children.Contains(_pane))
            {
                _pane = new LayoutDocumentPane();
                Group.Children.Add(_pane);
            }
            var document = new LayoutDocument();
            var control = new GlobalVariablesControl();

            document.Content = control;
            document.Title = "Zmienne globalne";
            _pane.Children.Add(document);
            _pane.SelectedContentIndex = _pane.Children.Count - 1;
            Documents.Add(document);
        }

        private void LiveTable_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {

        }





        private void QueryCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (context != null)
                e.CanExecute = true;
        }

        private void ShowSqlQuery()
        {
            if (!Panel.Children.Contains(Group))
            {
                Group = new LayoutDocumentPaneGroup();
                Panel.Children.Add(Group);
            }
            if (_pane == null || !Group.Children.Contains(_pane))
            {
                _pane = new LayoutDocumentPane();
                Group.Children.Add(_pane);
            }

            var document = new LayoutDocument();
           
            var control = new SqlCmdControl();
            control.GotFocus += Control_GotFocus;
            control.LostFocus += Control_LostFocus;
            currentSqlQuery = control;

            document.Content = control;
            document.Title = "Polecenie SQL";
            _pane.Children.Add(document);
            _pane.SelectedContentIndex = _pane.Children.Count - 1;
            Documents.Add(document);
        }

        private void ShowQuery()
        {
            if (!Panel.Children.Contains(Group))
            {
                Group = new LayoutDocumentPaneGroup();
                Panel.Children.Add(Group);
            }
            if (_pane == null || !Group.Children.Contains(_pane))
            {
                _pane = new LayoutDocumentPane();
                Group.Children.Add(_pane);
            }
            var document = new LayoutDocument();

            var control = new QueryControl();
            document.Content = control;
            document.Title = "Kreator Kwerend";
            _pane.Children.Add(document);
            _pane.SelectedContentIndex = _pane.Children.Count - 1;
            Documents.Add(document);
        }

        private void CmdControl_LostFocus(object sender, RoutedEventArgs e)
        {

        }

        private void CmdControl_GotFocus(object sender, RoutedEventArgs e)
        {

        }



        private void QueryCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ShowQuery();

        }

        private void TrendCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ShowTrend();
        }

        private void ShowTrend()
        {
            if (!Panel.Children.Contains(Group))
            {
                Group = new LayoutDocumentPaneGroup();
                Panel.Children.Add(Group);
            }
            if (_pane == null || !Group.Children.Contains(_pane))
            {
                _pane = new LayoutDocumentPane();
                Group.Children.Add(_pane);
            }
            var document = new LayoutDocument();
            var control = new TrendControl();

            control.GotFocus += Control_GotFocus;
            control.LostFocus += Control_LostFocus;
            document.Closing += Document_Closing;

            document.Content = control;
            document.Title = "Wykres";
            _pane.Children.Add(document);
            _pane.SelectedContentIndex = _pane.Children.Count - 1;
            Documents.Add(document);
        }


        private void TrendQueryCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            var cnx = EntityContext.GetContext();

            if (cnx != null && cnx.Tags != null && cnx.Tags.Count > 0)
                e.CanExecute = true;
        }

        private void StartCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = server != null && !server.IsStarted && context != null && context.Tags.Count > 0;
        }

        private void StartCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Start();
        }

        private void StopCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = server != null && server.IsStarted;
        }

        private void StopCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Stop();
        }

        private async void OpenCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (project != null)
            {
                foreach (var arch in context.TagArchives)
                {
                    Archives.Items.Remove(arch);
                }
                project = null;
                context = null;
            }


            var open = new System.Windows.Forms.OpenFileDialog();
            var res = open.ShowDialog();

            if (res == System.Windows.Forms.DialogResult.OK)
            {
                busy.IsBusy = true;
                string file = open.FileName;
                try
                {
                    project = ArchvieProjectInfo.Load(file);
                }
                catch(Exception ex)
                {
                    MessageBox.Show("Wystąpił błąd podczas odczytywania pliku", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    busy.IsBusy =false;
                    return;
                }
                context = await EntityContext.GetContextAsync(project.DbProvider);
                context.CollectionsChanged += Context_CollectionsChanged;
                
                LoadArchvies();

                if (server == null)
                {
                    server = IoServer.GetIoServer(project);
                    server.ErrorOccured += Server_ErrorOccured;
                    
                }
                ProjectName = System.IO.Path.GetFileNameWithoutExtension(file);
                ProjectItem.DataContext = ProjectName;

                ProjectItem.Visibility = Visibility.Visible;
                busy.IsBusy = false;
                SetEnabled();

              ////  var vr = new VMRuntime.VMTaskClass16PlatformWrapper();
              //  project.DataSource =vr;
              //  server.Target = vr;
            }
        }



        private async void NewCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SourceWizardWindow wizard = new SourceWizardWindow();
            wizard.ShowDialog();
            project = wizard.ArProjInfo;

            if (wizard.ArProjInfo != null)
            {
                context = await EntityContext.GetContextAsync(project.DbProvider);
                context.CollectionsChanged += Context_CollectionsChanged;

                foreach (var item in project.ReadVariables())
                {
                    context.GlobalVariables.Add(item);
                }
                await context.SaveChangesAsync();

                if (server == null)
                {
                    server = IoServer.GetIoServer(project);
                    server.ErrorOccured += Server_ErrorOccured;
                }
                ProjectName = System.IO.Path.GetFileNameWithoutExtension(project.ArchiveProjectPath);
                ProjectItem.DataContext = ProjectName;

                ProjectItem.Visibility = Visibility.Visible;
                //var vr = new VMRuntime.VMTaskClass16PlatformWrapper();
                //project.DataSource = vr;
                //server.Target = vr;
            }
        }

        private void Server_ErrorOccured(object sender, string e)
        {
            Stop();
            MessageBox.Show(e, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void Context_CollectionsChanged(object sender, EventArgs e)
        {
            SetEnabled();
        }


        private void SetEnabled()
        {
            if (project != null)
            {

                if (context.Tags.Count() > 0 )
                {
                    TrendButton.IsEnabled = true;
                    ViewTrendMenuItem.IsEnabled = true;

                    if (server != null && server.IsStarted)
                    {
                        StartButton.IsEnabled = false;
                        PauseButton.IsEnabled = true;
                        //ProjectStartMenuItem.IsEnabled = false;
                        //ProjectStopMenuItem.IsEnabled = true;
                    }
                    else if (server != null && !server.IsStarted)
                    {
                        StartButton.IsEnabled = true;
                        PauseButton.IsEnabled = false;
                        //ProjectStartMenuItem.IsEnabled = true;
                        //ProjectStopMenuItem.IsEnabled = false;
                    }
                }
                else if(context.Tags.Count == 0)
                {
                    StartButton.IsEnabled = false;
                    PauseButton.IsEnabled = false;
                }


                SqlCmdButton.IsEnabled = true;
                QueryButton.IsEnabled = true;

                FileCloseMenuItem.IsEnabled = true;

                ViewSqlQueryMenuItem.IsEnabled = true;
                ViewQueryMenuItem.IsEnabled = true;

                ProjectDsMenuItem.IsEnabled = true;
                ProjectDbMenuItem.IsEnabled = true;
                ProjectProgramMenuItem.IsEnabled = true;


            }
            else
            {
                SqlCmdButton.IsEnabled = false;
                QueryButton.IsEnabled = false;
                TrendButton.IsEnabled = false;
                StartButton.IsEnabled = false;
                PauseButton.IsEnabled = false;



                FileCloseMenuItem.IsEnabled = false;

                ViewSqlQueryMenuItem.IsEnabled = false;
                ViewQueryMenuItem.IsEnabled = false;
                ViewTrendMenuItem.IsEnabled = false;

                ProjectDsMenuItem.IsEnabled = false;
                ProjectDbMenuItem.IsEnabled = false;
                ProjectProgramMenuItem.IsEnabled = false;
                ProjectStartMenuItem.IsEnabled = false;
                ProjectStopMenuItem.IsEnabled = false;
            }
        }

        private async void FileCloseMenuItem_Click(object sender, RoutedEventArgs e)
        {
          var res = System.Windows.MessageBox.Show("Czy na pewno chcesz zamknąć projekt?", "Zamykanie",
                MessageBoxButton.YesNo, MessageBoxImage.Asterisk);

            if(res == MessageBoxResult.Yes)
            {
                server.Stop();
                server.ErrorOccured -= Server_ErrorOccured;
                project.Save();
                var cnx = EntityContext.GetContext();
                await cnx.SaveChangesAsync();
            }
            ProjectItem.Visibility = Visibility.Hidden;
            

            foreach (var arch in context.TagArchives)
            {
                Archives.Items.Remove(arch);
            }
            project = null;
            context = null;


            SetEnabled();
        }



        private async void SaveCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            project.Save();
            var cnx = EntityContext.GetContext();
            await cnx.SaveChangesAsync();
        }

        private void FileSaveAsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var save = new System.Windows.Forms.SaveFileDialog();
            save.DefaultExt = ".xml";
           

            if(save.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                project.SaveAs(save.FileName);
            }
        }


        private void SaveCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = project != null && project.DbProvider != null;
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }



        private void UndoCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = currentSqlQuery != null && currentSqlQuery.CanUndo;
        }

        private void RedoCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = currentSqlQuery != null && currentSqlQuery.CanRedo;
        }

        private void RedoCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            currentSqlQuery.Redo();
        }

        private void UndoCommand_Execute(object sender, ExecutedRoutedEventArgs e)
        {
            currentSqlQuery.Undo();
        }

        private void CopyCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = currentSqlQuery != null && currentSqlQuery.CanCopy;
        }

        private void CopyCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            currentSqlQuery.Copy();
        }

        private void CutCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = currentSqlQuery != null && currentSqlQuery.CanCopy;
        }

        private void CutCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            currentSqlQuery.Cut();
        }

        private void PasteCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = currentSqlQuery != null && currentSqlQuery.CanPaste;
        }

        private void PasteCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            currentSqlQuery.Paste();
        }

        private void DeleteCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = currentSqlQuery != null && currentSqlQuery.CanDelete;
        }

        private void DeleteCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            currentSqlQuery.Delete();
        }

        private void SelectAllCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = currentSqlQuery != null && currentSqlQuery.CanSelect;
        }

        private void SelectAllCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            currentSqlQuery.Select();
        }



        private void DsCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = project != null && server!=null && !server.IsStarted; 
        }

        private void DsCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            CPDev.SADlg.GlobalDataSources.UpdateTargetList();
            CPDev.SADlg.GlobalDataSources okno = new CPDev.SADlg.GlobalDataSources();

            if (okno.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                CPDev.Public.ICPSim_Target target = CPDev.SADlg.GlobalDataSources.CurrentTarget;

                if (target != null)
                {
                    project.DataSource = target;
                    server.Target = target;
                }

            }
        }

        private void DbCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {            
            var edb = new EditDbDialog(project.DbType,project.ConnectionString);
            edb.ShowDialog();

            if(edb.DbProvider!=null)
                project.DbProvider = edb.DbProvider;
        }

        private void ProgramCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.OpenFileDialog();
            dialog.Filter = "XML files (*.xml)|*.xml |DCP files (*.dcp)|*dcp";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var change = MessageBox.Show("Zmiana pliku projektu może spowodować nieprawidłowe odczytywanie " +
                      "wartości zmiennych w przypadku rozbieżności w ich deklaracji " +
                      "między obecnym a wybranym projektem, Kontynuować?", "Projekt",
                      MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (change == MessageBoxResult.Yes)
                {
                    project.CPDevProgramPath = dialog.FileName;
                    server.LoadProgram(dialog.FileName);
                }
            }
        }

        private void AddArchiveCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            AddArchive();
        }



        private void GenRepCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {

        }

        private void ExportCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            //e.CanExecute = _project != null && ((currentQuery != null && currentQuery.CanExport)
            //    || (currentSqlQuery != null && currentSqlQuery.CanExport));
                
        }

        private void ExportCommand_Execute(object sender, ExecutedRoutedEventArgs e)
        {
            //currentQuery?.Export();
            //currentSqlQuery?.Export();
        }


        private async void Document_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var doc = sender as LayoutDocument;
            if (doc != null)
            {
                Documents.Remove(doc);
                var control = doc.Content as UserControl;

                if (control != null)
                {
                    control.GotFocus -= Control_GotFocus;
                    control.LostFocus -= Control_LostFocus;


                    if(control is TagsControl)
                    {
                      var res = MessageBox.Show("Czy chcesz zapisać wprowadzone zmiany?", "Zapisz",
                            MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                        if (res == MessageBoxResult.Yes)
                            await context.SaveChangesAsync();
                        else if (res == MessageBoxResult.No)
                            context.Tags.RestoreChanges();
                        else
                            e.Cancel = true;

                    }
                    else if(control is EventsControl)
                    {
                        var res = MessageBox.Show("Czy chcesz zapisać wprowadzone zmiany?", "Zapisz",
                            MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                        if (res == MessageBoxResult.Yes)
                            await context.SaveChangesAsync();
                        else if (res == MessageBoxResult.No)
                            context.Events.RestoreChanges();
                        else
                            e.Cancel = true;
                    }

                }
                doc.Closing -= Document_Closing;
                doc.IsSelectedChanged -= Document_IsSelectedChanged;
            }
        }

        private void Document_IsSelectedChanged(object sender, EventArgs e)
        {
            //var doc = sender as LayoutDocument;

        }

        private void Control_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is ISqlQueryCommands)
            {
                currentSqlQuery = null;
            }
        }




        private void Control_GotFocus(object sender, RoutedEventArgs e)
        {
             if(sender is ISqlQueryCommands)
             {
                currentSqlQuery = sender as ISqlQueryCommands;
             }   
        }



        private void LookupTables_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed && e.ClickCount == 2)
            {
                ShowLookup();
            }
        }

        private void ShowLookup()
        {
            if (!Panel.Children.Contains(Group))
            {
                Group = new LayoutDocumentPaneGroup();
                Panel.Children.Add(Group);
            }
            if (_pane == null || !Group.Children.Contains(_pane))
            {
                _pane = new LayoutDocumentPane();
                Group.Children.Add(_pane);
            }

            var document = new LayoutDocument();
            var control = new LookupTableControl();

            document.Content = control;
            document.Title = "Tabela podglądowa";
            _pane.Children.Add(document);
            _pane.SelectedContentIndex = _pane.Children.Count - 1;
        }

        private void SqlCmdButton_Click(object sender, RoutedEventArgs e)
        {
            ShowSqlQuery();
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            Start();
            
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            Stop();
        }

        private async void RemoveArchiveMenuItem_Click(object sender, RoutedEventArgs e)
        {

            

            var item = (sender as MenuItem).Header as TextBlock;
            string txt = item.Text;
            string archiveName = txt.Substring(5);

            var res =  MessageBox.Show("Czy na pewno chcesz usunąć archiwum '"+archiveName+"'?", "Usuń archiwum",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (res == MessageBoxResult.Yes)
            {
                var archive = context.TagArchives.Where(t => t.Name.Equals(archiveName)).FirstOrDefault();
                if (archive != null)
                {
                    context.TagArchives.Remove(archive);
                    Archives.Items.Remove(archive);
                }
                await context.SaveChangesAsync();
            }

        }

        private void EditArchiveMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as MenuItem).Header as TextBlock;
            string txt = item.Text;
            string archiveName = txt.Substring(7);
            var archive = context.TagArchives.Where(t => t.Name.Equals(archiveName)).FirstOrDefault();
            if (archive != null)
            {
                EditArchive(archive);
            }
        }

        private async void Grid_KeyDown(object sender, KeyEventArgs e)
        {
            

            if (e.Key == Key.Delete)
            {
                var tvi = sender as TreeViewItem;
                var ta = tvi.Header as TagArchive;
                if (ta != null)
                {
                    var res = MessageBox.Show("Czy na pewno chcesz usunąć archiwum '" + ta.Name + "'?",
                    "Usuń archiwum", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (res == MessageBoxResult.Yes)
                    {
                        tvi.KeyDown -= Grid_KeyDown;
                        Archives.Items.Remove(ta);
                        context.TagArchives.Remove(ta);
                        await context.SaveChangesAsync();
                    }
                }
            }
            


        }

        private void Archives_Selected(object sender, RoutedEventArgs e)
        {
            var tvi = e.OriginalSource as TreeViewItem;
            tvi.KeyDown += Grid_KeyDown;

        }

        private void Archives_Unselected(object sender, RoutedEventArgs e)
        {
            var tvi = e.OriginalSource as TreeViewItem;
            tvi.KeyDown -= Grid_KeyDown;

        }

        private void QueryButton_Click(object sender, RoutedEventArgs e)
        {
            ShowQuery();
        }

        private void SqlQueryCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (context != null)
                e.CanExecute = true;
        }

        private void SqlQueryCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ShowSqlQuery();
        }

        private void TrendCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (context != null && context.TagArchives.Count>0 && context.Tags.Count>0)
                e.CanExecute = true;
        }

        private void DbCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = project != null && server!=null && !server.IsStarted;
        }

        private void ProgramCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = project != null && server != null && !server.IsStarted;
        }

        private void AddArchiveCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = project != null && server!=null && !server.IsStarted;
        }

        private void LoadScriptCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = project != null;
        }

        private async void LoadScriptCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.OpenFileDialog();
            dialog.Filter = "SQL files (*.sql)|*.sql |All files (*.*)|*.*";

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string txt;

                using (var stream = dialog.OpenFile())
                {
                    using (var sr = new StreamReader(stream))
                    {
                        txt = await sr.ReadToEndAsync();
                    }
                }

                if (!Panel.Children.Contains(Group))
                {
                    Group = new LayoutDocumentPaneGroup();
                    Panel.Children.Add(Group);
                }
                if (_pane == null || !Group.Children.Contains(_pane))
                {
                    _pane = new LayoutDocumentPane();
                    Group.Children.Add(_pane);
                }
                var document = new LayoutDocument();
                var cmdControl = new SqlCmdControl(txt);

                document.Content = cmdControl;
                document.Title = "Polecenie SQL";
                _pane.Children.Add(document);
                _pane.SelectedContentIndex = _pane.Children.Count - 1;
                Documents.Add(document);
            }
        }

        private void CloseAllCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Group != null && _pane != null && Group.Children.Contains(_pane) && _pane.Children.Count > 0;
        }

        private void CloseAlltCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var list = _pane.Children.ToList();
            foreach (var item in list)
            {
                item.Close();
            }
        }

        private void FloatCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Group != null && _pane != null && Group.Children.Contains(_pane)
                && _pane.Children.Count > 0 && _pane.SelectedContent != null;
        }

        private void FloatCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            _pane.SelectedContent.Float();
        }

        private void FloatAllCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Group != null && _pane != null && Group.Children.Contains(_pane)
                && _pane.Children.Count > 0 && _pane.SelectedContent != null;
        }

        private void FloatAllCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var list = _pane.Children.ToList();
            foreach (var item in list)
            {
                item.Float();
            }
        }

        private void DockCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {

            e.CanExecute = Group != null && _pane != null
                 && Documents.Where(d=>d.IsFloating).Count()>0;
        }

        private void DockCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            foreach (var item in Documents)
            {
                item.DockAsDocument();
            }
        }

        private void TrendButton_Click(object sender, RoutedEventArgs e)
        {
            ShowTrend();
        }

        private void ProjectAddArchiveMenuItem_Click(object sender, RoutedEventArgs e)
        {
            AddArchive();
        }

        private async void Start()
        {
            if(!server.ProgramLoaded)
            {
              //var res=  MessageBox.Show("Wczytać nowy program?", "Program",
              //    MessageBoxButton.YesNo, MessageBoxImage.Question);

              //  if(res == MessageBoxResult.Yes)
              //  {
                    server.LoadProgram();
                //}
                //else
                //{
                //    return;
                //}
            }
            await context.SaveChangesAsync();
            server.Start();
    //        ProgresBar.IsIndeterminate = true;
            StatusText.Text = "Praca...";
            StartButton.IsEnabled = false;
            PauseButton.IsEnabled = true;
            NewArchive.IsEnabled = false;
        }

        private void EventsCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = project != null && server != null;
        }

        private void EventsCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ShowEvents();
        }

        private void GvsCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = project != null && server != null;
        }

        private void GvsCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ShowGvs();
        }

        private void LookupCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = project != null && server != null;
        }

        private void LookupCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ShowLookup();
        }

        private void Stop()
        {
            server.Stop();
            StartButton.IsEnabled = true;
            PauseButton.IsEnabled = false;
            NewArchive.IsEnabled = true;
    //        ProgresBar.IsIndeterminate = false;
            StatusText.Text = "Zatrzymano";
        }
    }
}
