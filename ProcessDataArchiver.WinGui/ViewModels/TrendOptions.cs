using ProcessDataArchiver.WinGui.Windows.DockingControls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.ComponentModel.DataAnnotations;

namespace ProcessDataArchiver.WinGui.ViewModels
{
    public class TrendOptions
    {
        private PlotControl plot;
        private int yMain, xMain, yMinor, xMinor;
        private Color plotColor, gridColor;
        private double maxVal, minVal;

        public TrendOptions(PlotControl p)
        {
            plot = p;
            YaxisMainGrids = p.YaxisMainGrids;
            YaxisMinorGrids = p.YaxisMinorGrids;
            XaxisMainGrids = p.XaxisMainGrids;
            XaxisMinorGrids = p.XaxisMinorGrids;
            PlotBackground = p.PlotBackground;
            GridColor = p.GridColor;
            MaxValue = p.MaxValue;
            MinValue = p.MinValue;
        }

        [Category("Podziałki")]
        [DisplayName("Główne podziałki na osi Y")]
        public int YaxisMainGrids
        {
            get { return yMain; }
            set
            {
                yMain = value > 1 ? value :1;
                plot.YaxisMainGrids = value > 1 ? value : 1;
            }
        }

        [Category("Podziałki")]
        [DisplayName("Dodatkowe podziałki na osi Y")]
        public int YaxisMinorGrids
        {
            get { return yMinor; }
            set
            {
                yMinor = value > 0 ? value : 0;
                plot.YaxisMinorGrids = value > 0 ? value : 0;
            }
        }

        [Category("Podziałki")]
        [DisplayName("Główne podziałki na osi X")]
        public int XaxisMainGrids
        {
            get { return xMain; }
            set
            {

                xMain = value>1?value:1;
                plot.XaxisMainGrids = value > 1 ? value :1;
            }
        }

        [Category("Podziałki")]
        [DisplayName("Dodatkowe podziałki na osi Y")]
        public int XaxisMinorGrids
        {
            get { return xMinor; }
            set
            {
                xMinor = value > 0 ? value : 0;
                plot.XaxisMinorGrids = value > 0 ? value : 0;
            }
        }

        [Category("Kolory")]
        [DisplayName("Kolor wykresu")]
        public Color PlotBackground
        {
            get { return plotColor; }
            set
            {
                plotColor = value;
                plot.PlotBackground = value;
            }
        }

        [Category("Kolory")]
        [DisplayName("Kolor podziałek")]
        public Color GridColor
        {
            get { return gridColor; }
            set
            {
                gridColor = value;
                plot.GridColor = value;
            }
        }

        [Category("Wartości")]
        [DisplayName("Maksymalna wartość")]
        public double MaxValue
        {
            get { return maxVal; }
            set
            {
                maxVal = value;
                plot.MaxValue = value;
            }
        }
        [Category("Wartości")]
        [DisplayName("Minimalna wartość")]
        
        public double MinValue
        {
            get { return minVal; }
            set
            {
                minVal = value;
                plot.MinValue = value;
            }
        }
    }
}
