using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace OpenUtau.Converters {
    public class BoolToOpacityConverter : IValueConverter {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
            if (value is bool b) {
                return b ? 1.0 : 0.0;
            }
            return 0.0;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
            if (value is double d) {
                return d > 0.5;
            }
            return false;
        }
    }
}
