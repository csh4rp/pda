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
using System.ComponentModel;
using System.Threading;
using ProcessDataArchiver.DataCore.Database.DbProviders;

namespace ProcessDataArchiver.WinGui.Dialogs
{
    /// <summary>
    /// Interaction logic for DetailsDialogBox.xaml
    /// </summary>
    public partial class DetailsDialogBox : Window,INotifyPropertyChanged
    {
        private bool clicked = false;
        private CancellationTokenSource cts;
        private Action action;
        private string message, details;
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler OperationCanceled;
        public string Message
        {
            get { return message; }
            set
            {
                message = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Message)));
            }
        }
        public string Details
        {
            get { return details; }
            set
            {
                details = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Details)));
            }
        }
        public bool Valid { get; set; }

        public DetailsDialogBox()
        {
            InitializeComponent();
            this.DataContext = this;
            this.Top = (MainWindow.TopPos + (MainWindow.WinHeight / 2)) - this.Height / 2;
            this.Left = (MainWindow.LeftPos + (MainWindow.WinWidth / 2)) - this.Width / 2;
        }

        public DetailsDialogBox(string msg,string title,CancellationTokenSource ct):this()
        {
            Message = msg;
            this.Title = title;
            cts = ct;

        }

        public  void ShowAndInvoke()
        {
            Indicator.IsActive = true;
            ErrorImg.Visibility = Visibility.Hidden;
            ApplyImg.Visibility = Visibility.Hidden;
            OkButton.IsEnabled = false;
            CancelButton.IsEnabled = true;
            DetailsButton.IsEnabled = false;
            this.Show();
        }

        public void SetWindowState(object sender,bool res)
        {
            if (res)
            {
                Indicator.IsActive = false;
                ApplyImg.Visibility = Visibility.Visible;
                Message = "Próba nawiązania połączenia przebiegła pomyślnie";
            }
            else
            {
                Indicator.IsActive = false;
                Message = "Wystąpił błąd podczas próby nawiązania połączenia";
                ErrorImg.Visibility = Visibility.Visible;
                ApplyImg.Visibility = Visibility.Hidden;
                if (!string.IsNullOrEmpty(Details))
                {
                    DetailsButton.IsEnabled = true;
                }
            }
            OkButton.IsEnabled = true;
        }



        private void DetailsButton_Click(object sender, RoutedEventArgs e)
        {
            if (!clicked)
            {
                ExpandPolygon.Stroke = Brushes.Transparent;
                ExpandPolygon.Fill = Brushes.Transparent;
                ExpandedPolygon.Stroke = Brushes.Black;
                ExpandedPolygon.Fill = Brushes.Black;
                clicked = true;

                DetailsGrid.Height = 200;
                this.Height += 200;
            }
            else
            {
                ExpandPolygon.Stroke = Brushes.Black;
                ExpandPolygon.Fill = Brushes.Black;
                ExpandedPolygon.Stroke = Brushes.Transparent;
                ExpandedPolygon.Fill = Brushes.Transparent;
                clicked = false;
                DetailsGrid.Height = 0;
                this.Height -= 200;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            OperationCanceled?.Invoke(this, EventArgs.Empty);
            cts.Cancel();
            this.Close();
        }

        private void Dismiss(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
