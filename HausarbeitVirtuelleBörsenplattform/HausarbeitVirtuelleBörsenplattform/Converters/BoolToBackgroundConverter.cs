using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace HausarbeitVirtuelleBörsenplattform.Converters
{
    /// <summary>
    /// Konvertiert einen booleschen Wert in eine Hintergrundfarbe, abhängig vom Parameter
    /// </summary>
    public class BoolToBackgroundConverter : IValueConverter
    {
        /// <summary>
        /// Konvertiert einen booleschen Wert in eine Hintergrundfarbe für die Kauf/Verkauf-Buttons
        /// </summary>
        /// <param name="value">Der boolesche Wert (IsKauf)</param>
        /// <param name="targetType">Der Zieltyp (wird ignoriert)</param>
        /// <param name="parameter">Ein optionaler Parameter, der angibt, ob es sich um den Kauf- oder Verkauf-Button handelt</param>
        /// <param name="culture">Die Kultur-Info (wird ignoriert)</param>
        /// <returns>Eine SolidColorBrush mit der entsprechenden Farbe</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is bool isKauf))
                return new SolidColorBrush(Colors.Transparent);

            // Parameter auswerten (true = Kauf-Button, false = Verkauf-Button)
            bool isKaufButton = true;
            if (parameter is string paramString)
            {
                bool.TryParse(paramString, out isKaufButton);
            }
            else if (parameter is bool paramBool)
            {
                isKaufButton = paramBool;
            }

            // Wenn Button und Status übereinstimmen (z.B. Kauf-Button und IsKauf=true)
            bool isSelected = (isKauf && isKaufButton) || (!isKauf && !isKaufButton);

            // Farben basierend auf Auswahl
            return isSelected
                ? new SolidColorBrush(Color.FromRgb(0xF1, 0xF8, 0xE9)) // Leichtes Grün für ausgewählt
                : new SolidColorBrush(Colors.Transparent);             // Transparent für nicht ausgewählt
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