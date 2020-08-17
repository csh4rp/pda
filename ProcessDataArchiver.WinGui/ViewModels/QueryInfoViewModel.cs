using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ProcessDataArchiver.WinGui.ViewModels
{
    public class QueryInfoViewModel : INotifyPropertyChanged
    {
        private string columnName, aggregateType, sortType, andCriteria, orCriteria;
        private bool selected;

        public string ColumnName
        {
            get { return columnName; }
            set { columnName = value; OnPropertyChanged(); }
        }

        public string AggregateType
        {
            get { return aggregateType; }
            set { aggregateType = value; OnPropertyChanged(); }
        }

        public string SortType
        {
            get { return sortType; }
            set { sortType = value; OnPropertyChanged(); }
        }

        public string AndCriteria
        {
            get { return andCriteria; }
            set { andCriteria = value; OnPropertyChanged(); }
        }

        public string OrCriteria
        {
            get { return orCriteria; }
            set { orCriteria = value; OnPropertyChanged(); }
        }

        public bool Selected
        {
            get { return selected; }
            set { selected = value; OnPropertyChanged(); }
        }

        public QueryInfoViewModel()
        {
            
        }

        public event PropertyChangedEventHandler PropertyChanged;

       private void OnPropertyChanged([CallerMemberName] string property = "")
       {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
       }

    }
}
