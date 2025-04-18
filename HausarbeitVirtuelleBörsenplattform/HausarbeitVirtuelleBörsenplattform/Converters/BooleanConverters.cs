using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace HausarbeitVirtuelleBörsenplattform.Converters
{
    /// <summary>
    /// Konvertiert einen booleschen Wert in das Gegenteil
    /// </summary>
    public class BooleanToBooleanInverterConverter : IValueConverter
    {
        /// <summary>
        /// Konvertiert einen booleschen Wert in das Gegenteil
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return true;
        }

        /// <summary>
        /// Konvertiert zurück (nicht implementiert)
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Konvertiert einen booleschen Wert in einen Visibility-Wert
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Konvertiert einen booleschen Wert in einen Visibility-Wert
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        /// <summary>
        /// Konvertiert zurück (nicht implementiert)
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}