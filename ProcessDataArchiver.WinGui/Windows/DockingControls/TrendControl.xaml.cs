using ProcessDataArchiver.DataCore.DbEntities;
using ProcessDataArchiver.WinGui.ViewModels;
using ProcessDataArchiver.WinGui.Windows.Commands;
using ProcessDataArchiver.WinGui.Windows.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
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
using Xceed.Wpf.Toolkit;

namespace ProcessDataArchiver.WinGui.Windows.DockingControls
{
    /// <summary>
    /// Interaction logic for TrendControl.xaml
    /// </summary>
    public partial class TrendControl : UserControl
    {
        private bool isStarted;
        private bool loaded;
       
        private double zoomValue = 1.0;
        private TrendOptions trendOptions;
        private bool shown;
        public TimeSpan Timespan
        {
            get { return PlotControl.TimeRange; }
            set
            {
                PlotControl.TimeRange = value;
            }
        }

        public DateTime FromDate
        {
            get { return PlotControl.FromDate; }
            set
            {
                PlotControl.FromDate = value;
            }
        }
        public DateTime ToDate
        {
            get { return PlotControl.ToDate; }
            set
            {
                PlotControl.ToDate = value;
            }
        }


        public ObservableCollection<PlotVariable> PlotVariables { get; set; } =
            new ObservableCollection<PlotVariable>();

        public ObservableCollection<double> Thickness { get; set; } =
            new ObservableCollection<double>();

        public ObservableCollection<string> ArchiveNames { get; set; } =
            new ObservableCollection<string>();

        public TrendControl()
        {
            InitializeComponent();
            loaded = true;
            TrendOptionsListView.DataContext = PlotVariables;
            Init();
            
        }

        private void Init()
        {
            
            Thickness.Add(1);
            Thickness.Add(2);
            Thickness.Add(5);
            Thickness.Add(10);

            var archives = EntityContext.GetContext().TagArchives.Select(a => a.Name);
            foreach (var item in archives)
            {
                ArchiveNames.Add(item);
            }
            Timespan = new TimeSpan(0, 1, 0);
            TrendTimeUpDown.Text = "00:01:00";
            Xceed.Wpf.Toolkit.PropertyGrid.PropertyDefinition xMaj, xMin, yMaj, yMin, pb, gc, maxV, minV;
            //Task.Run(() =>
            //{
            //    xMaj = new Xceed.Wpf.Toolkit.PropertyGrid.PropertyDefinition();
            //    xMaj.TargetProperties.Add("XaxisMainGrids");

            //    xMin = new Xceed.Wpf.Toolkit.PropertyGrid.PropertyDefinition();
            //    xMin.TargetProperties.Add("XaxisMinorGrids");
                

            //    yMaj = new Xceed.Wpf.Toolkit.PropertyGrid.PropertyDefinition();
            //    yMaj.TargetProperties.Add("YaxisMainGrids");
                

            //    yMin = new Xceed.Wpf.Toolkit.PropertyGrid.PropertyDefinition();
            //    yMin.TargetProperties.Add("YaxisMinorGrids");
                

            //    pb = new Xceed.Wpf.Toolkit.PropertyGrid.PropertyDefinition();
            //    pb.TargetProperties.Add("PlotBackGround");
                

            //    gc = new Xceed.Wpf.Toolkit.PropertyGrid.PropertyDefinition();
            //    gc.TargetProperties.Add("GridColor");
                

            //    maxV = new Xceed.Wpf.Toolkit.PropertyGrid.PropertyDefinition();
            //    maxV.TargetProperties.Add("MaxValue");                
            //    minV = new Xceed.Wpf.Toolkit.PropertyGrid.PropertyDefinition();
            //    minV.TargetProperties.Add("MinValue");

            //    Dispatcher.Invoke(() =>
            //    {
            //        PropGrid.PropertyDefinitions.Add(xMaj);
            //        PropGrid.PropertyDefinitions.Add(xMin);
            //        PropGrid.PropertyDefinitions.Add(yMaj);
            //        PropGrid.PropertyDefinitions.Add(yMin);
            //        PropGrid.PropertyDefinitions.Add(pb);
            //        PropGrid.PropertyDefinitions.Add(gc);
            //        PropGrid.PropertyDefinitions.Add(maxV);
            //        PropGrid.PropertyDefinitions.Add(minV);
            //    });
            //});
            trendOptions = new TrendOptions(PlotControl);
            PropGrid.SelectedObject = trendOptions;

            
        }


        public bool CanAdd
        {
            get
            {
                return true;
            }
        }

        public bool CanRemove
        {
            get
            {
                return TrendOptionsListView.SelectedItem != null;
            }
        }

        public bool IsStarted
        {
            get
            {
                return isStarted;
            }
            private set
            {
                isStarted = value;
            }
        }

        public bool IsStatic
        {
            get
            {   
                if(PlotControl!=null)
                    return PlotControl.IsStatic;
                return false;
            }

            set
            {
                if(PlotControl!=null)
                    PlotControl.IsStatic = value;
            }
        }

        

        public void Add()
        {
            var v = new PlotVariable();
            PlotVariables.Add(v);
        }

        public void ChangeTimeRange(DateTime from, DateTime to)
        {
            FromDate = from;
            ToDate = to;
            PlotControl.FromDate = from;
            PlotControl.ToDate = to;
        }

        public void ChangeTimespan(TimeSpan timespan)
        {
            Timespan = timespan;
            PlotControl.TimeRange = Timespan;
        }

        public void Remove()
        {
            var sel = TrendOptionsListView.SelectedItem as PlotVariable;
            if (sel != null)
            {
                PlotVariables.Remove(sel);
            }
        }



        public void ShowAdvanced()
        {
            PlotOptionsDialog dialog = new PlotOptionsDialog();

            dialog.MaxValue = PlotControl.MaxValue;
            dialog.MinValue = PlotControl.MinValue;
            dialog.XaxisMajorGridLines = PlotControl.XaxisMainGrids;
            dialog.YaxisMajorGridLines = PlotControl.YaxisMainGrids;
            dialog.XaxisMinorGridLines = PlotControl.XaxisMinorGrids;
            dialog.YaxisMinorGridLines = PlotControl.YaxisMinorGrids;
            dialog.RefreshSpanMs = (int)PlotControl.RefreshSpan.TotalMilliseconds;
            dialog.PlotColor = PlotControl.PlotBackground;
            dialog.GridLinesColor = PlotControl.GridColor;

            dialog.ShowDialog();

            if (dialog.Changed)
            {
                PlotControl.MaxValue = dialog.MaxValue;
                PlotControl.MinValue = dialog.MinValue;
                PlotControl.XaxisMainGrids = dialog.XaxisMajorGridLines;
                PlotControl.YaxisMainGrids = dialog.YaxisMajorGridLines;
                PlotControl.XaxisMinorGrids = dialog.XaxisMinorGridLines;
                PlotControl.YaxisMinorGrids = dialog.YaxisMinorGridLines;
                PlotControl.RefreshSpan = new TimeSpan(0, 0, 0, 0, dialog.RefreshSpanMs);
                PlotControl.PlotBackground = (Color)dialog.PlotColor;
                PlotControl.GridColor = (Color)dialog.GridLinesColor;

                PlotControl.Prepare();
            }
        }

        public void Start()
        {
            IsStarted = true;

            if (PlotControl.IsStatic) {


            }
            else
            {
                foreach (var v in PlotVariables)
                {

                    v.Values = new List<KeyValuePair<DateTime, double>>();
                }
            }

            PlotControl.PlotVariables = PlotVariables.ToList();

            PlotControl.Start();

        }

        public void Stop()
        {
            IsStarted = false;
            PlotControl.Stop();
        }

        public void ZoomIn()
        {
            zoomValue *= 1.1;
        }

        public void ZoomOut()
        {
            if (zoomValue > 1.0)
                zoomValue /= 1.1;
            else
                zoomValue = 1.0;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            Add();
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            Remove();
        }

        private void Cb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        { 
            if (loaded)
            {

                if ((TrendTypeCb.SelectedItem as ComboBoxItem).Name.Equals("Active"))
                {

                    IsStatic = false;

                    TrendTimeUpDown.Visibility = Visibility.Visible;
                    StartTrendButton.Visibility = Visibility.Collapsed;

                    StartActiveTrendBtn.Visibility = Visibility.Visible;
                    PauseActiveTrendBtn.Visibility = Visibility.Visible;

                    FromTrendTb.Visibility = Visibility.Collapsed;
                    ToTrendTb.Visibility = Visibility.Collapsed;
                    FromDtPicker.Visibility = Visibility.Collapsed;
                    ToDtPicker.Visibility = Visibility.Collapsed;

                    TredSep1.Visibility = Visibility.Visible;

                    if (IsStarted)
                    {
                        StartActiveTrendBtn.IsEnabled = false;
                        PauseActiveTrendBtn.IsEnabled = true;
                    }
                    else
                    {
                        StartActiveTrendBtn.IsEnabled = true;
                        PauseActiveTrendBtn.IsEnabled = false;
                    }

                }
                else
                {
                    if (IsStarted)
                    {
                        Stop();
                    }

                    IsStatic = true;
                    StartTrendButton.Visibility = Visibility.Visible;
                    StartActiveTrendBtn.Visibility = Visibility.Collapsed;
                    PauseActiveTrendBtn.Visibility = Visibility.Collapsed;

                    TrendTimeUpDown.Visibility = Visibility.Collapsed;
                    FromTrendTb.Visibility = Visibility.Visible;
                    ToTrendTb.Visibility = Visibility.Visible;
                    FromDtPicker.Visibility = Visibility.Visible;
                    ToDtPicker.Visibility = Visibility.Visible;

                    TredSep1.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void StartTrendButton_Click(object sender, RoutedEventArgs e)
        {
            if (!IsStarted && !IsStatic)
            {
                Start();

            }
            else if (IsStarted && !IsStatic)
            {
                Stop();


            }
            else if (IsStatic)
            {
                var from = DateTime.Parse(FromDtPicker.Text);
                var to = DateTime.Parse(ToDtPicker.Text);

                ChangeTimeRange(from, to);
                Draw();
            }
        }

        private void UpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            string txt = TrendTimeUpDown.Text;
            if (!string.IsNullOrEmpty(txt))
            {
                var span = TimeSpan.Parse(txt);

                ChangeTimespan(span);
            }
        }

        private void SettingsTrendButton_Click(object sender, RoutedEventArgs e)
        {

            //  ShowAdvanced();
            if (!shown)
            {
                MainGrid.ColumnDefinitions[2].Width = new GridLength(250);
                shown = true;
            }
            else
            {
                MainGrid.ColumnDefinitions[2].Width = new GridLength(0);
                shown = false;
            }
        }

        private void ZoomInTrendButton_Click(object sender, RoutedEventArgs e)
        {
            zoomValue *= 1.1;
            PlotControl.ZoomValue = zoomValue;
        }

        private void ZoomOutTrendButton_Click(object sender, RoutedEventArgs e)
        {
            if (zoomValue > 1.0)
                zoomValue /= 1.1;
            else
                zoomValue = 1.0;

            PlotControl.ZoomValue = zoomValue;
        }

        private void AutoSizeTrendButton_Click(object sender, RoutedEventArgs e)
        {
            zoomValue = 1.0;
            PlotControl.ZoomValue = zoomValue;
        }

        public async void Draw()
        {
            foreach (var v in PlotVariables)
            {

                var data = await EntityContext.GetContext().DbProvider
                    .GetTagValuesAsync(v.Tag, FromDate, ToDate);
                v.Values = data.ToList();
            }

            PlotControl.PlotVariables = PlotVariables.ToList();

            PlotControl.Draw();
        }

        private void StartActiveTrendBtn_Click(object sender, RoutedEventArgs e)
        {
            Start();
            StartActiveTrendBtn.IsEnabled = false;
            PauseActiveTrendBtn.IsEnabled = true;
        }

        private void PauseActiveTrendBtn_Click(object sender, RoutedEventArgs e)
        {
            Stop();
            StartActiveTrendBtn.IsEnabled = true;
            PauseActiveTrendBtn.IsEnabled = false;
        }
    }
}
