using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace ProcessDataArchiver.WinGui.Resources.Converters
{
    public class TypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(targetType == typeof(string))
            {
                string v = value.ToString();
                
                switch (v)
                {
                    case "System.Boolean":
                        return "Logiczna";
                    case "System.SByte":
                        return "Liczba całkowita 8 bitowa ze znakiem";
                    case "System.Byte":
                        return "Liczba całkowita 8 bitowa bez znaku";
                    case "System.Int16":
                        return "Liczba całkowita 16 bitowa ze znakiem";
                    case "System.UInt16":
                        return "Liczba całkowita 16 bitowa bez znaku";
                    case "System.Int32":
                        return "Liczba całkowita 32 bitowa ze znakiem";
                    case "System.UInt32":
                        return "Liczba całkowita 32 bitowa bez znaku";
                    case "System.Int64":
                        return "Liczba całkowita 32 bitowa ze znakiem";
                    case "System.UInt64":
                        return "Liczba całkowita 32 bitowa bez znaku";
                    case "System.Single":
                        return "Liczba zmiennoprzecinkowa 32 bitowa";
                    case "System.Double":
                        return "Liczba zmiennoprzecinkowa 64 bitowa";
                    case "System.Decimal":
                        return "Liczba zmiennoprzecinkowa 128 bitowa";
                }
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
