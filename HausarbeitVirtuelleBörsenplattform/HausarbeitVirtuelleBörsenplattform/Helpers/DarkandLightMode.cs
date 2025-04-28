using System;
using System.Windows;
using System.Diagnostics;

namespace HausarbeitVirtuelleBörsenplattform.Helpers
{
    public static class DarkAndLightMode
    {
        // Pfade zu den Theme-Dictionaries
        private const string LightThemePath = "/Themes/LightTheme.xaml";
        private const string DarkThemePath = "/Themes/DarkTheme.xaml";

        private static ResourceDictionary _currentTheme;

        public static bool IsDarkModeEnabled { get; private set; } = false;

        public static void Initialize()
        {
            Debug.WriteLine("DarkAndLightMode wird initialisiert");
            // Standard-Theme (Light) laden
            SetLightTheme();
        }

        public static void SetDarkTheme()
        {
            try
            {
                Debug.WriteLine("Aktiviere Dark Mode");
                if (_currentTheme != null)
                    Application.Current.Resources.MergedDictionaries.Remove(_currentTheme);

                _currentTheme = new ResourceDictionary
                {
                    Source = new Uri(DarkThemePath, UriKind.Relative)
                };
                Application.Current.Resources.MergedDictionaries.Add(_currentTheme);
                IsDarkModeEnabled = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Aktivieren des Dark Mode: {ex.Message}");
            }
        }

        public static void SetLightTheme()
        {
            try
            {
                Debug.WriteLine("Aktiviere Light Mode");
                if (_currentTheme != null)
                    Application.Current.Resources.MergedDictionaries.Remove(_currentTheme);

                _currentTheme = new ResourceDictionary
                {
                    Source = new Uri(LightThemePath, UriKind.Relative)
                };
                Application.Current.Resources.MergedDictionaries.Add(_currentTheme);
                IsDarkModeEnabled = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Aktivieren des Light Mode: {ex.Message}");
            }
        }

        public static void ToggleTheme()
        {
            if (IsDarkModeEnabled)
                SetLightTheme();
            else
                SetDarkTheme();
        }
    }
}