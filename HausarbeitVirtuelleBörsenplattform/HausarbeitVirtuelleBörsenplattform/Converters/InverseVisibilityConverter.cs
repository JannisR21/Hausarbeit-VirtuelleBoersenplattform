using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace HausarbeitVirtuelleBörsenplattform.Converters
{
    /// <summary>
    /// Konvertiert einen booleschen Wert in einen Visibility-Wert, 
    /// wobei true zu Collapsed und false zu Visible wird
    /// </summary>
    public class InverseVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Konvertiert einen booleschen Wert in einen Visibility-Wert
        /// true -> Collapsed, false -> Visible
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        /// <summary>
        /// Konvertiert zurück (nicht implementiert)
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility != Visibility.Visible;
            }
            return false;
        }
    }
}