using System;
using System.Globalization;
using System.Windows.Data;

namespace HausarbeitVirtuelleBörsenplattform.Converters
{
    /// <summary>
    /// Konvertiert einen Wert zu einem Boolean. True, wenn der Wert nicht null ist; sonst False.
    /// </summary>
    public class NullToBooleanConverter : IValueConverter
    {
        /// <summary>
        /// Konvertiert einen Wert zu einem booleschen Wert.
        /// </summary>
        /// <param name="value">Der zu konvertierende Wert</param>
        /// <param name="targetType">Der Zieltyp (wird ignoriert)</param>
        /// <param name="parameter">Ein optionaler Parameter (wird ignoriert)</param>
        /// <param name="culture">Die Kultur (wird ignoriert)</param>
        /// <returns>True, wenn der Wert nicht null ist; sonst False</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null;
        }

        /// <summary>
        /// Konvertiert einen booleschen Wert zurück (nicht implementiert).
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("RückKonvertierung nicht unterstützt.");
        }
    }
}