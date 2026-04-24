using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using WarehouseApp.Models;

namespace WarehouseApp.Converters
{
    public class TypeToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MovementType type)
            {
                return type == MovementType.In ? Brushes.Green : Brushes.Red;
            }
            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}