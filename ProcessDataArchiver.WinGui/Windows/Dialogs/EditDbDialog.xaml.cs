using ProcessDataArchiver.DataCore.Database.DbProviders;
using ProcessDataArchiver.DataCore.Database.Schema;
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
using System.Windows.Shapes;

namespace ProcessDataArchiver.WinGui.Windows.Dialogs
{
    /// <summary>
    /// Interaction logic for EditDbDialog.xaml
    /// </summary>
    public partial class EditDbDialog : Window
    {
        private SourceWizard.Pages.DatabasePage page;

        public IDatabaseProvider DbProvider { get; private set; }


        public EditDbDialog(DatabaseType dbType, string cs)
        {
            InitializeComponent();

            page = new SourceWizard.Pages.DatabasePage(dbType, cs);
            ContentFrame.NavigationService.Navigate(page);

            page.ValidationChanged += Page_ValidationChanged;        

        }

        private void Page_ValidationChanged(object sender, bool e)
        {
            if (e)
            {
                this.ApplyButton.IsEnabled = true;
            }
            else
            {
                this.ApplyButton.IsEnabled = false;
            }
        }

        private async void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            if (page.IsValid)
            {
              var res =  MessageBox.Show("Czy na pewno chcesz zmienić ustawienia bazy danych?", "Baza danych",
                    MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                if(res == MessageBoxResult.Yes)
                {
                   bool finished = await page.Finish();
                   DbProvider = page.DbProvider;
                    this.Close();
                }
                else if(res == MessageBoxResult.No)
                {
                    this.Close();
                }
                
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
