using System;
using System.Globalization;
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
        /// Konvertiert zurück - implementiert das Gegenteil der Convert-Methode
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return true;
        }
    }
}