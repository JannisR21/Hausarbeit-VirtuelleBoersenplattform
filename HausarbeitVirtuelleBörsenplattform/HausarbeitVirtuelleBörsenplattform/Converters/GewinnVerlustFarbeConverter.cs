using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace HausarbeitVirtuelleBörsenplattform.Converters
{
    /// <summary>
    /// Konvertiert einen Wert (Gewinn/Verlust) in eine entsprechende Farbe (Grün/Rot)
    /// </summary>
    public class GewinnVerlustFarbeConverter : IValueConverter
    {
        /// <summary>
        /// Konvertiert einen numerischen Wert in eine Farbe: Positiv = Grün, Negativ = Rot, Null = Grau
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return new SolidColorBrush(Colors.Gray);

            if (value is decimal decimalValue)
            {
                if (decimalValue > 0)
                    return new SolidColorBrush(Colors.Green);
                else if (decimalValue < 0)
                    return new SolidColorBrush(Colors.Red);
                else
                    return new SolidColorBrush(Colors.Gray);
            }
            else if (value is double doubleValue)
            {
                if (doubleValue > 0)
                    return new SolidColorBrush(Colors.Green);
                else if (doubleValue < 0)
                    return new SolidColorBrush(Colors.Red);
                else
                    return new SolidColorBrush(Colors.Gray);
            }
            else if (value is int intValue)
            {
                if (intValue > 0)
                    return new SolidColorBrush(Colors.Green);
                else if (intValue < 0)
                    return new SolidColorBrush(Colors.Red);
                else
                    return new SolidColorBrush(Colors.Gray);
            }
            else if (value is float floatValue)
            {
                if (floatValue > 0)
                    return new SolidColorBrush(Colors.Green);
                else if (floatValue < 0)
                    return new SolidColorBrush(Colors.Red);
                else
                    return new SolidColorBrush(Colors.Gray);
            }
            else if (value is string stringValue)
            {
                // Versuche, den String als Dezimalzahl zu parsen
                if (decimal.TryParse(stringValue.Replace("%", "").Replace("+", "").Trim(),
                    NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsedValue))
                {
                    if (parsedValue > 0)
                        return new SolidColorBrush(Colors.Green);
                    else if (parsedValue < 0)
                        return new SolidColorBrush(Colors.Red);
                }

                // Prüfe auf "+" oder "-" am Anfang
                if (stringValue.StartsWith("+"))
                    return new SolidColorBrush(Colors.Green);
                else if (stringValue.StartsWith("-"))
                    return new SolidColorBrush(Colors.Red);
            }

            // Standardwert, wenn nichts passt
            return new SolidColorBrush(Colors.Gray);
        }

        /// <summary>
        /// Die Rückkonvertierung wird nicht unterstützt
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("Die Rückkonvertierung von Farbe zu Gewinn/Verlust wird nicht unterstützt.");
        }
    }
}