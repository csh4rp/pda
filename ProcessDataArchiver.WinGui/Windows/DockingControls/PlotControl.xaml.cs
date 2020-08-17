using ProcessDataArchiver.DataCore.DbEntities.Tags;
using ProcessDataArchiver.WinGui.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ProcessDataArchiver.WinGui.Windows.DockingControls
{
    /// <summary>
    /// Interaction logic for PlotControl.xaml
    /// </summary>
    public partial class PlotControl : UserControl
    {
        private double zoomValue, horizontalOffset,verticalOffset;
        private bool isStatic,loaded;
        private DateTime minDate, maxDate;
        private double minValue = 0, maxValue = 1;

        private TimeSpan timeRange = new TimeSpan(0, 1, 0);
        private TimeSpan refreshSpan = new TimeSpan(0, 0, 0,0,250);


        private int yMain=4, xMain=4, yMinor=1, xMinor=1;
        private Color plotColor = Colors.White, gridColor = Colors.Gray;
        private double maxVal =1, minVal= 0;



        private double[] splitedValue;
        private DateTime[] splitedDates;

        private Timer timer;

        private bool sizeChanged,started;
        public const int VK_LBUTTON = 0x01;

        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(int vKey);

        public List<PlotVariable> PlotVariables { get; set; } = new List<PlotVariable>();

        public bool IsStatic
        {
            get { return isStatic; }
            set
            {
                if (isStatic != value)
                {
                    isStatic = value;
                    ChangePlotType();
                }
            }
        }

        public int YaxisMainGrids
        {
            get { return yMain; }
            set
            {
                yMain = value;
                DrawPlot();

            }
        }


        public int YaxisMinorGrids
        {
            get { return yMinor; }
            set
            {
                yMinor = value;
                DrawPlot();
            }
        }


        public int XaxisMainGrids
        {
            get { return xMain; }
            set
            {
                xMain = value;
                DrawPlot();
            }
        }


        public int XaxisMinorGrids
        {
            get { return xMinor; }
            set
            {
                xMinor = value;
                DrawPlot();
            }
        }


        public Color PlotBackground
        {
            get { return plotColor; }
            set
            {
                plotColor = value;
                PlotCanvas.Background = new SolidColorBrush(value);
            }
        }



        public Color GridColor
        {
            get { return gridColor; }
            set
            {
                gridColor = value;
                DrawPlot();
            }
        }


        public double GridLinesThickness { get; set; } = 1;
        public double SmallGridLinesThickness { get; set; } = 0.5;

        

        public double ZoomValue
        {
            get { return zoomValue; }
            set
            {
                if (zoomValue != value)
                {
                    zoomValue = value;
                    ZoomPlot();
                }
            }
        }

        public double MaxValue
        {
            get { return maxValue; }
            set
            {
                maxValue = value;
                //if(loaded)
                ZoomPlot();
            }
        }

        public double MinValue
        {
            get { return minValue; }
            set
            {
                minValue = value;
                //if(loaded)
                ZoomPlot();
            }
        }

        public DateTime FromDate
        {
            get { return minDate; }
            set
            {
                minDate = value;
            }
        }

        public DateTime ToDate
        {
            get { return maxDate; }
            set
            {
                maxDate = value;
            }
        }

        public TimeSpan TimeRange
        {
            get { return timeRange; }
            set
            {
                if (!timeRange.Equals(value))
                {
                    timeRange = value;
                    ChangeTimeRange();
                }
            }
        }

        public TimeSpan RefreshSpan
        {
            get { return refreshSpan; }
            set
            {
                refreshSpan = value;
            }
        }



        public PlotControl()
        {
            InitializeComponent();
            loaded = true;

            this.SizeChanged += PlotControl_SizeChanged;
            new DispatcherTimer(TimeSpan.FromMilliseconds(50), DispatcherPriority.Normal,
                delegate 
                {
                    if (sizeChanged)
                    {
                        bool mouseDown = GetAsyncKeyState(VK_LBUTTON) < 0;

                        //if (mouseDown && started && !IsStatic && timer.Enabled)
                        //{
                        //    timer.Stop();
                        //}

                        //if (!mouseDown)
                        //{
                        //    Prepare();
                        //    DrawPlot();

                        //    if (started && !IsStatic)
                        //    {
                        //        timer.Start();
                        //    }


                        //    sizeChanged = false;
                        //}
                    }
                }, Dispatcher);

            

        }

        private void PlotControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            sizeChanged = true;
            
        }

        private void PlotCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
           
            ZoomPlot();
        }

        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var sc = sender as ScrollViewer;
            horizontalOffset = sc.HorizontalOffset;
            verticalOffset = sc.VerticalOffset;

            if (!IsStatic && !started)
                RefreshStoped();
            else if (IsStatic)
            {
                RefreshStoped();
            }
        }

        private void PlotBorder_SizeChanged(object sender, SizeChangedEventArgs e)
        {
           // ZoomPlot();

        }

        public void Prepare()
        {
            SetValues();
            SetDates();

            if (PlotCanvas.ActualHeight > 0 && PlotCanvas.ActualWidth > 0)
            {
                splitedValue = SplitValues(MinValue, MaxValue, (int)PlotCanvas.ActualHeight).ToArray();
                splitedDates = SplitDateTimes(FromDate, ToDate, (int)PlotCanvas.ActualWidth).ToArray();
            }

            PlotCanvas.Background = new SolidColorBrush(PlotBackground);
            DrawPlot();

        }


        public void DrawPlot()
        {
            if (splitedValue != null)
            {

                DrawXaxisValues();
                DrawYaxisValues();
                DrawGraphLines(verticalOffset, horizontalOffset);
                foreach (var pv in PlotVariables)
                {
                    PlotCanvas.Children.Add(DrawPolyLine(pv));
                }
            }
        }


        private Polyline DrawPolyLine(PlotVariable pv)
        {
            IEnumerable<KeyValuePair<DateTime, double>> kvs = pv.Values;

            double width = PlotCanvas.ActualWidth;
            double height = PlotCanvas.ActualHeight;


            List<Point> pList = new List<Point>();

            foreach (var kv in kvs)
            {
                double currentValue = kv.Value;
                DateTime currentKey = kv.Key;

                double currentDiff = (currentKey - FromDate).TotalMinutes;
                double maxDiff = (ToDate - FromDate).TotalMinutes;

                pList.Add(new Point(((currentDiff) * width / (maxDiff))
                    , ((currentValue - MinValue) * height / (MaxValue - MinValue))));
            }

            Polyline pline = new Polyline();

            pline.Stroke = new SolidColorBrush(pv.LineColor);
            pline.StrokeThickness = pv.LineThickness;
            pline.Points = new PointCollection(pList);

            return pline;
        }




        private void DrawXaxisValues()
        {
            Grid grid = new Grid();

            double spanY = 0.0;

            if ((PlotBorder.ActualWidth - 2) >= PlotCanvas.ActualWidth)
                spanY = (PlotBorder.ActualWidth - 2) / (XaxisMainGrids+1);
            else
                spanY = (PlotBorder.ActualWidth - 20) / (XaxisMainGrids+1);

            int countX = XaxisMainGrids+2;

            DateTime begin = splitedDates[(int)horizontalOffset];
            DateTime end;

            if ((PlotBorder.ActualWidth - 2) < PlotCanvas.ActualWidth)
            {
                if ((int)(horizontalOffset + PlotBorder.ActualWidth - 19) >= splitedDates.Length)
                    end = splitedDates.Last();
                else if (((int)(horizontalOffset + PlotBorder.ActualWidth - 19)) < 0)
                    end = splitedDates.Last();
                else
                    end = splitedDates[(int)(horizontalOffset + PlotBorder.ActualWidth - 19)];
            }
            else
            {
                if ((int)(horizontalOffset + PlotBorder.ActualWidth - 2)>= splitedDates.Length)
                    end = splitedDates.Last();
                else 
                    end = splitedDates[(int)(horizontalOffset + PlotBorder.ActualWidth - 2)];
            }

            DateTime[] splitedValues = SplitDateTimes(begin, end,countX-1).ToArray();


            ColumnDefinition[] columns = new ColumnDefinition[countX];
            TextBlock[] textBlocks = new TextBlock[countX];

            for (int i = 0; i < countX; i++)
            {
                textBlocks[i] = new TextBlock();
                textBlocks[i].HorizontalAlignment = HorizontalAlignment.Center;
                textBlocks[i].Text = splitedValues[i].FormatString();


                columns[i] = new ColumnDefinition();

                if (i == 0 || i == (countX - 1))
                    columns[i].Width = new GridLength(spanY + 100);/////
                else
                    columns[i].Width = new GridLength(spanY);

                if (i == 0)
                {
                    Grid.SetColumnSpan(textBlocks[i], 1);
                    Grid.SetColumn(textBlocks[i], i);

                    textBlocks[i].Margin = new Thickness(0, 10, spanY - 100, 0);////
                }

                else if (i == 1)
                {
                    Grid.SetColumn(textBlocks[i], i - 1);
                    Grid.SetColumnSpan(textBlocks[i], 2);

                        textBlocks[i].Margin = new Thickness(100, 10, 0, 0);//
                }

                else
                {
                    Grid.SetColumn(textBlocks[i], i - 1);
                    Grid.SetColumnSpan(textBlocks[i], 2);
                    if (i == (countX - 1))
                    {
                            textBlocks[i].Margin = new Thickness(0, 10, 100, 0);//
                    }
                    else
                    {
                            textBlocks[i].Margin = new Thickness(0, 10, 0, 0);
                    }
                        
                }

                grid.ColumnDefinitions.Add(columns[i]);
                grid.Children.Add(textBlocks[i]);

            }

            KeysGrid.Children.Clear();
            KeysGrid.Children.Add(grid);
        }




        private void DrawYaxisValues()
        {
            Grid grid = new Grid();
            double spanX = 0.0;

            if ((PlotBorder.ActualHeight - 2) >= PlotCanvas.ActualHeight)
                spanX = (PlotBorder.ActualHeight - 2) / (YaxisMainGrids+1);
            else
                spanX = (PlotBorder.ActualHeight - 20) / (YaxisMainGrids+1);

            int countY = YaxisMainGrids + 2;
            double min = 0, max = 0;


            max = splitedValue[(int)verticalOffset];
            if ((PlotBorder.ActualHeight - 2) < PlotCanvas.ActualHeight)
            {
                if ((int)(verticalOffset + PlotBorder.ActualHeight - 19) >= splitedValue.Length)
                    min = splitedValue.Last();
                else
                    min = splitedValue[(int)(verticalOffset + PlotBorder.ActualHeight - 19)];
            }
            else
            {
                if ((int)(verticalOffset + PlotBorder.ActualHeight - 2) >= splitedValue.Length)
                    min = splitedValue.Last();
                else 
                    min = splitedValue[(int)(verticalOffset + PlotBorder.ActualHeight - 2)];
            }

            double[] splitedValues = SplitValues(min, max,countY-1).ToArray();

            RowDefinition[] rows = new RowDefinition[countY];
            TextBlock[] textBlocks = new TextBlock[countY];


            for (int i = 0; i < countY; i++)
            {
                rows[i] = new RowDefinition();

                if (i == 0)
                    rows[i].Height = new GridLength(spanX + 10);
                else if (i == (countY - 1))
                    rows[i].Height = new GridLength(spanX + 60);///
                else
                    rows[i].Height = new GridLength(spanX);
            }


            for (int i = 0; i < countY; i++)
            {
                textBlocks[i] = new TextBlock();
                textBlocks[i].VerticalAlignment = VerticalAlignment.Center;
                textBlocks[i].HorizontalAlignment = HorizontalAlignment.Right;
                textBlocks[i].TextWrapping = TextWrapping.Wrap;

                if (i == 0)
                {
                    Grid.SetRowSpan(textBlocks[i], 1);
                    Grid.SetRow(textBlocks[i], i);
                    textBlocks[i].Margin = new Thickness(0, 0, 5, spanX - 10);
                    textBlocks[i].Text = splitedValues[i].ToString("F2");
                }
                else if (i == 1)
                {
                    Grid.SetRowSpan(textBlocks[i], 2);
                    Grid.SetRow(textBlocks[i], i - 1);

                    textBlocks[i].Margin = new Thickness(0, 10, 5, 0);
                    textBlocks[i].Text = (splitedValues[i]).ToString("F2");
                }
                else
                {
                    Grid.SetRowSpan(textBlocks[i], 2);
                    Grid.SetRow(textBlocks[i], i - 1);

                    if (i == (countY - 1))
                    {
                        textBlocks[i].Margin = new Thickness(0, 0, 5, 60);
                        textBlocks[i].Text = (splitedValues[countY - 1]).ToString("F2");
                    }
                    else
                    {
                        textBlocks[i].Margin = new Thickness(0, 0, 5, 0);
                        textBlocks[i].Text = (splitedValues[i]).ToString("F2");
                    }
                }

            }

            for (int i = 0; i < countY; i++)
            {
                grid.RowDefinitions.Add(rows[i]);
                grid.Children.Add(textBlocks[i]);
            }
            ValuesGrid.Children.Clear();
            ValuesGrid.Children.Add(grid);
        }

        private Line DrawLine(double x1, double y1, double x2, double y2,
            SolidColorBrush brush = null, double thickness = 1)
        {
            Line line = new Line();
            line.X1 = x1;
            line.X2 = x2;
            line.Y1 = y1;
            line.Y2 = y2;
            line.Stroke = brush??Brushes.Black;
            line.StrokeThickness = thickness;

            return line;
        }

        private void DrawGraphLines(double verOffset = 0, double horOffset = 0)
        {
            PlotCanvas.Children.Clear();

            double width, height;


            double spanX = 0;
            if ((PlotBorder.ActualHeight - 2) >= PlotCanvas.ActualHeight)
            {
                spanX = (PlotBorder.ActualHeight-0) / (YaxisMainGrids+1);/////
                height = PlotBorder.ActualHeight-0;
            }
            else
            {
                spanX = (PlotBorder.ActualHeight - 20) / (YaxisMainGrids+1);
                height = PlotBorder.ActualHeight - 20;
            }

            double spanY = 0;// PlotBorder.ActualWidth / XasixLines;
            if ((PlotBorder.ActualWidth - 2) >= PlotCanvas.ActualWidth)
            {
                spanY = (PlotBorder.ActualWidth) / (XaxisMainGrids+1);
                width = PlotBorder.ActualWidth-20;
            }
            else
            {
                spanY = (PlotBorder.ActualWidth - 20) / (XaxisMainGrids+1);
                width = PlotBorder.ActualWidth-0;
            }
            double vOff = (PlotCanvas.ActualHeight - (PlotBorder.ActualHeight-2))-verOffset;

            

            // Poziome
            for (double i = 0; i < height; i += spanX)
            {
                double h;
                if ((PlotBorder.ActualHeight - 2) >= PlotCanvas.ActualHeight)
                    h = i;
                else
                    h = i + 18;
                Line lineX = DrawLine(horOffset, h+vOff, PlotBorder.ActualWidth + horOffset, h+ vOff);
                lineX.StrokeThickness = GridLinesThickness;
                lineX.Stroke = new SolidColorBrush(GridColor);
                PlotCanvas.Children.Add(lineX);

                double smallSpanX = spanX / (YaxisMinorGrids+1);

                for (double j = (h+smallSpanX); j < ((h+spanX)-1); j += smallSpanX)////
                {
                    Line smallLineX = DrawLine(horOffset, j+ vOff, PlotBorder.ActualWidth + horOffset, j+ vOff);
                    smallLineX.StrokeThickness = SmallGridLinesThickness;
                    smallLineX.Stroke = new SolidColorBrush(GridColor);
                    PlotCanvas.Children.Add(smallLineX);
                }

            }

            // Pionowe
            for (double i = 0; i < width; i += spanY)
            {
                Line lineY = DrawLine(i + horOffset, vOff, i + horOffset, PlotBorder.ActualHeight+vOff);
                lineY.StrokeThickness = GridLinesThickness;
                lineY.Stroke = new SolidColorBrush(GridColor);
                PlotCanvas.Children.Add(lineY);

                double smallSpanY = spanY / (XaxisMinorGrids+1);

                for (double j = (i+smallSpanY); j < ((i + spanY)-1); j += smallSpanY)/////
                {
                    Line smallLineY = DrawLine(j + horOffset, vOff, j + horOffset, PlotBorder.ActualHeight+vOff);
                    smallLineY.StrokeThickness = SmallGridLinesThickness;
                    smallLineY.Stroke = new SolidColorBrush(GridColor);
                    PlotCanvas.Children.Add(smallLineY);
                }


            }

        }


        private IEnumerable<DateTime> SplitDateTimes(DateTime start, DateTime end, int count)
        {

            if (count > 0)
            {
                var diff = (end - start);

                double msDiff = (diff.TotalMilliseconds / (count));

                for (int i = 0; i <= count; i++)
                {
                    yield return start.AddMilliseconds(i * msDiff);
                }
            }
            else
            {
                yield return start;
                yield return end;
            }
        }


        private IEnumerable<double> SplitValues(double min, double max, int count)
        {
            double diff = Math.Abs((max - min) / (count));

            for (int i = 0; i <= count; i++)
            {
                yield return max - (i * diff);
            }

        }

        private void SetValues()
        {
            if (!IsStatic)
            {


            }
            else if(PlotVariables.Count>0)
            {
                MaxValue = PlotVariables.SelectMany(p => p.Values.Select(s => s.Value)).Max();
                MinValue = PlotVariables.SelectMany(p => p.Values.Select(s => s.Value)).Min();
            }
        }

        private void SetDates()
        {
            if (!IsStatic)
            {

                ToDate = DateTime.Now;
                FromDate = DateTime.Now - TimeRange;

                foreach (var pv in PlotVariables)
                {
                    var toDelete = pv.Values.Where(p => p.Key < FromDate);
                    if (toDelete != null && toDelete.Count() > 0)
                    {
                        int fIndex = pv.Values.IndexOf(toDelete.First());
                        pv.Values.RemoveRange(fIndex, (toDelete.Count()-1));
                    }
                }
            }

        }

        private void ChangePlotType()
        {
            if (IsStatic)
            {
                if (timer != null)
                    timer.Stop(); 
            }
            else
            {
                if (timer == null)
                {
                    timer = new Timer(refreshSpan.TotalMilliseconds);
                    timer.Elapsed += RefreshPlot;
                    timer.Start();
                }
                else
                {
                    timer.Stop();
                    timer.Elapsed -= RefreshPlot;
                    timer = null;
                }
            }
        }

        private  void RefreshPlot(object sender, ElapsedEventArgs e)
        {
            SetDates();
            foreach (var item in PlotVariables)
            {
                if (item.Tag != null)
                {
                    var val = item.Tag.CurrentValue;
                    double value = double.Parse(val.ToString());

                    item.Values.Add(new KeyValuePair<DateTime, double>(DateTime.Now, value));


                    if (item.Values.Select(s => s.Value).Last() > this.MaxValue)
                        MaxValue = item.Values.Select(s => s.Value).ToList().Last();

                    if (item.Values.Select(s => s.Value).Last() < this.MinValue)
                        MinValue = item.Values.Select(s => s.Value).ToList().Last();
                }
            }

            splitedValue = SplitValues(MinValue, MaxValue, (int)PlotCanvas.ActualHeight).ToArray();
            splitedDates = SplitDateTimes(FromDate, ToDate, (int)PlotCanvas.ActualWidth).ToArray();

            Dispatcher.Invoke(() =>
            {
                DrawPlot();
            });
            
        }

        private void ZoomPlot()
        {
            Dispatcher.Invoke(() =>
            {
                double bWidth = PlotBorder.ActualWidth;
                double bHeight = PlotBorder.ActualHeight;

                if (bWidth > 0 && bHeight > 0)
                {

                    if (zoomValue > 1)
                    {
                        PlotCanvas.Height = zoomValue * bHeight;
                        PlotCanvas.Width = zoomValue * bWidth;
                    }
                    else
                    {
                        PlotCanvas.Height = PlotBorder.Height - 2;//bHeight - 2;
                        PlotCanvas.Width = PlotBorder.Width - 2;//bWidth - 2;
                    }



                    if (!IsStatic && !started)
                    {
                        RefreshStoped();
                    }
                    else if (IsStatic)
                    {
                        splitedValue = SplitValues(MinValue, MaxValue, (int)PlotCanvas.ActualHeight).ToArray();
                        splitedDates = SplitDateTimes(FromDate, ToDate, (int)PlotCanvas.ActualWidth).ToArray();

                        DrawPlot();

                    }
                }
            });
        }

        public void Stop()
        {
            if (timer != null)
            {
                timer.Stop();
                timer.Elapsed -= RefreshPlot;
                timer = null;
            }
            started = false;
        }


        public void Start()
        {
            if (timer == null)
            {
                timer = new Timer(refreshSpan.TotalMilliseconds);
                timer.Elapsed += RefreshPlot;
            }
            timer?.Start();
            started = true;
        }

        private void ChangeTimeRange()
        {
            //if (timer!=null && timer.Enabled)
            //{
            //    timer.Stop();
            //    timer.Elapsed -= RefreshPlot;
            //    timer.Dispose();

            //    timer = new Timer(refreshSpan.TotalMilliseconds);
            //    timer.Elapsed += RefreshPlot;
            //    timer.Start();
            //}

            if(!IsStatic && !started)
            {
                RefreshStoped();
            }
            


        }

        private void RefreshStoped()
        {
            //SetDates();
            //foreach (var item in PlotVariables)
            //{
            //    var val = item.Tag.CurrentValue;
            //    double value = double.Parse(val.ToString());

            //    item.Values.Add(new KeyValuePair<DateTime, double>(DateTime.Now, value));


            //    if (item.Values.Select(s => s.Value).Last() > this.MaxValue)
            //        MaxValue = item.Values.Select(s => s.Value).ToList().Last();

            //    if (item.Values.Select(s => s.Value).Last() < this.MinValue)
            //        MinValue = item.Values.Select(s => s.Value).ToList().Last();

            //}

            splitedValue = SplitValues(MinValue, MaxValue, (int)PlotCanvas.ActualHeight).ToArray();
            splitedDates = SplitDateTimes(FromDate, ToDate, (int)PlotCanvas.ActualWidth).ToArray();

            Dispatcher.Invoke(() =>
            {
                DrawPlot();
            });
        }


        public void Draw()
        {
            SetDates();
            if (PlotVariables.Count != 0 )
            {
                var values = PlotVariables.SelectMany(p => p.Values.Select(s => s.Value));

                if (values.Count() > 0)
                {
                    MaxValue = values.Max();
                    MinValue = values.Min();
                }
            }
                splitedValue = SplitValues(MinValue, MaxValue, (int)PlotCanvas.ActualHeight).ToArray();
                splitedDates = SplitDateTimes(FromDate, ToDate, (int)PlotCanvas.ActualWidth).ToArray();
            
                DrawPlot();
            

        }


    }




    public static class Helper
    {
        public static string FormatString(this DateTime date)
        {           
            if(date.Second<10)
                return "  " + date.ToShortTimeString()+":0"+date.Second + "\n" + date.ToShortDateString();
            else
                return "  " + date.ToShortTimeString() + ":" + date.Second + "\n" + date.ToShortDateString();
        }
    }

}
