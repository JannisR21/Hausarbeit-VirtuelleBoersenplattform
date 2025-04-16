using System;
using System.Globalization;
using System.Windows.Data;

namespace HausarbeitVirtuelleBörsenplattform.Helpers
{
    /// <summary>
    /// Konvertiert einen String-Vergleich in einen booleschen Wert und kehrt das Ergebnis um.
    /// Wird verwendet, um zu prüfen, ob ein Button deaktiviert werden soll, wenn die
    /// entsprechende Ansicht bereits aktiv ist.
    /// </summary>
    public class InverseViewMatchConverter : IValueConverter
    {
        /// <summary>
        /// Konvertiert einen String-Vergleich in einen booleschen Wert und kehrt das Ergebnis um.
        /// </summary>
        /// <param name="value">Der aktuelle View-Name</param>
        /// <param name="targetType">Der Zieltyp (wird ignoriert)</param>
        /// <param name="parameter">Der zu vergleichende View-Name</param>
        /// <param name="culture">Die Kultur-Info (wird ignoriert)</param>
        /// <returns>False, wenn die Werte übereinstimmen, sonst True</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return true;

            // Wenn der aktuelle View-Name dem Parameter entspricht, dann nicht aktiviert (false)
            // Ansonsten aktiviert (true)
            return !value.ToString().Equals(parameter.ToString());
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