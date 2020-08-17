using ProcessDataArchiver.DataCore.DbEntities;
using ProcessDataArchiver.DataCore.DbEntities.Tags;
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
    /// Interaction logic for CreateArchiveWindow.xaml
    /// </summary>
    public partial class ArchiveDialog : Window
    {
        public ArchiveDialog()
        {
            InitializeComponent();

            int index = 1;
            string archname = "Archiwum"+index;

            while (EntityContext.GetContext().TagArchives.Select(t => t.Name).Contains(archname))
            {
                archname = archname.Replace(index.ToString(), (++index).ToString());
            }
            NameTextBox.Text = archname;
        }


        public TagArchive Archive { get; set; }
        public bool Applied { get;private set; }


        private void CreateArButton_Click(object sender, RoutedEventArgs e)
        {
            Applied = true;

            var type = (TypeComboBox.SelectedItem as ComboBoxItem)
                .Content.ToString().Equals("Znaczniki w osobnych kolumnach") ? ArchiveType.Wide : ArchiveType.Normal;
            Archive = new TagArchive { Name = NameTextBox.Text, ArchiveType = type };

            this.Close();
        }


        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
