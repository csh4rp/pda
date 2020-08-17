using ProcessDataArchiver.DataCore.DbEntities;
using System;
using System.Collections;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace ProcessDataArchiver.WinGui.Windows.Dialogs
{
    /// <summary>
    /// Interaction logic for SelectGvDialog.xaml
    /// </summary>
    public partial class SelectGvDialog : Window
    {
        private bool loaded;
        private bool? discrete;
        public ICollectionView FilteredVariables { get; set; }
        public IEnumerable<GlobalVariable> GlobalVariables { get; }
        public GlobalVariable SelectedGv { get; set; }
        public IList<GlobalVariable> SelectedGvs { get; set; }

        public bool Applied { get; set; }

        public SelectGvDialog()
        {
            InitializeComponent();

            loaded = true;


            GlobalVariables = EntityContext.GetContext().GlobalVariables;
            FilteredVariables = CollectionViewSource.GetDefaultView(GlobalVariables);
            this.DataContext = this;
        }

        //public SelectGvDialog(bool discrete)
        //{
        //    InitializeComponent();
        //    loaded = true;

            
        //    var cnx = EntityContext.GetContext();
        //    if (discrete)
        //    {
        //        GlobalVariables = cnx.GlobalVariables.Where(g => g.NetType == typeof(bool));
        //    }
        //    else
        //    {
        //        GlobalVariables = cnx.GlobalVariables.Where(g => g.NetType != typeof(bool));
        //    }
        //    DataTypeCb.Visibility = Visibility.Hidden;
        //    DataTypeTb.Visibility = Visibility.Hidden;

        //    FilteredVariables = CollectionViewSource.GetDefaultView(GlobalVariables);
        //    this.DataContext = this;
        //}

        public SelectGvDialog(bool multiple, bool? discrete = null):this()
        {

            this.discrete = discrete;
            if (multiple)
            {
                GvListView.SelectionMode = SelectionMode.Multiple;
            }
            else
            {
                GvListView.SelectionMode = SelectionMode.Single;
            }

            var cnx = EntityContext.GetContext();

            if (discrete != null)
            {
                DataTypeCb.Visibility = Visibility.Hidden;
                DataTypeTb.Visibility = Visibility.Hidden;
                if (discrete == true)
                {
                    GlobalVariables = cnx.GlobalVariables.Where(g => g.NetType == typeof(bool));
                }
                else
                {
                    GlobalVariables = cnx.GlobalVariables.Where(g => g.NetType != typeof(bool));
                }
                FilteredVariables = CollectionViewSource.GetDefaultView(GlobalVariables);
            }
        }


        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            var sel = GvListView.SelectedItems;
            SelectedGvs = new List<GlobalVariable>();
            foreach (var gv in sel)
            {
                SelectedGvs.Add(gv as GlobalVariable);
            }
            //var type = x.GetType();
            
            if (SelectedGvs != null)
            {
                if(SelectedGvs.Count<2)
                {
                    SelectedGv = SelectedGvs[0];
                }
                Applied = true;
                this.Close();
            }


            ///
            //SelectedGv = GvListView.SelectedItem as GlobalVariable;
            //if (SelectedGv != null)
            //{
            //    Applied = true;
            //    this.Close();
            //}
            //else
            //{
            //    SelectedGvs = GvListView.SelectedItems as IEnumerable<GlobalVariable>;
            //    if (SelectedGvs != null)
            //    {

            //    }
            //}

        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void FilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (loaded)
            {
                if (discrete == null)
                {
                    string name = ((sender as ComboBox).SelectedItem as ComboBoxItem).Name;
                    SetFilter(FilterTextBox.Text, name);
                }
                else if (discrete == true)
                {
                    SetFilter(FilterTextBox.Text, "Discrete");
                }
                else if (discrete == false)
                {
                    SetFilter(FilterTextBox.Text, "Analog");
                }
            }
        }

        private void DataTypeCb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (loaded)
            {
                if (discrete == null)
                {
                    string name = ((sender as ComboBox).SelectedItem as ComboBoxItem).Name;
                    SetFilter(FilterTextBox.Text, name);
                }
                else if(discrete == true)
                {
                    SetFilter(FilterTextBox.Text, "Discrete");
                }
                else if(discrete == false)
                {
                    SetFilter(FilterTextBox.Text, "Analog");
                }
            }
        }


        private void SetFilter(string text, string type)
        {
            if (!string.IsNullOrEmpty(text) && type.Equals("All"))
            {
                FilteredVariables.Filter = (g) =>
                {
                    var gv = g as GlobalVariable;
                    if (gv.Name.StartsWith(text))
                        return true;
                    return false;
                };
            }
            else if(!string.IsNullOrEmpty(text) && type.Equals("Discrete"))
            {
                FilteredVariables.Filter = (g) =>
                {
                    var gv = g as GlobalVariable;
                    if (gv.Name.StartsWith(text) && gv.NetType==typeof(bool))
                        return true;
                    return false;
                };
            }
            else if (!string.IsNullOrEmpty(text) && type.Equals("Analog"))
            {
                FilteredVariables.Filter = (g) =>
                {
                    var gv = g as GlobalVariable;
                    if (gv.Name.StartsWith(text) && gv.NetType != typeof(bool))
                        return true;
                    return false;
                };
            }
            else if (string.IsNullOrEmpty(text) && type.Equals("All"))
            {
                FilteredVariables.Filter = null;
            }
            else if (string.IsNullOrEmpty(text) && type.Equals("Discrete"))
            {
                FilteredVariables.Filter = (g) =>
                {
                    var gv = g as GlobalVariable;
                    if (gv.NetType == typeof(bool))
                        return true;
                    return false;
                };
            }
            else if (string.IsNullOrEmpty(text) && type.Equals("Analog"))
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

        private void GvListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var sel = GvListView.SelectedItem;
            if (sel != null)
            {
                SelectButton.IsEnabled = true;
            }
            else
            {
                SelectButton.IsEnabled = false;
            }
        }
    }
}
