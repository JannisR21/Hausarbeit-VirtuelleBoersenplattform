using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace HausarbeitVirtuelleBörsenplattform.Converters
{
    /// <summary>
    /// Konvertiert einen Gewinn/Verlust-Wert in eine entsprechende Farbe:
    /// - Positiv (Gewinn) = Grün
    /// - Negativ (Verlust) = Rot
    /// - Null = Schwarz
    /// </summary>
    public class GewinnVerlustFarbeConverter : IValueConverter
    {
        /// <summary>
        /// Konvertiert einen decimal-Wert in eine Farbe basierend auf dem Vorzeichen
        /// </summary>
        /// <param name="value">Der Gewinn/Verlust als decimal</param>
        /// <param name="targetType">Der Zieltyp (wird ignoriert)</param>
        /// <param name="parameter">Ein optionaler Parameter (wird ignoriert)</param>
        /// <param name="culture">Die Kultur-Info (wird ignoriert)</param>
        /// <returns>Eine Brush mit der entsprechenden Farbe</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal gewinnVerlust)
            {
                if (gewinnVerlust > 0)
                {
                    return new SolidColorBrush(Colors.Green);
                }
                else if (gewinnVerlust < 0)
                {
                    return new SolidColorBrush(Colors.Red);
                }
            }

            return new SolidColorBrush(Colors.Black);
        }

        /// <summary>
        /// Konvertiert von Brush zurück zu decimal (nicht implementiert)
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}