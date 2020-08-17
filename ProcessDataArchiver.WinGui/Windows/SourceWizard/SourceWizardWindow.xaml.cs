using ProcessDataArchiver.DataCore.Acquisition;
using ProcessDataArchiver.DataCore.Database.DbProviders;
using ProcessDataArchiver.WinGui.Windows.SourceWizard.Pages;
using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Shapes;

namespace ProcessDataArchiver.WinGui.Windows.SourceWizard
{
    /// <summary>
    /// Interaction logic for SourceWizardWindow.xaml
    /// </summary>
    public partial class SourceWizardWindow : Window
    {

        private Page currentPage;
        private Page previousPage;

        private ProjectPage projectPage = new ProjectPage();
        private DatabasePage dbPage = new DatabasePage();


        public ArchvieProjectInfo ArProjInfo { get; set; }


        public SourceWizardWindow()
        {
            InitializeComponent();

            projectPage.ValidationChanged += (s, e) =>
            { Page_ValidationChanged(s, !projectPage.HasErrors); };

            dbPage.ValidationChanged += Page_ValidationChanged;
            dbPage.ConnectionStateChanged += DbPage_ConnectionStateChanged;
            ContentFrame.NavigationService.Navigate(projectPage);
            currentPage = projectPage;

       //     if (projectPage.IsValid) NextButton.IsEnabled = true;
        }

        private void DbPage_ConnectionStateChanged(object sender, bool e)
        {
            if (e)
            {
                NextButton.IsEnabled = true;
                BackButton.IsEnabled = true;
            }
            else
            {
                NextButton.IsEnabled = false;
                BackButton.IsEnabled = false;
            }
        }

        private void Page_ValidationChanged(object sender, bool e)
      {
            if (e)
            {
                NextButton.IsEnabled = true;
            }
            else
            {
                NextButton.IsEnabled = false;
            }
        }


        private async void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage == projectPage)
            {
                if (ArchvieProjectInfo.ReadVariables(projectPage.CpProjectPath).Count() > 0)
                {
                    try
                    {
                        File.Create(projectPage.ProjectFolderPath + "\\" + projectPage.ProjectName + ".xml");
                    }
                    catch(Exception ex)
                    {
                        MessageBox.Show("Nie udało się utworzyć pliku w podanej lokalizacji",
                            "Błąd", MessageBoxButton.OKCancel, MessageBoxImage.Error);
                        return;
                    }
                    ContentFrame.NavigationService.Navigate(dbPage);
                    currentPage = dbPage;
                    previousPage = projectPage;
                    BackButton.IsEnabled = true;
                    NextButton.Content = "Zakończ";
                    (currentPage as DatabasePage).ValidatePage();
                }
                else
                {
                    MessageBox.Show("Wybrany projekt nie zawiera zmiennych globalnych",
                        "Błąd", MessageBoxButton.OKCancel, MessageBoxImage.Error);
                }

            }
            else if(currentPage == dbPage)
            {
                if (dbPage.IsValid)
                {                 
                    bool res = await dbPage.Finish();
                    if (res)
                    {
                        previousPage = dbPage;

                        ArProjInfo = new ArchvieProjectInfo(dbPage.DbProvider, projectPage.CpDataSource)
                        {
                            ArchiveProjectPath = projectPage.ProjectFolderPath+"\\" + projectPage.ProjectName + ".xml",
                            CPDevProgramPath = projectPage.CpProjectPath,
                        };
                        this.Close();
                    }
                }
            }

        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage.Equals(dbPage))
            {
                ContentFrame.NavigationService.Navigate(projectPage);
                currentPage = projectPage;
                BackButton.IsEnabled = false;
                NextButton.IsEnabled = true;
                NextButton.Content = "Dalej";
            }

        }

        
    }
}
