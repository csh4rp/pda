using ProcessDataArchiver.DataCore.DbEntities;
using ProcessDataArchiver.DataCore.DbEntities.Events;
using ProcessDataArchiver.WinGui.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Xml.Linq;
using System.Collections;
using ProcessDataArchiver.DataCore.DbEntities.Tags;
using ProcessDataArchiver.DataCore.Infrastructure;
using Outlook = Microsoft.Office.Interop.Outlook;

namespace ProcessDataArchiver.WinGui.Windows.Dialogs
{
    /// <summary>
    /// Interaction logic for CreateEventWindow.xaml
    /// </summary>
    public partial class EventDialog : Window,INotifyPropertyChanged,INotifyDataErrorInfo
    {
        private bool loaded,enabled,accountsLoaded,messageShown;
        private IEvent ev;
        private string evName, comment, agvName,dgvName,emailAdresses;
        private double trigValue;
        private int delay;
        private GlobalVariable agv,dgv;
        private KeyValuePair<string, TimeSpan> selectedCycle;
        private string selectedAccount;

        private IEnumerable<string> accountNames;

        public ObservableCollection<SummaryInfoViewModel> SummaryItems { get; set; }
            = new ObservableCollection<SummaryInfoViewModel>();

        public ObservableCollection<ITag> SnapshotItems { get; set; }
            = new ObservableCollection<ITag>();

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        public IDictionary<string, TimeSpan> CycleTimes { get; set; } = EntityContext.CycleTimes;
        public EventActionType EvActionType { get; set; } = EventActionType.None;
        
        public IEvent NewEvent { get; set; }

        public bool Applied { get; set; } = false;

        public string EventName
        {
            get { return evName; }
            set
            {
                evName = value;
                OnPropertyChanged();
                Validate(nameof(EventName));
            }
        }


        public string Comment
        {
            get { return comment; }
            set
            {
                comment = value;
                OnPropertyChanged();
            }
        }

        public string AgvName
        {
            get { return agvName; }
            set
            {
                agvName = value;
                OnPropertyChanged();
                Validate(nameof(AgvName));
            }
        }

        public string EventType { get; set; }
        public string DgvName
        {
            get { return dgvName; }
            set
            {
                dgvName = value;
                OnPropertyChanged();
                Validate(nameof(DgvName));
            }
        }

        public KeyValuePair<string,TimeSpan> SelectedCycle
        {
            get { return selectedCycle; }
            set
            {
                selectedCycle = value;
                OnPropertyChanged();
            }
        }

        public string TriggerValue
        {
            get { return trigValue.ToString(); }
            set
            {
                double val;
                bool res = double.TryParse(value, out val);
                if (res) trigValue = val;
                OnPropertyChanged();
            }
        }

        public bool Enabled
        {
            get { return enabled; }
            set
            {
                enabled = value;
                OnPropertyChanged();
            }
        }

        public int Delay
        {
            get { return delay; }
            set
            {
                delay = value;
                OnPropertyChanged();
                Validate(nameof(Delay));
            }
        }
        public bool TypeChanged
        {
            get
            {
                return (ev!=null && NewEvent!=null &&  ev.EventType != NewEvent.EventType);
            }
        }

        public string SelectedAccount
        {
            get
            {
                return selectedAccount;
            }
            set
            {
                selectedAccount = value;
                OnPropertyChanged();
            }
        }

        public string EmailAddresses
        {
            get { return emailAdresses; }
            set
            {
                emailAdresses = value;
                OnPropertyChanged();
                Validate(nameof(EmailAddresses));
            }
        }

        private Dictionary<string, IEnumerable<string>> errors =
            new Dictionary<string, IEnumerable<string>>();


        public bool HasErrors
        {
            get
            {
                return errors.Count > 0;
            }
        }



        public EventDialog()
        {
            InitializeComponent();
            loaded = true;
            this.DataContext = this;
            SetAccounts();
            SetCycleCbs();
            SetEnability();
            FillCbs();

            var cnx = EntityContext.GetContext();
            int index = 1;
            string name = "Zdarzenie"+index;
            
            while (cnx.Events.Select(e=>e.Name).Contains(name))
            {
                name = name.Replace(index.ToString(), (index + 1).ToString());
                index++; 
            }

            SummaryItems.CollectionChanged += SummaryItems_CollectionChanged;
            SnapshotItems.CollectionChanged += SnapshotItems_CollectionChanged;

            EventName = name;
            Enabled = true;
            ApplyButton.IsEnabled = false;
            
        }

        public EventDialog(IEvent e)
        {
            InitializeComponent();
            loaded = true;
            this.DataContext = this;

            ev = e;
            SetAccounts();
            SetInitialValues();
            SetCycleCbs();
            SetEnability();
            FillCbs();
            ApplyButton.IsEnabled = true;
        }

        private void SnapshotItems_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            //  if (SnapshotItems.Count > 0 && !HasErrors)
            //  {
            //      ApplyButton.IsEnabled = true;
            ////      errors.Add("Snapshot", new[] { "Empty" });
            //  }
            //  else
            //  {
            // //     errors.Remove("Snapshot");
            //      ApplyButton.IsEnabled = false;
            //  }
            Validate();
        }

        private void SummaryItems_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            var i = e.NewItems;

            Validate();
            //if (SummaryItems.Count > 0 && !HasErrors)
            //{
            //    ApplyButton.IsEnabled = true;
            ////    errors.Add("Sumamry", new[] { "Empty" });
            //}
            //else
            //{
            // //   errors.Remove("Summary");
            //    ApplyButton.IsEnabled = false;
            //}
        }




        private void SetAccounts()
        {
            Task.Run(() =>
            {
                var acc = EmailProvider.GetAccounts();
                int count = acc.Count;
                var list = new List<string>();
                for (int i = 1; i <= count; i++)
                {
                    list.Add(acc[i].DisplayName);
              //      list.Add("przykladowy@mail.com");//////////////////////////////////
                }
                accountNames = list;
                Dispatcher.Invoke(() =>
                {
                    FromMailCb.ItemsSource = accountNames;
                    FromMailCb.SelectedIndex = 0;
                    accountsLoaded = true;
                    var item = (ActionTypeCb.SelectedItem as ComboBoxItem).Content.ToString();
                    if (accountNames == null || accountNames.Count() == 0
                        && accountsLoaded && !messageShown && item.Equals("Email"))
                    {
                        MessageBox.Show("Program Outlook nie posiada skonfigurowanego konta email." +
                            " Aby móc wysyłać wiadomości email skonfiguruj konto", "Email",
                            MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                        messageShown = true;
                    }
                });
                
            });
        }

        private void SetInitialValues()
        {
            if (ev != null)
            {
                EventName= ev.Name;
                Comment= ev.Comment;
                Enabled = ev.Enabled;

                if(ev.EventActionType == EventActionType.None)
                {
                    ActionTypeCb.SelectedIndex = 0;
                }
                else if (ev.EventActionType == EventActionType.Snapshot)
                {
                    //////////////////
                    var tag1 = EntityContext.GetContext().Tags.FirstOrDefault();

                    ActionTypeCb.SelectedIndex = 1;

                    var root = XDocument.Parse(ev.ActionText);
                    var snap = root.Descendants("snapshot").FirstOrDefault();
                    var tags = snap.Descendants("tag");
                    foreach (var item in tags)
                    {
                        string tagName = item.Descendants("name").FirstOrDefault().Value;
                        string archive = item.Descendants("archivename").FirstOrDefault().Value;
                        var tag = EntityContext.GetContext().Tags.Where(t => t.Name.Equals(tagName) &&
                        t.TagArchive != null && t.TagArchive.Name.Equals(archive));
                        SnapshotItems.Add(tag.FirstOrDefault());
                    }

                }
                else if(ev.EventActionType == EventActionType.Email)
                {
                    ActionTypeCb.SelectedIndex = 2;

                    var root = XDocument.Parse(ev.ActionText);
                    var email = root.Descendants("email").FirstOrDefault();
                    string from = email.Descendants("from").FirstOrDefault().Value;
                    string addresses = email.Descendants("addresses").FirstOrDefault().Value;
                    string subject = email.Descendants("subject").FirstOrDefault().Value;
                    string body = email.Descendants("body").FirstOrDefault().Value;
                    /////////////////////////////
                    if (!accountNames.Contains(from))
                    {
                        SelectedAccount = accountNames.First();
                    }
                    SelectedAccount = from;
                    AddressesTextBox.Text = addresses;
                    SubjectTextBox.Text = subject;
                    BodyTextBox.Text = body;


                }
                else if (ev.EventActionType == EventActionType.Summary)
                {
                    ActionTypeCb.SelectedIndex = 3;

                    var doc = XDocument.Parse(ev.ActionText);
                    var root = doc.Descendants("summary").FirstOrDefault();
                    var tags = root.Descendants("tag");
                    foreach (var item in tags)
                    {
                        string tagName = item.Descendants("name").FirstOrDefault().Value;
                        string action = item.Descendants("action").FirstOrDefault().Value;
                        string timespan = item.Descendants("timespan").FirstOrDefault().Value;
                        string archive = item.Descendants("archivename").FirstOrDefault().Value;
                        TimeSpan ts = TimeSpan.FromMinutes(double.Parse(timespan));
                        SummaryItems.Add(new SummaryInfoViewModel
                        {
                            Action = action,
                            TagName = tagName,
                            TimeSpan = ts,
                            ArchiveName = archive
                        }
                        );
                    }

                    }
                else
                {
                    ActionTypeCb.SelectedIndex = 4;
                    SqlTextBox.Text = ev.ActionText;
                }


                if (ev is DiscreteEvent)
                {
                    var de = ev as DiscreteEvent;
                    TypeCb.SelectedIndex = 1;

                    if (de.EdgeType == EdgeType.Both)
                        EdgeTypeCb.SelectedIndex = 2;
                    else if (de.EdgeType == EdgeType.Raising)
                        EdgeTypeCb.SelectedIndex = 0;
                    else if (de.EdgeType == EdgeType.Falling)
                        EdgeTypeCb.SelectedIndex = 1;

                    dgv = de.GlobalVariable;
                    DgvName = dgv.Name;
                    Delay = de.Delay;
                    SelectedCycle = CycleTimes.Where(c => c.Value.Equals(de.RefreshSpan)).FirstOrDefault();

                }
                else if(ev is AnalogEvent)
                {
                    var ae = ev as AnalogEvent;
                    TypeCb.SelectedIndex = 0;

                    if (ae.EventTriggerType == EventTriggerType.Equals)
                        TriggerTypeCb.SelectedIndex = 0;
                    else if (ae.EventTriggerType == EventTriggerType.MoreThan)
                        TriggerTypeCb.SelectedIndex = 1;
                    else if (ae.EventTriggerType == EventTriggerType.LessThan)
                        TriggerTypeCb.SelectedIndex = 2;
                    else if (ae.EventTriggerType == EventTriggerType.NotEquals)
                        TriggerTypeCb.SelectedIndex = 3;
                    else if (ae.EventTriggerType == EventTriggerType.MoreOrEqual)
                        TriggerTypeCb.SelectedIndex = 4;
                    else if (ae.EventTriggerType == EventTriggerType.LessOrEqual)
                        TriggerTypeCb.SelectedIndex = 5;

                    agv = ae.GlobalVariable;
                    AgvName = agv.Name;
                    TriggerValue = ae.TriggerValue.ToString();
                    Delay = ae.Delay;
                    SelectedCycle = CycleTimes.Where(c => c.Value.Equals(ae.RefreshSpan)).FirstOrDefault();
                }
                else if(ev is CyclicEvent)
                {
                    var ce = ev as CyclicEvent;
                    TypeCb.SelectedIndex = 2;

                    if(ce.EventCycleType == EventCycleType.Hourly)
                    {
                        CycleTypeCb.SelectedIndex = 0;
                        MinuteCb.SelectedIndex = ce.CycleStamp.Minutes;
                    }
                    else if (ce.EventCycleType == EventCycleType.Daily)
                    {
                        CycleTypeCb.SelectedIndex = 1;
                        HourCb.SelectedIndex = ce.CycleStamp.Hours;
                        MinuteCb.SelectedIndex = ce.CycleStamp.Minutes;
                    }
                    else if (ce.EventCycleType == EventCycleType.Weekly)
                    {
                        CycleTypeCb.SelectedIndex = 2;
                        DayCb.SelectedIndex = ce.CycleStamp.Days;
                        HourCb.SelectedIndex = ce.CycleStamp.Hours;
                        MinuteCb.SelectedIndex = ce.CycleStamp.Minutes;
                    }
                    else if (ce.EventCycleType == EventCycleType.Monthly)
                    {
                        CycleTypeCb.SelectedIndex = 3;
                        DayCb.SelectedIndex = ce.CycleStamp.Days;
                        HourCb.SelectedIndex = ce.CycleStamp.Hours;
                        MinuteCb.SelectedIndex = ce.CycleStamp.Minutes;
                    }
                    else if (ce.EventCycleType == EventCycleType.Periodic)
                    {
                        CycleTypeCb.SelectedIndex = 4;
                        DayCb.SelectedIndex = ce.CycleStamp.Days;
                        HourCb.SelectedIndex = ce.CycleStamp.Hours;
                        MinuteCb.SelectedIndex = ce.CycleStamp.Minutes;
                    }
                }

            }
            SetEnability();
        }

        private void FillCbs()
        {
            var content = (CycleTypeCb.SelectedItem as ComboBoxItem).Content.ToString();
            if (!content.Equals("Okresowy"))
            {
                var hours = new int[24];
                for (int i = 0; i < 24; i++)
                {
                    hours[i] = i + 1;
                }
                var minutes = new int[60];
                for (int i = 0; i < 60; i++)
                {
                    minutes[i] = i;
                }
                HourCb.ItemsSource = hours;
                MinuteCb.ItemsSource = minutes;
            }
            else
            {
                var hours = new int[24];
                for (int i = 0; i < 24; i++)
                {
                    hours[i] = i ;
                }
                var minutes = new int[60];
                for (int i = 0; i < 60; i++)
                {
                    minutes[i] = i;
                }
                HourCb.ItemsSource = hours;
                MinuteCb.ItemsSource = minutes;
            }
        }

        private void SetDayCb()
        {

                var content = (CycleTypeCb.SelectedItem as ComboBoxItem).Content.ToString();
                if (content.Equals("Tygodniowy"))
                {
                    var days = new string[7];
                    days[0] = "Poniedziałek";
                    days[1] = "Wtorek";
                    days[2] = "Środa";
                    days[3] = "Czwartek";
                    days[4] = "Piątek";
                    days[5] = "Sobota";
                    days[6] = "Niedziela";
                    DayCb.ItemsSource = days;
                    DayCb.SelectedIndex = 0;
            }
                else if (content.Equals("Miesięczny"))
                {
                    var days = new int[31];
                    for (int i = 0; i < 31; i++)
                    {
                        days[i] = i + 1;
                    }
                    DayCb.ItemsSource = days;
                    DayCb.SelectedIndex = 0;

                }
                else if (content.Equals("Okresowy"))
                {
                var days = new int[31];
                for (int i = 0; i < 31; i++)
                {
                    days[i] = i;
                }
                DayCb.ItemsSource = days;
                DayCb.SelectedIndex = 0;
                }
                else
                {
                    var days = new int[365];
                    for (int i = 0; i < 365; i++)
                    {
                        days[i] = i+1;
                    }
                    DayCb.ItemsSource = days;
                    DayCb.SelectedIndex = 0;
            }
            
        }

        private void CycleTypeCb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (loaded)
            {
                SetCycleCbs();
                SetDayCb();
                FillCbs();
            }
        }

        private void SetEnability()
        {
            if (loaded)
            {
                var evType = (TypeCb.SelectedItem as ComboBoxItem).Content.ToString();

                if (evType.Equals("Analogowy"))
                {
                    CyclicGrid.Visibility = Visibility.Hidden;
                    AnalogGrid.Visibility = Visibility.Visible;
                    DiscreteGrid.Visibility = Visibility.Hidden;
                }
                else if (evType.Equals("Dyskretny"))
                {
                    CyclicGrid.Visibility = Visibility.Hidden;
                    AnalogGrid.Visibility = Visibility.Hidden;
                    DiscreteGrid.Visibility = Visibility.Visible;
                }
                else
                {
                    CyclicGrid.Visibility = Visibility.Visible;
                    AnalogGrid.Visibility = Visibility.Hidden;
                    DiscreteGrid.Visibility = Visibility.Hidden;

                }


                    
                
            }
        }

        private void SetCycleCbs()
        {

                var cType = (CycleTypeCb.SelectedItem as ComboBoxItem).Content.ToString();

                if (cType.Equals("Wyłączony"))
                {
                    DayTb.Text = "Dzien:";
                    HourTb.Text = "Godzina:";
                    MinuteTb.Text = "Minuta:";

                    DayTb.Foreground = Brushes.Gray;
                    DayCb.IsEnabled = false;
                    DayCb.Foreground = Brushes.Gray;
                    HourTb.Foreground = Brushes.Gray;
                    HourCb.IsEnabled = false;
                    HourCb.Foreground = Brushes.Gray;
                    MinuteTb.Foreground = Brushes.Gray;
                    MinuteCb.IsEnabled = false;
                    MinuteCb.Foreground = Brushes.Gray;
                }
                else if (cType.Equals("Godzinowy"))
                {
                    DayTb.Text = "Dzien:";
                    HourTb.Text = "Godzina:";
                    MinuteTb.Text = "Minuta:";

                    DayTb.Foreground = Brushes.Gray;
                    DayCb.IsEnabled = false;
                    DayCb.Foreground = Brushes.Gray;
                    HourTb.Foreground = Brushes.Gray;
                    HourCb.IsEnabled = false;
                    HourCb.Foreground = Brushes.Gray;
                    MinuteTb.Foreground = Brushes.Black;
                    MinuteCb.IsEnabled = true;
                    MinuteCb.Foreground = Brushes.Black;
                }
                else if (cType.Equals("Dzienny"))
                {
                    DayTb.Text = "Dzien:";
                    HourTb.Text = "Godzina:";
                    MinuteTb.Text = "Minuta:";
                    DayTb.Foreground = Brushes.Gray;
                    DayCb.IsEnabled = false;
                    DayCb.Foreground = Brushes.Gray;
                    HourTb.Foreground = Brushes.Black;
                    HourCb.IsEnabled = true;
                    HourCb.Foreground = Brushes.Black;
                    MinuteTb.Foreground = Brushes.Black;
                    MinuteCb.IsEnabled = true;
                    MinuteCb.Foreground = Brushes.Black;
                }
                else if (cType.Equals("Tygodniowy") || cType.Equals("Miesięczny"))
                {

                    DayTb.Text = "Dzien:";
                    HourTb.Text = "Godzina:";
                    MinuteTb.Text = "Minuta:";

                    DayTb.Foreground = Brushes.Black;
                    DayCb.IsEnabled = true;
                    DayCb.Foreground = Brushes.Black;
                    HourTb.Foreground = Brushes.Black;
                    HourCb.IsEnabled = true;
                    HourCb.Foreground = Brushes.Black;
                    MinuteTb.Foreground = Brushes.Black;
                    MinuteCb.IsEnabled = true;
                    MinuteCb.Foreground = Brushes.Black;
                }
                else
                {
                    DayTb.Text = "Dni:";
                    HourTb.Text = "Godziny:";
                    MinuteTb.Text = "Minuty:";
                    DayTb.Foreground = Brushes.Black;
                    DayCb.IsEnabled = true;
                    DayCb.Foreground = Brushes.Black;
                    HourTb.Foreground = Brushes.Black;
                    HourCb.IsEnabled = true;
                    HourCb.Foreground = Brushes.Black;
                    MinuteTb.Foreground = Brushes.Black;
                    MinuteCb.IsEnabled = true;
                    MinuteCb.Foreground = Brushes.Black;
                }
            
            
        }

        private void SetActionCb()
        {
            var item = (ActionTypeCb.SelectedItem as ComboBoxItem).Content.ToString();

            if (item.Equals("Brak akcji"))
            {
                ActionGroupBox.Visibility = Visibility.Hidden;

                SqlGrid.Visibility = Visibility.Hidden;
                SnapshotGrid.Visibility = Visibility.Hidden;
                EmailGrid.Visibility = Visibility.Hidden;
                SummaryGrid.Visibility = Visibility.Hidden;
                if (!HasErrors)
                {
                    ApplyButton.IsEnabled = true;
                }
            }
            else if (item.Equals("Zapis wartości w bazie"))
            {
                ActionGroupBox.Visibility = Visibility.Visible;

                SqlGrid.Visibility = Visibility.Hidden;
                SnapshotGrid.Visibility = Visibility.Visible;
                EmailGrid.Visibility = Visibility.Hidden;
                SummaryGrid.Visibility = Visibility.Hidden;
                if (SummaryItems.Count > 0 && !HasErrors)
                {
                    ApplyButton.IsEnabled = true;
                }
                else
                {
                    ApplyButton.IsEnabled = false;
                }
            }
            else if (item.Equals("Email"))
            {
                if(accountNames == null || accountNames.Count() == 0 
                    && accountsLoaded && !messageShown)
                {
                    MessageBox.Show("Program Outlook nie posiada skonfigurowanego konta email." +
                        " Aby móc wysyłać wiadomości email skonfiguruj konto", "Email",
                        MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                    messageShown = true;
                }


                ActionGroupBox.Visibility = Visibility.Visible;
                SqlGrid.Visibility = Visibility.Hidden;
                SnapshotGrid.Visibility = Visibility.Hidden;
                EmailGrid.Visibility = Visibility.Visible;
                SummaryGrid.Visibility = Visibility.Hidden;

                if(!string.IsNullOrEmpty(SelectedAccount) && !string.IsNullOrEmpty(SubjectTextBox.Text) 
                    &&!string.IsNullOrEmpty(AddressesTextBox.Text) && string.IsNullOrEmpty(BodyTextBox.Text)
                    && !HasErrors)
                {
                    ApplyButton.IsEnabled = true;
                }
                else
                {
                    ApplyButton.IsEnabled = false;
                }
            }
            else if (item.Equals("Zestawienie"))
            {
                ActionGroupBox.Visibility = Visibility.Visible;

                SqlGrid.Visibility = Visibility.Hidden;
                SnapshotGrid.Visibility = Visibility.Hidden;
                EmailGrid.Visibility = Visibility.Hidden;
                SummaryGrid.Visibility = Visibility.Visible;

                if (SummaryItems.Count>0 && !HasErrors)
                {
                    ApplyButton.IsEnabled = true;
                }
                else
                {
                    ApplyButton.IsEnabled = false;
                }
            }
            else if (item.Equals("Polecenie SQL"))
            {
                ActionGroupBox.Visibility = Visibility.Visible;

                SqlGrid.Visibility = Visibility.Visible;
                SnapshotGrid.Visibility = Visibility.Hidden;
                EmailGrid.Visibility = Visibility.Hidden;
                SummaryGrid.Visibility = Visibility.Hidden;

                if(!string.IsNullOrEmpty(SqlTextBox.Text) && !HasErrors)
                {
                    ApplyButton.IsEnabled = true;
                }
                else
                {
                    ApplyButton.IsEnabled = false;
                }
            }
        }


        private void TypeCb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (loaded)
            {
                SetEnability();
                var sel = (TypeCb.SelectedItem as ComboBoxItem).Content.ToString();

                if (sel.Equals("Analogowy"))
                {
                    if (string.IsNullOrEmpty(AgvName))
                    {
                        ApplyButton.IsEnabled = false;
                    }
                    else if(!HasErrors)
                    {
                        ApplyButton.IsEnabled = true;
                    }
                }
                else if (sel.Equals("Dyskretny"))
                {
                    if (string.IsNullOrEmpty(DgvName))
                    {
                        ApplyButton.IsEnabled = false;
                    }
                    else if (!HasErrors)
                    {
                        ApplyButton.IsEnabled = true;
                    }
                }
                else if (!HasErrors)
                {
                    ApplyButton.IsEnabled = true;
                }
            }
        }


        private void ActionTypeCb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (loaded)
            {
                var type = (ActionTypeCb.SelectedItem as ComboBoxItem).Content.ToString();
                if (type.Equals("Brak akcji"))
                    EvActionType = EventActionType.None;
                else if (type.Equals("Zapis wartości w bazie"))
                    EvActionType = EventActionType.Snapshot;
                else if (type.Equals("Emaile"))
                    EvActionType = EventActionType.Email;
                else if (type.Equals("Zestawienie"))
                    EvActionType = EventActionType.Summary;
                else if (type.Equals("Polecenie SQL"))
                    EvActionType = EventActionType.Sql;
                SetActionCb();
            }
        }

        private void AddSummaryButton_Click(object sender, RoutedEventArgs e)
        {
            SelectSummaryDialog ss = new SelectSummaryDialog();
            ss.ShowDialog();
            if (ss.Applied)
            {
                foreach (var item in ss.TagSummaryInfos)
                {
                    SummaryItems.Add(item);
                }
            }
        }

        private void RemoveSummaryButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = SummaryListView.SelectedItems;
            if (selected != null)
            {
                var list = new List<SummaryInfoViewModel>();
                foreach (var item in selected)
                {
                    list.Add(item as SummaryInfoViewModel);
                }
                foreach (var item in list)
                {
                    SummaryItems.Remove(item);
                }
                RemoveSummaryButton.IsEnabled = false;
            }
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            var evType = (TypeCb.SelectedItem as ComboBoxItem).Content.ToString();
            var acType = (ActionTypeCb.SelectedItem as ComboBoxItem).Content.ToString();

            if (evType.Equals("Analogowy"))
            {
                var cycle =((KeyValuePair<string,TimeSpan>)AnCycleCb.SelectedItem).Value;

                NewEvent = new AnalogEvent
                {
                    Comment = CommentTextBox.Text,
                    Name = NameTextBox.Text,
                    GlobalVariable = agv,
                    GlobalVariableId = agv.Id,
                    Enabled = (bool)EnabledCheckBox.IsChecked,
                    RefreshSpan = cycle,
                    TriggerValue = trigValue,
                    EventTriggerType = GetTriggerType(),
                    LastChanged = DateTime.Now,
                    EventActionType = EvActionType                 
                };
                


            }
            else if (evType.Equals("Dyskretny"))
            {
                var cycle = ((KeyValuePair<string, TimeSpan>)AnCycleCb.SelectedItem).Value;
                var edgeType = (EdgeTypeCb.SelectedItem as ComboBoxItem).Content.ToString();

                NewEvent = new DiscreteEvent
                {
                    Comment = CommentTextBox.Text,
                    Name = NameTextBox.Text,
                    GlobalVariable = dgv,
                    GlobalVariableId = dgv.Id,
                    Enabled = (bool)EnabledCheckBox.IsChecked,
                    RefreshSpan = cycle,
                    LastChanged = DateTime.Now,
                    EventActionType = EvActionType
                };

                if (edgeType.Equals("Narastające"))
                {
                    (NewEvent as DiscreteEvent).EdgeType = EdgeType.Raising;
                }
                else if (edgeType.Equals("Opdające"))
                {
                    (NewEvent as DiscreteEvent).EdgeType = EdgeType.Falling;
                }
                else if (edgeType.Equals("Wszystkie"))
                {
                    (NewEvent as DiscreteEvent).EdgeType = EdgeType.Both;
                }

            }
            else if (evType.Equals("Cykliczny"))
            {

                var cycle = GetCyclicSpan();

                NewEvent = new CyclicEvent
                {
                    Comment = CommentTextBox.Text,
                    Name = NameTextBox.Text,
                    Enabled = (bool)EnabledCheckBox.IsChecked,
                    CycleStamp = cycle.Item2,
                    EventCycleType = cycle.Item1,
                    LastChanged = DateTime.Now,
                    EventActionType = EvActionType
                };
            }

            CreateAction();
            Applied = true;
            this.Close();
        }

        private void AddTagSnapButton_Click(object sender, RoutedEventArgs e)
        {
            var st = new SelectTagDialog();
            st.ShowDialog();
            if (st.Applied)
            {
                var tags = st.SelectedTags;
                foreach (var gv in tags)
                {
                    SnapshotItems.Add(gv);
                }
            }
        }

        private void RemoveTagSnapButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = SnapshotListView.SelectedItems;
            if (selected != null)
            {
                var list = new List<ITag>();
                foreach (var item in selected)
                {
                    list.Add(item as ITag);
                }
                foreach (var tag in list)
                {
                    SnapshotItems.Remove(tag);
                }
                RemoveTagSnapButton.IsEnabled = false;
            }
        }

        private void GvBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            SelectGvDialog sgv = null;
            var item = (TypeCb.SelectedItem as ComboBoxItem).Content.ToString();
            if (item.Equals("Analogowy"))
            {
                sgv = new SelectGvDialog(false,false);
                sgv.ShowDialog();
                if (sgv.Applied)
                {
                    agv = sgv.SelectedGv;
                    AgvName = agv.Name;
                }
            }
            else if (item.Equals("Dyskretny"))
            {
                sgv = new SelectGvDialog(false,true);
                sgv.ShowDialog();
                if (sgv.Applied)
                {
                    dgv = sgv.SelectedGv;
                    DgvName = dgv.Name;
                }
            }


        }

        private EventTriggerType GetTriggerType()
        {
            var item = (TriggerTypeCb.SelectedItem as ComboBoxItem).Content.ToString();

            if (item.Equals("="))
                return EventTriggerType.Equals;
            else if (item.Equals(">="))
                return EventTriggerType.MoreOrEqual;
            else if (item.Equals(">"))
                return EventTriggerType.MoreThan;
            else if (item.Equals("<="))
                return EventTriggerType.LessOrEqual;
            else if (item.Equals("<"))
                return EventTriggerType.LessThan;
            else if (item.Equals("<>"))
                return EventTriggerType.NotEquals;

            throw new ArgumentException("Wrong Enum type!");
        }



        private void SummaryListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var sel = SummaryListView.SelectedItem;
            if(sel!=null && !RemoveSummaryButton.IsEnabled)
            {
                RemoveSummaryButton.IsEnabled = true;
            }
        }

        private void SnapshotListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var sel = SnapshotListView.SelectedItem;
            if(sel!=null && !RemoveTagSnapButton.IsEnabled)
            {
                RemoveTagSnapButton.IsEnabled = true;
            }
        }

        private void FromMailCb_DropDownOpened(object sender, EventArgs e)
        {
      //      SetAccounts();
        }



        private void SubjectTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CheckEmail();
        }

        private void SqlTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
           // string txtx = SqlTextBox.Text;

            //if (!string.IsNullOrEmpty(txtx))
            //{
            //    ApplyButton.IsEnabled = false;
            //}
            //else if (!HasErrors)
            //{
            //    ApplyButton.IsEnabled = true;
            //}
            Validate();
        }

        private void AddressesTextBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            CheckEmail();
        }

        private void HourCb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void Cb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (loaded)
            {
                var type = (CycleTypeCb.SelectedItem as ComboBoxItem).Content.ToString();

                if (type == "Okresowy")
                {
                    int minute=0, hour=0, day=0;
                    if(MinuteCb.SelectedItem!=null)
                        minute = (int)MinuteCb.SelectedItem;
                    if (HourCb.SelectedItem != null)
                        hour = (int)HourCb.SelectedItem;
                    if (DayCb.SelectedItem != null)
                        day = (int)DayCb.SelectedItem;
                    if (minute == 0 && hour == 0 && day == 0)
                        ApplyButton.IsEnabled = false;
                    else
                        Validate();
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BodyTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CheckEmail();
        }

        private void CheckEmail()
        {
            if (!string.IsNullOrEmpty(SelectedAccount) && !string.IsNullOrEmpty(SubjectTextBox.Text)
                && !string.IsNullOrEmpty(AddressesTextBox.Text) && string.IsNullOrEmpty(BodyTextBox.Text)
                && !HasErrors)
            {
                bool valid = false;
                try
                {
                    string[] addresses = AddressesTextBox.Text.Split(';');
                    foreach (var item in addresses)
                    {
                        var addr = new System.Net.Mail.MailAddress(AddressesTextBox.Text);
                    }
                    
                    valid = true;
                }
                catch
                {
                    valid = false;
                }

                if(valid)
                    ApplyButton.IsEnabled = true;
            }
            else
            {
                ApplyButton.IsEnabled = false;
            }
        }


        private Tuple<EventCycleType,TimeSpan> GetCyclicSpan()
        {
            var item = (CycleTypeCb.SelectedItem as ComboBoxItem).Content.ToString();

            if (item.Equals("Godzinowy"))
            {
                int minute = (int)MinuteCb.SelectedItem;
                return Tuple.Create(EventCycleType.Hourly,
                    new TimeSpan(0,minute,0));
            }
            else if (item.Equals("Dzienny"))
            {
                int minute = (int)MinuteCb.SelectedItem;
                int hour = (int)HourCb.SelectedItem;
                return Tuple.Create(EventCycleType.Daily,
                   new TimeSpan(hour,minute,0));
            }
            else if (item.Equals("Tygodniowy"))
            {
                int day = DayCb.SelectedIndex;
                int minute = (int)MinuteCb.SelectedItem;
                int hour = (int)HourCb.SelectedItem;
                return Tuple.Create(EventCycleType.Weekly,
                    new TimeSpan(day,hour,minute,0));
            }
            else if (item.Equals("Miesięczny"))
            {
                int day = (int)DayCb.SelectedItem;
                int minute = (int)MinuteCb.SelectedItem;
                int hour = (int)HourCb.SelectedItem;
                return Tuple.Create(EventCycleType.Monthly,
                    new TimeSpan(day, hour, minute, 0));
            }
            else if (item.Equals("Okresowy"))
            {
                int day = (int)DayCb.SelectedItem;
                int minute = (int)MinuteCb.SelectedItem;
                int hour = (int)HourCb.SelectedItem;
                return Tuple.Create(EventCycleType.Periodic,
                     new TimeSpan(day, hour, minute, 0));
            }

            throw new ArgumentException("Unsupported data type");
        }


        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
           // Validate(name);
        }

        private void Validate(string propName)
        {
            switch (propName)
            {
                case "EventName":
                    if(ev!=null && ev.Name != this.EventName && EntityContext.GetContext().Events
                        .Select(t => t.Name).Contains(this.EventName) &&
                        !errors.ContainsKey(propName))
                    {
                        errors.Add(propName, new[] { "Zdarzenie o podanej nazwie juz istnieje" });
                        
                    }
                    else if (ev != null && errors.ContainsKey(propName) && (ev.Name == this.EventName ||
                    !EntityContext.GetContext().Events.Select(t => t.Name).Contains(this.EventName)))
                    {
                        errors.Remove(propName);
                    }
                    else if (ev == null)
                    {
                        var contains = EntityContext.GetContext().Events.Select(t => t.Name).Contains(this.EventName);
                        if (contains && !errors.ContainsKey(propName))
                        {
                            errors.Add(propName, new[] { "Zdarzenie o podanej nazwie juz istnieje" });
                            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propName));
                        }
                        else if (!contains && errors.ContainsKey(propName))
                        {
                            errors.Remove(propName);
                            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propName));
                        }
                    }

                    if (string.IsNullOrEmpty(this.EventName) && !errors.ContainsKey(propName))
                    {
                        errors.Add(propName, new[] { "Nazwa zdarzenia nie może być pusta" });
                    }
                    else if (!string.IsNullOrEmpty(this.EventName) && errors.ContainsKey(propName))
                    {
                        errors.Remove(propName);
                    }
                    break;

                case "AgvName":
                    EventType = (TypeCb.SelectedItem as ComboBoxItem).Content.ToString();
                    if (EventType.Equals("Analogowy"))
                    {
                        if (string.IsNullOrEmpty(AgvName))
                        {
                            if (!errors.ContainsKey(propName))
                                errors.Add(propName, new[] { "Nie wybrano zmiennej globalnej" });
                        }
                        else
                        {
                            if (errors.ContainsKey(propName))
                                errors.Remove(propName);
                        }
                    }

                    break;

                case "DgvName":
                   EventType = (TypeCb.SelectedItem as ComboBoxItem).Content.ToString();
                    if (EventType.Equals("Dyskretny"))
                    {
                        if (string.IsNullOrEmpty(DgvName))
                        {
                            if(!errors.ContainsKey(propName))
                                errors.Add(propName, new[] { "Nie wybrano zmiennej globalnej" });
                        }
                        else
                        {
                            if(errors.ContainsKey(propName))
                                errors.Remove(propName);
                        }
                    }
                    break;

                case "Delay":

                    if (Delay<0 && !errors.ContainsKey(propName))
                    {
                            errors.Add(propName,new[] { "Wartość opóźnienia nie może być ujemna" });
                    }
                    else if(errors.ContainsKey(propName))
                    {
                        errors.Remove(propName);
                    }
                    break;

                case "EmailAddresses":
                    bool valid = false;
                    var ci = (ActionTypeCb.SelectedItem as ComboBoxItem).Content.ToString();
                    try
                    {
                        string[] addresses = AddressesTextBox.Text.Split(';');
                        foreach (var item in addresses)
                        {
                            var addr = new System.Net.Mail.MailAddress(AddressesTextBox.Text);
                        }

                        valid = true;
                    }
                    catch
                    {
                        valid = false;
                    }
                    if(!valid && !errors.ContainsKey(propName) && ci.Equals("Email"))
                    {
                        errors.Add(propName, new[] { "Podane adresy email nie są prawidłowe" });
                    }
                    else if (valid && errors.ContainsKey(propName) && ci.Equals("Email"))
                    {
                        errors.Remove(propName);
                    }
                    break;


            }

            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propName));
            if (HasErrors || (ev!=null && NewEvent==null))
                ApplyButton.IsEnabled = false;
            else
                ApplyButton.IsEnabled = true;
        }


        private void CreateAction()
        {
            string acTyp = (ActionTypeCb.SelectedItem as ComboBoxItem).Content.ToString();

            if (acTyp.Equals("Zapis wartości w bazie"))
            {
                var root = new XElement("snapshot");
                foreach (var t in SnapshotItems)
                {
                    var tag = new XElement("tag");
                    tag.Add(new XElement("archivename", t.TagArchive.Name));
                    tag.Add(new XElement("name", t.Name));
                    root.Add(tag);
                }
                NewEvent.ActionText = root.ToString();
                NewEvent.EventActionType = EventActionType.Snapshot;
                NewEvent.ActionToInvoke = EventActionFactory.CreateAction(NewEvent);
            }
            else if (acTyp.Equals("Email"))
            {
                var root = new XElement("email");
                var from = new XElement("from", SelectedAccount);
                var addresses = new XElement("addresses", AddressesTextBox.Text);
                var subject = new XElement("subject", SubjectTextBox.Text);
                var body = new XElement("body", BodyTextBox.Text);

                root.Add(from);
                root.Add(addresses);
                root.Add(subject);
                root.Add(body);

                NewEvent.ActionText = root.ToString();
                NewEvent.EventActionType = EventActionType.Email;
                NewEvent.ActionToInvoke = EventActionFactory.CreateAction(NewEvent);
            }
            else if (acTyp.Equals("Zestawienie"))
            {
                var root = new XElement("summary");
                foreach (var item in SummaryItems)
                { 
                    var tag = new XElement("tag");
                    tag.Add(new XElement("archivename", item.ArchiveName));
                    tag.Add(new XElement("name",item.TagName));
                    tag.Add(new XElement("action", item.Action));
                    tag.Add(new XElement("timespan", item.TimeSpan.TotalMinutes));
                    root.Add(tag);
                }
                NewEvent.ActionText = root.ToString();
                NewEvent.EventActionType = EventActionType.Summary;
                NewEvent.ActionToInvoke = EventActionFactory.CreateAction(NewEvent);
            }
            else if(acTyp.Equals("Polecenie SQL"))
            {
                NewEvent.ActionText = SqlTextBox.Text;
                NewEvent.EventActionType = EventActionType.Sql;
                NewEvent.ActionToInvoke = EventActionFactory.CreateAction(NewEvent);
            }
        }

        private void Validate()
        {
            Validate(nameof(AgvName));
            Validate(nameof(DgvName));
            Validate(nameof(EventName));
            Validate(nameof(EmailAddresses));
            Validate(nameof(Delay));

            var sel = (TypeCb.SelectedItem as ComboBoxItem).Content.ToString();
            bool gvValid = false;
            if (sel.Equals("Analogowy"))
            {
                if (string.IsNullOrEmpty(AgvName))
                {
                    gvValid = false;
                }
                else if (!HasErrors)
                {
                    gvValid = true;
                }
            }
            else if (sel.Equals("Dyskretny"))
            {
                if (string.IsNullOrEmpty(DgvName))
                {
                    gvValid = false;
                }
                else if (!HasErrors)
                {
                    gvValid = true;
                }
            }
            else if (!HasErrors)
            {
                gvValid = true;
            }



            var item = (ActionTypeCb.SelectedItem as ComboBoxItem).Content.ToString();
            
            if (item.Equals("Brak akcji"))
            {

                if (!HasErrors &&gvValid)
                {
                    ApplyButton.IsEnabled = true;
                }
            }
            else if (item.Equals("Zapis wartości w bazie"))
            {
                if (SnapshotItems.Count > 0 && !HasErrors &&gvValid)
                {
                    ApplyButton.IsEnabled = true;
                }
                else
                {
                    ApplyButton.IsEnabled = false;
                }
            }
            else if (item.Equals("Email"))
            {
                //if (accountNames == null || accountNames.Count() == 0
                //    && accountsLoaded && !messageShown)
                //{
                //    MessageBox.Show("Program Outlook nie posiada skonfigurowanego konta email." +
                //        " Aby móc wysyłać wiadomości email skonfiguruj konto", "Email",
                //        MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                //    messageShown = true;
                //}


                //ActionGroupBox.Visibility = Visibility.Visible;
                //SqlGrid.Visibility = Visibility.Hidden;
                //SnapshotGrid.Visibility = Visibility.Hidden;
                //EmailGrid.Visibility = Visibility.Visible;
                //SummaryGrid.Visibility = Visibility.Hidden;

                if (!string.IsNullOrEmpty(SelectedAccount) && !string.IsNullOrEmpty(SubjectTextBox.Text)
                    && !string.IsNullOrEmpty(AddressesTextBox.Text) && string.IsNullOrEmpty(BodyTextBox.Text)
                    && !HasErrors &&gvValid)
                {
                    ApplyButton.IsEnabled = true;
                }
                else
                {
                    ApplyButton.IsEnabled = false;
                }
            }
            else if (item.Equals("Zestawienie"))
            {
                //ActionGroupBox.Visibility = Visibility.Visible;

                //SqlGrid.Visibility = Visibility.Hidden;
                //SnapshotGrid.Visibility = Visibility.Hidden;
                //EmailGrid.Visibility = Visibility.Hidden;
                //SummaryGrid.Visibility = Visibility.Visible;

                if (SummaryItems.Count > 0 && !HasErrors &&gvValid)
                {
                    ApplyButton.IsEnabled = true;
                }
                else
                {
                    ApplyButton.IsEnabled = false;
                }
            }
            else if (item.Equals("Polecenie SQL"))
            {
                //ActionGroupBox.Visibility = Visibility.Visible;

                //SqlGrid.Visibility = Visibility.Visible;
                //SnapshotGrid.Visibility = Visibility.Hidden;
                //EmailGrid.Visibility = Visibility.Hidden;
                //SummaryGrid.Visibility = Visibility.Hidden;

                if (!string.IsNullOrEmpty(SqlTextBox.Text) && !HasErrors &&gvValid)
                {
                    ApplyButton.IsEnabled = true;
                }
                else
                {
                    ApplyButton.IsEnabled = false;
                }
            }
        }


        public IEnumerable GetErrors(string propertyName)
        {
            if (errors.ContainsKey(propertyName))
                return errors[propertyName];
            return null;
        }
    }
}
