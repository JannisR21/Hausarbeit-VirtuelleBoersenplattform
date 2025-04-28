using System;
using System.Globalization;
using System.Windows.Data;

namespace HausarbeitVirtuelleBörsenplattform.Converters
{
    /// <summary>
    /// Konvertiert einen Enum-Wert in einen Boolean-Wert und umgekehrt
    /// </summary>
    public class EnumToBooleanConverter : IValueConverter
    {
        /// <summary>
        /// Konvertiert einen Enum-Wert zu einem Boolean
        /// </summary>
        /// <param name="value">Der zu konvertierende Wert</param>
        /// <param name="targetType">Der Zieltyp</param>
        /// <param name="parameter">Der Enum-Wert, der mit value verglichen wird</param>
        /// <param name="culture">Die Kultur</param>
        /// <returns>true wenn value und parameter gleich sind, sonst false</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            // Wenn value null nullable ist
            if (value is bool boolValue)
            {
                bool paramValue;
                if (parameter is bool)
                {
                    paramValue = (bool)parameter;
                }
                else if (parameter is string && bool.TryParse((string)parameter, out bool parsedBool))
                {
                    paramValue = parsedBool;
                }
                else
                {
                    return false;
                }

                return boolValue == paramValue;
            }

            string valueStr = value.ToString();
            string parameterStr = parameter.ToString();

            return valueStr.Equals(parameterStr, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Konvertiert einen Boolean-Wert zurück zu einem Enum-Wert
        /// </summary>
        /// <param name="value">Der zu konvertierende Wert</param>
        /// <param name="targetType">Der Zieltyp</param>
        /// <param name="parameter">Der Enum-Wert, der zurückgegeben wird, wenn value true ist</param>
        /// <param name="culture">Die Kultur</param>
        /// <returns>Den Enum-Wert, wenn value true ist, sonst den Standard-Enum-Wert</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null || !(value is bool))
                return null;

            bool boolValue = (bool)value;

            if (!boolValue)
                return null;

            // Wenn targetType bool ist
            if (targetType == typeof(bool) || targetType == typeof(bool?))
            {
                if (parameter is bool)
                {
                    return parameter;
                }
                else if (parameter is string && bool.TryParse((string)parameter, out bool parsedBool))
                {
                    return parsedBool;
                }
                else
                {
                    return null;
                }
            }

            // Wenn targetType ein Enum ist
            if (targetType.IsEnum)
            {
                return Enum.Parse(targetType, parameter.ToString());
            }

            return parameter;
        }
    }
}