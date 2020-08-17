using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
    /// Interaction logic for AdvancedPlotOptionsDialog.xaml
    /// </summary>
    public partial class PlotOptionsDialog : Window,INotifyPropertyChanged
    {
        private int xAxisMajor, yAxisMajor, xAxisMinor, yAxisMinor;
        private Color plotColor, gridColor;
        private TimeSpan refreshSpan;
        private double maxVal, minVal;

        public event PropertyChangedEventHandler PropertyChanged;

        public int XaxisMajorGridLines
        {
            get { return xAxisMajor; }
            set
            {
                xAxisMajor = value;
                OnPropertyChanged();
            }
        }
        public int YaxisMajorGridLines
        {
            get { return yAxisMajor; }
            set
            {
                yAxisMajor = value;
                OnPropertyChanged();
            }
        }
        public int XaxisMinorGridLines
        {
            get { return xAxisMinor; }
            set
            {
                xAxisMinor = value;
                OnPropertyChanged();
            }
        }
        public int YaxisMinorGridLines
        {
            get { return yAxisMinor; }
            set
            {
                yAxisMinor = value;
                OnPropertyChanged();
            }
        }

        public Color? PlotColor
        {
            get { return plotColor; }
            set
            {
                plotColor = (Color)value;
                OnPropertyChanged();
            }
        }

        public Color? GridLinesColor
        {
            get { return gridColor; }
            set
            {
                gridColor = (Color)value;
                OnPropertyChanged();
            }
        }

        public int RefreshSpanMs
        {
            get { return (int)refreshSpan.TotalMilliseconds; }
            set
            {
                refreshSpan = new TimeSpan(0,0,0,0,value);
                OnPropertyChanged();
            }
        }

        public double MaxValue
        {
            get { return maxVal; }
            set
            {
                maxVal = value;
                OnPropertyChanged();
            }
        }
        public double MinValue
        {
            get { return minVal; }
            set
            {
                minVal = value;
                OnPropertyChanged();
            }
        }

        public bool Changed { get; private set; }

        public PlotOptionsDialog()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        private void SetInitialValues()
        {
            XaxisMajorGrids.Text = XaxisMajorGridLines.ToString();
            YaxisMajorGrids.Text = YaxisMajorGridLines.ToString();
            XaxisMinorGrids.Text = XaxisMinorGridLines.ToString();
            XaxisMinorGrids.Text = XaxisMinorGridLines.ToString();

            PlotColorPicker.SelectedColor = PlotColor;
            GridColorPicker.SelectedColor = GridLinesColor;

            RefreshSpanUpDown.Text = ((int)refreshSpan.TotalMilliseconds).ToString();
            MaxValUpDown.Text = MaxValue.ToString();
            MinValUpDown.Text = MinValue.ToString();

        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            //XaxisMajorGridLines = int.Parse(XaxisMajorGrids.Text);
            //YaxisMajorGridLines = int.Parse(YaxisMajorGrids.Text);
            //XaxisMinorGridLines = int.Parse(XaxisMinorGrids.Text);
            //YaxisMinorGridLines = int.Parse(YaxisMinorGrids.Text);

            //PlotColor = (Color)PlotColorPicker.SelectedColor;
            //GridLinesColor = (Color)GridColorPicker.SelectedColor;

            //RefreshSpan = new TimeSpan(0, 0, 0, 0, int.Parse(RefreshSpanUpDown.Text));
            //MaxValue = double.Parse(MaxValUpDown.Text);
            //MinValue = double.Parse(MinValUpDown.Text);
            Changed = true;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Changed = false;
            this.Close();
        }


        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }



    }
}
