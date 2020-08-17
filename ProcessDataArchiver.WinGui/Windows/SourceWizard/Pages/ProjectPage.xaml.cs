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
using ProcessDataArchiver.DataCore.Database.Schema;
using System.Windows.Forms;
using System.ComponentModel;
using System.IO;
using System.Collections.ObjectModel;
using System.Collections;

namespace ProcessDataArchiver.WinGui.Windows.SourceWizard.Pages
{
    /// <summary>
    /// Interaction logic for ProjectPage.xaml
    /// </summary>
    public partial class ProjectPage : Page,INotifyPropertyChanged,INotifyDataErrorInfo
    {
        private OpenFileDialog cpProjectDialog = new OpenFileDialog();
        private FolderBrowserDialog projectFolderDialog = new FolderBrowserDialog();

        private string projectFolderPath;
        private string cpProjectPath;
        private string projectName,targetName;
        private Dictionary<string, IEnumerable<string>> errors = new Dictionary<string, IEnumerable<string>>();


        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<bool> ValidationChanged;
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        public bool IsValid {
            get
            {
                return ErrorList.Count == 0;
            }

        }


        public string CpProjectPath
        {
            get { return cpProjectPath; }
            set
            {
                cpProjectPath = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CpProjectPath)));
                Validate(nameof(CpProjectPath));
            }
        }

        public string ProjectFolderPath
        {
            get { return projectFolderPath; }
            set
            {
                projectFolderPath = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ProjectFolderPath)));
                Validate(nameof(ProjectFolderPath));
            }
        }
        public string ProjectName
        {
            get
            { return projectName; }

            set
            {
                projectName = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ProjectName)));
                Validate(nameof(ProjectName));
            }
        }

        public string TargetName
        {
            get { return targetName; }
            set
            {
                targetName = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TargetName)));
                CpDataSource = CPDev.SADlg.GlobalDataSources.Targets
                    .Where(t => t.Name.Equals(TargetName)).FirstOrDefault();
            }
        }

        public CPDev.Public.ICPSim_Target CpDataSource { get; set; }

        public ObservableCollection<string> TargetsNames { get; set; } = new ObservableCollection<string>();

        public ObservableCollection<string> ErrorList { get; set; } = new ObservableCollection<string>();

        public bool HasErrors
        {
            get
            {
                return errors.Count > 0;
            }
        }

        public void Validate(string propName)
        {

                switch (propName)
                {
                    case nameof(ProjectName):
                    if (string.IsNullOrEmpty(ProjectName))
                    {
                        if (!errors.ContainsKey(propName))
                        {
                            errors.Add(propName, new[] { "Nazwa nie może być pusta" });
                        }
                    }
                    else if (ProjectName.Contains(" "))
                    {
                        if (!errors.ContainsKey(propName))
                        {
                            errors.Add(propName,new[] { "Nazwa nie może zawierać spacji" });
                        }

                    }

                    else if(errors.ContainsKey(propName))
                    {
                        errors.Remove(propName);
                    }
                    break;

                    case nameof(CpProjectPath):
                    if (String.IsNullOrEmpty(CpProjectPath))
                    {
                        if (!errors.ContainsKey(propName))
                            errors.Add(propName, new[] { "Nie wybrano projektu" });
                    }
                    else if (!File.Exists(CpProjectPath))
                    {
                        if (!errors.ContainsKey(propName))
                            errors.Add(propName, new[] { "Plik o podanej nazwie nie istnieje" });
                    }
                    else if (!File.Exists(System.IO.Path.ChangeExtension(CpProjectPath, ".xcp")))
                    {
                        if (!errors.ContainsKey(propName))
                            errors.Add(propName, new[] { "W wybranym folderze nie istnieje plik .xcp" });
                    }
                    else if(errors.ContainsKey(propName))
                    {
                        errors.Remove(propName);
                    }
                    break;

                    case nameof(ProjectFolderPath):
                    if (String.IsNullOrEmpty(ProjectFolderPath))
                    {
                        if (!errors.ContainsKey(propName))
                        {
                            errors.Add(propName, new[] { "Nie wybrano folderu" });
                            ErrorList.Add("Nie Wybrano folderu");
                        }

                    }
                    else if (errors.ContainsKey(propName))
                    {
                        errors.Remove(propName);
                    }
                    break;
                }
            OnErrorsChanged(propName);
            
        }

        public ProjectPage()
        {
            InitializeComponent();

            this.DataContext = this;
            this.ProjectName = "Projekt1";
            this.CpProjectPath = "";
            this.ProjectFolderPath = @"C:\";
            cpProjectDialog.Filter = "CPDev project(*.xml, *.dcp)|*.xml;*.dcp|All files (*.*)|*.*";

            CPDev.SADlg.GlobalDataSources.UpdateTargetList();
            var targets = CPDev.SADlg.GlobalDataSources.Targets;
            foreach (var tar in targets)
            {
                TargetsNames.Add(tar.Name);
            }
            if (TargetsNames.Count > 0)
                TargetName = TargetsNames[0];

            errors.Remove(nameof(CpProjectPath));
            OnErrorsChanged(nameof(CpProjectPath));

        }

        private void PathButton_Click(object sender, RoutedEventArgs e)
        {
            if(projectFolderDialog.ShowDialog() == DialogResult.OK)
            {
                ProjectFolderPath = projectFolderDialog.SelectedPath;
                PathTextBox.Text = ProjectFolderPath;
            }
        }

        private void ProjectPathButton_Click(object sender, RoutedEventArgs e)
        {
            if(cpProjectDialog.ShowDialog() == DialogResult.OK)
            {
                CpProjectPath = cpProjectDialog.FileName;
                
            }
        }




        private void DataSourcePathButton_Click(object sender, RoutedEventArgs e)
        {
            CPDev.SADlg.GlobalDataSources.UpdateTargetList();
            CPDev.SADlg.GlobalDataSources okno = new CPDev.SADlg.GlobalDataSources();

            if (okno.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                CPDev.Public.ICPSim_Target target = CPDev.SADlg.GlobalDataSources.CurrentTarget;
                
                if (target != null)
                {
                    TargetsNames.Clear();
                    var targets = CPDev.SADlg.GlobalDataSources.Targets;
                    foreach (var tar in targets)
                    {
                        TargetsNames.Add(tar.Name);
                    }
                    TargetName = target.Name;
                }

            }
           
        }

        private void OnErrorsChanged(string pName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(pName));

            ValidationChanged?.Invoke(this, !this.HasErrors);

        }

        public IEnumerable GetErrors(string propertyName)
        {
            if (errors.ContainsKey(propertyName))
            {
                return errors[propertyName];
            }
            return null;
        }
    }
}
