using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace ProcessDataArchiver.WinGui.Windows.Dialogs
{
    /// <summary>
    /// Interaction logic for LoadingWindow.xaml
    /// </summary>
    public partial class LoadingWindow : Window
    {
        private Func<Task<bool>> action;
        private CancellationTokenSource tokenSource;
        public bool Connected { get; set; }
        
        public LoadingWindow()
        {
            InitializeComponent();
        }

        public LoadingWindow(string message, string title,Func<Task<bool>> ac,CancellationTokenSource cts):this()
        {
            this.Title = title;
            this.TxtBlk.Text = message;
            tokenSource = cts;
            action = ac;
            this.Closing += (s, e) => { tokenSource.Cancel(); };
        }

        public async void ShowAndWait()
        {           
            Show();
            try
            {
                Connected = await action();
            }
            catch (Exception) { }
            if (Connected)
            {
                
                this.TxtBlk.Text = "Próba nawiązania połączenia przebiegła pomyślnie";
                this.Button.Content = "OK";
            }
            else
            {
                this.TxtBlk.Text = "Wystąpił błąd podczas próby nawiązania połączenia";
                this.Button.Content = "OK";
            }
            Indicator.IsActive = false;
            //     Close();
        }

        

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if(!Connected)
                tokenSource.Cancel();
            this.Close();
        }
    }
}
