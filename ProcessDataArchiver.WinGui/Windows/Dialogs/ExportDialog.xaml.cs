using ProcessDataArchiver.DataCore.Infrastructure;
using System;
using System.Collections.Generic;
using System.Data;
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
    /// Interaction logic for ExportDialog.xaml
    /// </summary>
    public partial class ExportDialog : Window
    {
        private DataTable queryData;
        private string path;

        public ExportDialog(DataTable data)
        {
            InitializeComponent();

            this.queryData = data;
        }



        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = (ExtComboBox.SelectedItem as ComboBoxItem).Content.ToString();

            if (selected.Equals("XML"))
            {
                path = PathTextBox.Text +  NameTextBox.Text + ".xml";
            }
            else if (selected.Equals("CSV"))
            {
                path = PathTextBox.Text +  NameTextBox.Text + ".csv";
            }
            if (selected.Equals("XLS"))
            {
                path = PathTextBox.Text +  NameTextBox.Text + ".xls";
            }
            
           await DataExportProvider.ExportDataAsync(queryData, path);

            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void SelectFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var selectFolder = new System.Windows.Forms.FolderBrowserDialog();
            selectFolder.ShowDialog();
            PathTextBox.Text = selectFolder.SelectedPath;
        }
    }
}
