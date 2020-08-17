using ProcessDataArchiver.DataCore.DbEntities;
using ProcessDataArchiver.WinGui.Windows.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

namespace ProcessDataArchiver.WinGui.Windows.DockingControls
{
    /// <summary>
    /// Interaction logic for GlobalVariablesControl.xaml
    /// </summary>
    public partial class GlobalVariablesControl : UserControl,IFilterCommands
    {
        private bool loaded;
        public ICollectionView FilteredVariables { get; set; }
        public ObservableCollection<GlobalVariable> GlobalVariables { get; }
        public GlobalVariable Selected { get; set; }

        public GlobalVariablesControl()
        {
            InitializeComponent();

            loaded = true;

            GlobalVariables = new ObservableCollection<GlobalVariable>();
            foreach (var gv in EntityContext.GetContext().GlobalVariables)
            {
                GlobalVariables.Add(gv);
            }
            FilteredVariables = CollectionViewSource.GetDefaultView(GlobalVariables);
            this.DataContext = this;
        }






        public void Filter(string name, string type)
        {
            if (!string.IsNullOrEmpty(name) && type.Equals("All"))
            {
                FilteredVariables.Filter = (g) =>
                {
                    var gv = g as GlobalVariable;
                    if (gv.Name.StartsWith(name))
                        return true;
                    return false;
                };
            }
            else if (!string.IsNullOrEmpty(name) && type.Equals("Discrete"))
            {
                FilteredVariables.Filter = (g) =>
                {
                    var gv = g as GlobalVariable;
                    if (gv.Name.StartsWith(name) && gv.NetType == typeof(bool))
                        return true;
                    return false;
                };
            }
            else if (!string.IsNullOrEmpty(name) && type.Equals("Analog"))
            {
                FilteredVariables.Filter = (g) =>
                {
                    var gv = g as GlobalVariable;
                    if (gv.Name.StartsWith(name) && gv.NetType != typeof(bool))
                        return true;
                    return false;
                };
            }
            else if (string.IsNullOrEmpty(name) && type.Equals("All"))
            {
                FilteredVariables.Filter = null;
            }
            else if (string.IsNullOrEmpty(name) && type.Equals("Discrete"))
            {
                FilteredVariables.Filter = (g) =>
                {
                    var gv = g as GlobalVariable;
                    if (gv.NetType == typeof(bool))
                        return true;
                    return false;
                };
            }
            else if (string.IsNullOrEmpty(name) && type.Equals("Analog"))
            {
                FilteredVariables.Filter = (g) =>
                {
                    var gv = g as GlobalVariable;
                    if (gv.NetType != typeof(bool))
                        return true;
                    return false;
                };
            }

        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (loaded)
            {
                string type = (SelectGvCb.SelectedItem as ComboBoxItem).Name;
                string name = SearchGvTextBox.Text;

                Filter(name, type);
            }
        }

        private void Cb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (loaded)
            {
                string type = (SelectGvCb.SelectedItem as ComboBoxItem).Name;
              //  if (!gotFocus)
              //      Filter("", type);
              //  else
              //  {
                    string name = SearchGvTextBox.Text;
                    Filter(name, type);
               // }
                

            }
        }

        //private void SearchGvTextBox_GotFocus(object sender, RoutedEventArgs e)
        //{
        //    SearchGvTextBox.Text = "";
        //    SearchGvTextBox.Foreground = Brushes.Black;
        //    SearchGvTextBox.GotFocus -= SearchGvTextBox_GotFocus;
        //    if (!gotFocus)
        //        gotFocus = true;
        //}
    }
}
