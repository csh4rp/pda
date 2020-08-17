using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Globalization;

namespace ProcessDataArchiver.WinGui.Resources.Converters
{
    public  class BackgroundConverter:IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ListViewItem item = value as ListViewItem;
            if (item != null)
            {
                ListView listView = ItemsControl.ItemsControlFromItemContainer(item) as ListView;
                int index = listView.ItemContainerGenerator.IndexFromContainer(item);

                if (index % 2 == 0)
                {
                    return System.Windows.Media.Brushes.AliceBlue;
                }
                else
                {
                    return System.Windows.Media.Brushes.White;
                }
            }
            else
            {
                var row = value as DataGridRow;
                if (row != null)
                {
                    DataGrid dg = ItemsControl.ItemsControlFromItemContainer(row) as DataGrid;
                    int index = dg.ItemContainerGenerator.IndexFromContainer(row);
                    if (index % 2 == 0)
                    {
                        return System.Windows.Media.Brushes.AliceBlue;
                    }
                    else
                    {
                        return System.Windows.Media.Brushes.White;
                    }
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
