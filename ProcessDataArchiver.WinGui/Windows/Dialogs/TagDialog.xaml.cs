using ProcessDataArchiver.DataCore.DbEntities;
using ProcessDataArchiver.DataCore.DbEntities.Tags;
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
using System.Collections;

namespace ProcessDataArchiver.WinGui.Windows.Dialogs
{
    /// <summary>
    /// Interaction logic for CreateTagWindow.xaml
    /// </summary>
    public partial class TagDialog : Window,INotifyPropertyChanged,INotifyDataErrorInfo
    {
        private ITag tag;
        private TagArchive _archive;
        private string _tagName , _comment, _eUnit, _gvName;
        private double _histeresis;
        private GlobalVariable _gv;
        private KeyValuePair<string, TimeSpan> selectedCycle;
        

        private bool _loaded;


        public bool Applied { get; set; }

        public string TagName
        {
            get { return _tagName; }
            set
            {
                _tagName = value;
                OnPropertyChanged();
            }
        }

        public string Comment
        {
            get { return _comment; }
            set
            {
                _comment = value;
                OnPropertyChanged();
            }
        }

        public string Histeresis
        {
            get { return _histeresis.ToString(); }
            set
            {
                double val;
                bool res = double.TryParse(value,out val);
                if (res) _histeresis = val;
                OnPropertyChanged();
            }
        }

        public string EUnit
        {
            get { return _eUnit; }
            set
            {
                _eUnit = value;
                OnPropertyChanged();
            }
        }

        public IDictionary<string,TimeSpan> CycleTimes { get; set; }

        public KeyValuePair<string,TimeSpan> SelectedCycle
        {
            get { return selectedCycle; }
            set
            {
                selectedCycle = value;
                OnPropertyChanged();
            }
        }

        public ITag NewTag { get; set; }
        public string GvName
        {
            get { return _gvName; }
            set
            {
                _gvName = value;
                OnPropertyChanged();
            }
        }

        public bool TypeChanged
        {
            get
            {
                return (tag.GlobalVariable.NetType == typeof(bool)) && (_gv.NetType != typeof(bool))
                    || (tag.GlobalVariable.NetType != typeof(bool)) && (_gv.NetType == typeof(bool));
            }
        }

        public bool HasErrors
        {
            get
            {
                return errors.Count() > 0;
            }
        }

        private Dictionary<string, IEnumerable<string>> errors =
            new Dictionary<string, IEnumerable<string>>();

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        public TagDialog()
        {
            InitializeComponent();
            _loaded = true;
            this.DataContext = this;
            SetInitialValues();
        }

        public TagDialog(ITag tag, TagArchive arch)
        {
            InitializeComponent();
            this.tag = tag;
            _archive = arch;
            _loaded = true;
            this.DataContext = this;
            SetInitialValues();
        }



        private void FinishButton_Click(object sender, RoutedEventArgs e)
        {
            var cycle = (KeyValuePair<string, TimeSpan>)CycleCb.SelectedItem;

            ArchivingType art;
            if ((ArchTypeCb.SelectedItem as ComboBoxItem).Name.Equals("Disabled"))
                art = ArchivingType.Disabled;
            else if ((ArchTypeCb.SelectedItem as ComboBoxItem).Name.Equals("Cyclic"))
                art = ArchivingType.Cyclic;
            else
                art = ArchivingType.Acyclic;

            if (_gv.NetType!= typeof(bool))
            {
                DeadbandType dbt;
                if (_histeresis > 0.0)
                {
                    if (PercentageRadio.IsChecked == true)
                        dbt = DeadbandType.Percentage;
                    else
                        dbt = DeadbandType.Absolute;
                }
                else
                    dbt = DeadbandType.None;


           
                    NewTag = new AnalogTag
                    {
                        Comment = this.Comment,
                        DeadbandType = dbt,
                        DeadbandValue = this._histeresis,
                        ArchivingType = art,
                        RefreshSpan = cycle.Value,
                        EuName = EUnit,
                        GlobalVariable = _gv,
                        GlobalVariableId = _gv.Id,
                        Name = this.TagName,
                        TagArchive = this._archive,
                        TagArchiveId = this._archive.Id,
                        LastChanged = DateTime.Now
                    };
                
            }
            else
            {


                NewTag = new DiscreteTag
                {
                    Comment = this.Comment,
                    ArchivingType = art,
                    RefreshSpan = cycle.Value,
                    GlobalVariable = _gv,
                    GlobalVariableId = _gv.Id,
                    Name = this.TagName,
                    TagArchive = this._archive,
                    TagArchiveId = this._archive.Id,
                    LastChanged = DateTime.Now
                };

            }

            Applied = true;
            this.Close();

        }

        private void ArchTypeCb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_loaded)
            {
                string name = (ArchTypeCb.SelectedItem as ComboBoxItem).Name;

                if (name.Equals("Disabled"))
                {
                    CycleTb.Foreground = Brushes.Gray;
                    CycleCb.IsEnabled = false;
                    CycleCb.Foreground = Brushes.Gray;
                    HisteresisTb.Foreground = Brushes.Gray;
                    HisteresisTextBox.IsEnabled = false;
                    UnitTb.Foreground = Brushes.Black;
                    UnitTextBox.IsEnabled = true;

                    PercentageRadio.IsEnabled = false;
                    AbsRadio.IsEnabled = false;
                }
                else if (name.Equals("Cyclic"))
                {
                    CycleTb.Foreground = Brushes.Black;
                    CycleCb.IsEnabled = true;
                    CycleCb.Foreground = Brushes.Black;
                    if (_gv != null && _gv.NetType == typeof(bool))
                    {
                        HisteresisTb.Foreground = Brushes.Gray;
                        HisteresisTextBox.IsEnabled = false;
                        UnitTb.Foreground = Brushes.Gray;
                        UnitTextBox.IsEnabled = false;

                        PercentageRadio.IsEnabled = false;
                        AbsRadio.IsEnabled = false;
                    }
                    else if (_gv != null && _gv.NetType != typeof(bool))
                    {
                        HisteresisTb.Foreground = Brushes.Gray;
                        HisteresisTextBox.IsEnabled = false;
                        UnitTb.Foreground = Brushes.Black;
                        UnitTextBox.IsEnabled = true;

                        PercentageRadio.IsEnabled = false;
                        AbsRadio.IsEnabled = false;
                    }
                }
                else if (name.Equals("Acyclic"))
                {
                    CycleTb.Foreground = Brushes.Black;
                    CycleCb.IsEnabled = true;
                    CycleCb.Foreground = Brushes.Black;
                    if (_gv != null && _gv.NetType == typeof(bool))
                    {
                        HisteresisTb.Foreground = Brushes.Gray;
                        HisteresisTextBox.IsEnabled = false;
                        UnitTb.Foreground = Brushes.Gray;
                        UnitTextBox.IsEnabled = false;

                        PercentageRadio.IsEnabled = false;
                        AbsRadio.IsEnabled = false;
                    }
                    else if (_gv != null && _gv.NetType != typeof(bool))
                    {
                        HisteresisTb.Foreground = Brushes.Black;
                        HisteresisTextBox.IsEnabled = true;
                        UnitTb.Foreground = Brushes.Black;
                        UnitTextBox.IsEnabled = true;

                        PercentageRadio.IsEnabled = true;
                        AbsRadio.IsEnabled = true;
                    }
                }
            }
        }

        private  void BrowseGvrs_Click(object sender, RoutedEventArgs e)
        {
            
            var gvdialog = new SelectGvDialog(false);
            gvdialog.ShowDialog();
            if (gvdialog.Applied)
            {
                _gv = gvdialog.SelectedGv;
                GvName = _gv.Name;
                if(_gv.NetType == typeof(bool))
                {
                    UnitTb.Foreground = Brushes.Gray;
                    UnitTextBox.IsEnabled = false;
                }
                else
                {
                    UnitTb.Foreground = Brushes.Black;
                    UnitTextBox.IsEnabled = true;
                }
            }
        }

        private void SetInitialValues()
        {
            CycleTimes = EntityContext.CycleTimes;
            
            CycleTb.Foreground = Brushes.Gray;
            CycleCb.IsEnabled = false;
            CycleCb.Foreground = Brushes.Gray;
            HisteresisTb.Foreground = Brushes.Gray;
            HisteresisTextBox.IsEnabled = false;
            UnitTb.Foreground = Brushes.Gray;
            UnitTextBox.IsEnabled = false;

            PercentageRadio.IsEnabled = false;
            AbsRadio.IsEnabled = false;

            if (tag != null)
            {
                TagName = tag.Name;
                this.Comment = tag.Comment;
                this.SelectedCycle = CycleTimes.Where(c => c.Value.Equals(tag.RefreshSpan))
                    .FirstOrDefault();

                _gv = tag.GlobalVariable;
                GvName = _gv.Name;
                this.Comment = tag.Comment;
                

                if (tag is AnalogTag)
                {
                    var atag = tag as AnalogTag;
                    this.Histeresis = atag.DeadbandValue.ToString();
                    this.EUnit = atag.EuName;
                }

                if (tag.ArchivingType == ArchivingType.Disabled)
                    ArchTypeCb.SelectedIndex = 0;
                else if (tag.ArchivingType == ArchivingType.Cyclic)
                    ArchTypeCb.SelectedIndex = 1;
                else
                    ArchTypeCb.SelectedIndex = 2;
                      
            }
            else
            {
                string name = "Znacznik";
                int l = 1;
                while (EntityContext.GetContext().Tags.Select(t => t.Name).Contains(name+l))
                {
                    l += 1;
                }
                TagName = name + l;

                FinishButton.IsEnabled = false;
                SelectedCycle = CycleTimes.FirstOrDefault();
            }

            errors.Clear();
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(TagName)));

        }


        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            if (tag!=null && tag.Name!=this.TagName && _archive.Tags.Select(t => t.Name).Contains(this.TagName) &&
                !errors.ContainsKey(name) && name.Equals("TagName"))
            {
                errors.Add(name, new[] { "Znacznik o podanej nazwie juz istnieje" });
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(name));
            }
            else if (tag!=null && errors.ContainsKey("TagName") && (tag.Name == this.TagName ||
                !_archive.Tags.Select(t => t.Name).Contains(this.TagName)))
            {
                errors.Remove(name);
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(name));
            }
            else if (tag == null)
            {
                var contains = EntityContext.GetContext().Tags.Select(t => t.Name).Contains(this.TagName);
                if (contains &&  !errors.ContainsKey(name))
                {
                    errors.Add(name, new[] { "Znacznik o podanej nazwie juz istnieje" });
                    ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(name));
                }
                else if(!contains && errors.ContainsKey(name))
                {
                    errors.Remove(name);
                    ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(name));
                }
            }

            if(this._gv == null && !errors.ContainsKey("GlobalVariable"))
            {
                errors.Add("GlobalVariable",new[] { "Nie wybrano zmiennej globalnej" });
            }
            else if (this._gv != null && errors.ContainsKey("GlobalVariable"))
            {
                errors.Remove("GlobalVariable");
            }

            DisableApply();

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            
        }

        public void DisableApply()
        {
            if (errors.Count > 0)
            {
                FinishButton.IsEnabled = false;
            }
            else
            {
                FinishButton.IsEnabled = true;
            }
        }


        public IEnumerable GetErrors(string propertyName)
        {
            if (errors.ContainsKey(propertyName))
                return errors[propertyName];
            else
                return null;
        }
    }
}
