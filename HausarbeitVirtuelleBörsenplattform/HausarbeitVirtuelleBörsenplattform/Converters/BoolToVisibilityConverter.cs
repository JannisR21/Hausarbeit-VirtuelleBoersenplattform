using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace HausarbeitVirtuelleBörsenplattform.Converters
{
    /// <summary>
    /// Konvertiert einen Boolean-Wert in einen Visibility-Wert und umgekehrt
    /// </summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Konvertiert einen Boolean-Wert in einen Visibility-Wert
        /// </summary>
        /// <param name="value">Der zu konvertierende Wert</param>
        /// <param name="targetType">Der Zieltyp</param>
        /// <param name="parameter">Ein Parameter, mit dem ein Inverted-Verhalten gesteuert werden kann</param>
        /// <param name="culture">Die Kultur</param>
        /// <returns>Visibility.Visible wenn true, sonst Visibility.Collapsed (oder umgekehrt bei Parameter=inverted)</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isInverted = parameter != null && parameter.ToString().ToLower() == "inverted";
            bool boolValue;

            // Prüfe den Typ des Werts und konvertiere entsprechend
            if (value is bool)
            {
                boolValue = (bool)value;
            }
            else if (value is int)
            {
                boolValue = ((int)value) > 0;
            }
            else if (value is double)
            {
                boolValue = ((double)value) > 0;
            }
            else if (value is float)
            {
                boolValue = ((float)value) > 0;
            }
            else if (value is decimal)
            {
                boolValue = ((decimal)value) > 0;
            }
            else
            {
                boolValue = value != null;
            }

            if (isInverted)
                boolValue = !boolValue;

            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Konvertiert einen Visibility-Wert zurück in einen Boolean-Wert
        /// </summary>
        /// <param name="value">Der zu konvertierende Wert</param>
        /// <param name="targetType">Der Zieltyp</param>
        /// <param name="parameter">Ein Parameter, mit dem ein Inverted-Verhalten gesteuert werden kann</param>
        /// <param name="culture">Die Kultur</param>
        /// <returns>true wenn Visibility.Visible, sonst false (oder umgekehrt bei Parameter=inverted)</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isInverted = parameter != null && parameter.ToString().ToLower() == "inverted";
            Visibility visibility = (Visibility)value;
            bool result = visibility == Visibility.Visible;

            if (isInverted)
                result = !result;

            return result;
        }
    }
}