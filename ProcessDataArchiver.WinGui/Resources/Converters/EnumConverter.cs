using ProcessDataArchiver.DataCore.DbEntities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace ProcessDataArchiver.WinGui.Resources.Converters
{
    public class EnumConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(targetType == typeof(string))
            {
                string v = value.ToString();
                switch (v)
                {
                    case "Analog":
                        return "Analogowy";
                    case "Discrete":
                        return "Dyskretny";
                    case "Cyclic":
                        return "Cykliczny";
                    case "Acyclic":
                        return "Acykliczny";
                    case "MAX":
                        return "Maksimum";
                    case "MIN":
                        return "Minimum";
                    case "SUM":
                        return "Suma";
                    case "AVG":
                        return "Średnia";
                    case "COUNT":
                        return "Zliczanie";
                    case "None":
                        return "Brak";
                    case "Snapshot":
                        return "Zapis wartości";
                    case "Email":
                        return "Email";
                    case "Sql":
                        return "Polecenie SQL";
                    case "Summary":
                        return "Zestawienie";
                    default:
                        break;
                }


            }
            else if(targetType == typeof(IEnumerable))
            {
                IEnumerable ien = (IEnumerable)value;
                var lista = new List<string>();
                foreach (var item in ien)
                {
                    if (item.Equals("MAX"))
                        lista.Add("Maksimum");
                    else if (item.Equals("MIN"))
                        lista.Add("Minimum");
                    else if (item.Equals("SUM"))
                        lista.Add("Suma");
                    else if (item.Equals("AVG"))
                        lista.Add("Średnia");
                    else if (item.Equals("COUNT"))
                        lista.Add("Zliczanie");
                    else
                        lista.Add(item.ToString());
                }
                return (IEnumerable)lista;
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
