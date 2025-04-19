using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;

namespace HausarbeitVirtuelleBörsenplattform.Helpers
{
    /// <summary>
    /// Hilfsklasse für Wasserzeichen in TextBox und PasswordBox
    /// </summary>
    public static class WatermarkHelper
    {
        public static readonly DependencyProperty WatermarkTextProperty =
            DependencyProperty.RegisterAttached("WatermarkText", typeof(string), typeof(WatermarkHelper),
                new PropertyMetadata(string.Empty, OnWatermarkTextChanged));

        public static string GetWatermarkText(DependencyObject obj)
        {
            return (string)obj.GetValue(WatermarkTextProperty);
        }

        public static void SetWatermarkText(DependencyObject obj, string value)
        {
            obj.SetValue(WatermarkTextProperty, value);
        }

        private static void OnWatermarkTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                // Event-Handler für TextBox
                textBox.Loaded -= TextBoxLoaded;
                textBox.Loaded += TextBoxLoaded;
                textBox.TextChanged -= TextBoxTextChanged;
                textBox.TextChanged += TextBoxTextChanged;
                textBox.GotFocus -= TextBoxGotFocus;
                textBox.GotFocus += TextBoxGotFocus;
                textBox.LostFocus -= TextBoxLostFocus;
                textBox.LostFocus += TextBoxLostFocus;

                if (textBox.IsLoaded)
                {
                    TextBoxLoaded(textBox, null);
                }
            }
            else if (d is PasswordBox passwordBox)
            {
                // Event-Handler für PasswordBox
                passwordBox.Loaded -= PasswordBoxLoaded;
                passwordBox.Loaded += PasswordBoxLoaded;
                passwordBox.PasswordChanged -= PasswordBoxPasswordChanged;
                passwordBox.PasswordChanged += PasswordBoxPasswordChanged;
                passwordBox.GotFocus -= PasswordBoxGotFocus;
                passwordBox.GotFocus += PasswordBoxGotFocus;
                passwordBox.LostFocus -= PasswordBoxLostFocus;
                passwordBox.LostFocus += PasswordBoxLostFocus;

                if (passwordBox.IsLoaded)
                {
                    PasswordBoxLoaded(passwordBox, null);
                }
            }
        }

        #region TextBox Handlers

        private static void TextBoxLoaded(object sender, RoutedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            SetTextBoxWatermark(textBox);
        }

        private static void TextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            SetTextBoxWatermark(textBox);
        }

        private static void TextBoxGotFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            ClearTextBoxWatermark(textBox);
        }

        private static void TextBoxLostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            SetTextBoxWatermark(textBox);
        }

        private static void SetTextBoxWatermark(TextBox textBox)
        {
            // Wenn TextBox leer ist und den Fokus nicht hat, Wasserzeichen anzeigen
            if (string.IsNullOrEmpty(textBox.Text) && !textBox.IsFocused)
            {
                AdornerLayer layer = AdornerLayer.GetAdornerLayer(textBox);
                if (layer != null)
                {
                    // Vorhandene Adorner entfernen
                    Adorner[] adorners = layer.GetAdorners(textBox);
                    if (adorners != null)
                    {
                        foreach (Adorner adorner in adorners)
                        {
                            if (adorner is WatermarkAdorner)
                            {
                                layer.Remove(adorner);
                            }
                        }
                    }

                    // Neuen Adorner hinzufügen
                    layer.Add(new WatermarkAdorner(textBox, GetWatermarkText(textBox)));
                }
            }
            else
            {
                ClearTextBoxWatermark(textBox);
            }
        }

        private static void ClearTextBoxWatermark(TextBox textBox)
        {
            AdornerLayer layer = AdornerLayer.GetAdornerLayer(textBox);
            if (layer != null)
            {
                Adorner[] adorners = layer.GetAdorners(textBox);
                if (adorners != null)
                {
                    foreach (Adorner adorner in adorners)
                    {
                        if (adorner is WatermarkAdorner)
                        {
                            layer.Remove(adorner);
                        }
                    }
                }
            }
        }

        #endregion

        #region PasswordBox Handlers

        private static void PasswordBoxLoaded(object sender, RoutedEventArgs e)
        {
            PasswordBox passwordBox = (PasswordBox)sender;
            SetPasswordBoxWatermark(passwordBox);
        }

        private static void PasswordBoxPasswordChanged(object sender, RoutedEventArgs e)
        {
            PasswordBox passwordBox = (PasswordBox)sender;
            SetPasswordBoxWatermark(passwordBox);
        }

        private static void PasswordBoxGotFocus(object sender, RoutedEventArgs e)
        {
            PasswordBox passwordBox = (PasswordBox)sender;
            ClearPasswordBoxWatermark(passwordBox);
        }

        private static void PasswordBoxLostFocus(object sender, RoutedEventArgs e)
        {
            PasswordBox passwordBox = (PasswordBox)sender;
            SetPasswordBoxWatermark(passwordBox);
        }

        private static void SetPasswordBoxWatermark(PasswordBox passwordBox)
        {
            // Wenn PasswordBox leer ist und den Fokus nicht hat, Wasserzeichen anzeigen
            if (string.IsNullOrEmpty(passwordBox.Password) && !passwordBox.IsFocused)
            {
                AdornerLayer layer = AdornerLayer.GetAdornerLayer(passwordBox);
                if (layer != null)
                {
                    // Vorhandene Adorner entfernen
                    Adorner[] adorners = layer.GetAdorners(passwordBox);
                    if (adorners != null)
                    {
                        foreach (Adorner adorner in adorners)
                        {
                            if (adorner is WatermarkAdorner)
                            {
                                layer.Remove(adorner);
                            }
                        }
                    }

                    // Neuen Adorner hinzufügen
                    layer.Add(new WatermarkAdorner(passwordBox, GetWatermarkText(passwordBox)));
                }
            }
            else
            {
                ClearPasswordBoxWatermark(passwordBox);
            }
        }

        private static void ClearPasswordBoxWatermark(PasswordBox passwordBox)
        {
            AdornerLayer layer = AdornerLayer.GetAdornerLayer(passwordBox);
            if (layer != null)
            {
                Adorner[] adorners = layer.GetAdorners(passwordBox);
                if (adorners != null)
                {
                    foreach (Adorner adorner in adorners)
                    {
                        if (adorner is WatermarkAdorner)
                        {
                            layer.Remove(adorner);
                        }
                    }
                }
            }
        }

        #endregion

        /// <summary>
        /// Adorner für Wasserzeichen
        /// </summary>
        private class WatermarkAdorner : Adorner
        {
            private readonly TextBlock _watermark;

            public WatermarkAdorner(UIElement adornedElement, string watermarkText) : base(adornedElement)
            {
                _watermark = new TextBlock
                {
                    Text = watermarkText,
                    Foreground = System.Windows.Media.Brushes.Gray,
                    Opacity = 0.7,
                    IsHitTestVisible = false
                };

                // Stil an das Control anpassen
                if (adornedElement is Control control)
                {
                    _watermark.FontFamily = control.FontFamily;
                    _watermark.FontSize = control.FontSize;
                    _watermark.FontStyle = control.FontStyle;
                    _watermark.FontWeight = control.FontWeight;
                }
            }

            protected override void OnRender(System.Windows.Media.DrawingContext drawingContext)
            {
                Control control = (Control)AdornedElement;

                // Position für das Wasserzeichen festlegen
                double left = 7; // Standardmäßiger linker Rand
                double top = (control.ActualHeight - _watermark.ActualHeight) / 2;

                // Für TextBox und PasswordBox mit Padding
                if (control is TextBoxBase textBox && textBox.Padding != null)
                {
                    left = textBox.Padding.Left;
                }
                else if (control is PasswordBox passwordBox && passwordBox.Padding != null)
                {
                    left = passwordBox.Padding.Left;
                }

                _watermark.Arrange(new Rect(new Point(left, top), new Size(control.ActualWidth - left - 5, control.ActualHeight)));
                drawingContext.DrawRectangle(System.Windows.Media.Brushes.Transparent, null, new Rect(0, 0, control.ActualWidth, control.ActualHeight));
                drawingContext.PushOpacity(_watermark.Opacity);
                drawingContext.DrawText(new FormattedText(
                    _watermark.Text,
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new System.Windows.Media.Typeface(_watermark.FontFamily, _watermark.FontStyle, _watermark.FontWeight, _watermark.FontStretch),
                    _watermark.FontSize,
                    _watermark.Foreground,
                    new System.Windows.Media.NumberSubstitution(),
                    1.0),
                    new Point(left, top));
                drawingContext.Pop();
            }
        }
    }
}