﻿using HausarbeitVirtuelleBörsenplattform.Models;
using HausarbeitVirtuelleBörsenplattform.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace HausarbeitVirtuelleBörsenplattform.Views
{
    /// <summary>
    /// Interaktionslogik für EinstellungenUserControl.xaml
    /// </summary>
    public partial class EinstellungenUserControl : UserControl
    {
        /// <summary>
        /// API-Key Property für Databinding
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// Initialisiert eine neue Instanz von EinstellungenUserControl
        /// </summary>
        public EinstellungenUserControl()
        {
            InitializeComponent();

            // API-Key aus App-Konfiguration laden
            ApiKey = App.TwelveDataApiKey;

            // DataContext setzen
            this.DataContext = this;

            // Event-Handler für DarkMode-Toggle hinzufügen
            DarkModeToggle.Checked += DarkModeToggle_Checked;
            DarkModeToggle.Unchecked += DarkModeToggle_Unchecked;
        }

        private void DarkModeToggle_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                Helpers.DarkAndLightMode.SetDarkTheme();

                // Einstellung speichern
                Properties.Settings.Default.IsDarkModeEnabled = true;
                Properties.Settings.Default.Save();

                Debug.WriteLine("Dark Mode aktiviert");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Aktivieren des Dark Mode: {ex.Message}");
                MessageBox.Show($"Fehler beim Aktivieren des Dark Mode: {ex.Message}",
                              "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DarkModeToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                Helpers.DarkAndLightMode.SetLightTheme();

                // Einstellung speichern
                Properties.Settings.Default.IsDarkModeEnabled = false;
                Properties.Settings.Default.Save();

                Debug.WriteLine("Light Mode aktiviert");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Deaktivieren des Dark Mode: {ex.Message}");
                MessageBox.Show($"Fehler beim Deaktivieren des Dark Mode: {ex.Message}",
                              "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Event-Handler für den Zurück-Button
        /// </summary>
        private void ZurückButton_Click(object sender, RoutedEventArgs e)
        {
            // Zur Hauptansicht zurückkehren
            NavigateBackToMainView();
        }

        /// <summary>
        /// Event-Handler für Passwort ändern Button
        /// </summary>
        private async void PasswortÄndernButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var passwordDialog = new PasswortAendernDialog();

                // Dialog als modal anzeigen und Besitzer setzen
                passwordDialog.Owner = Window.GetWindow(this);
                bool? result = passwordDialog.ShowDialog();

                if (result == true && passwordDialog.PasswordChanged)
                {
                    MessageBox.Show(
                        "Ihr Passwort wurde erfolgreich geändert.",
                        "Passwort geändert",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler im PasswortÄndernButton_Click: {ex.Message}");
                MessageBox.Show($"Fehler: {ex.Message}", "Fehler",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Event-Handler für E-Mail ändern Button
        /// </summary>
        private void EmailÄndernButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("E-Mail-Änderung wird in einer zukünftigen Version implementiert.",
                            "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Event-Handler für API-Key speichern Button
        /// </summary>


        private async void PortfolioZurücksetzenButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show(
                    "Möchten Sie wirklich Ihr gesamtes Portfolio zurücksetzen?\n\n" +
                    "WICHTIG: Diese Aktion wird Ihr Portfolio komplett leeren und Ihren Kontostand auf das Startguthaben von 15.000€ zurücksetzen, unabhängig vom aktuellen Wert Ihrer Aktien.\n\n" +
                    "Dies ist ein vollständiger Neustart und kann nicht rückgängig gemacht werden.",
                    "Portfolio vollständig zurücksetzen",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    Mouse.OverrideCursor = Cursors.Wait; // Zeige Warte-Cursor an

                    // MainViewModel abrufen
                    var mainWindow = Window.GetWindow(this) as MainWindow;
                    var mainViewModel = mainWindow?.DataContext as MainViewModel;

                    if (mainViewModel != null && mainViewModel.AktuellerBenutzer != null)
                    {
                        int benutzerId = mainViewModel.AktuellerBenutzer.BenutzerID;

                        // Konstanten für den Reset
                        const decimal START_KONTOSTAND = 15000.00m; // Neuanfang mit 15.000€

                        bool erfolg = false;

                        // UI-Thread verwenden, um die Collection sofort zu leeren (visuelles Feedback)
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            // Collection leeren
                            mainViewModel.PortfolioViewModel.PortfolioEintraege.Clear();

                            // Gesamtwerte neu berechnen
                            mainViewModel.PortfolioViewModel.BerechneGesamtwerte();
                        });

                        // 1. Alle Portfolio-Einträge des Benutzers aus der Datenbank löschen
                        var dbEntries = await App.DbService.GetPortfolioByBenutzerIdAsync(benutzerId);
                        if (dbEntries != null && dbEntries.Count > 0)
                        {
                            foreach (var entry in dbEntries)
                            {
                                erfolg = await App.DbService.RemovePortfolioEintragAsync(benutzerId, entry.AktienID);
                                if (!erfolg)
                                {
                                    throw new Exception("Fehler beim Löschen eines Portfolio-Eintrags");
                                }
                            }
                        }

                        // 2. Alle Aktieneinträge aus der Datenbank bereinigen, die nicht mehr benötigt werden
                        try
                        {
                            // 2.1 Hole alle Aktien aus der Datenbank
                            var alleAktien = await App.DbService.GetAllAktienAsync();

                            // 2.2 Aktualisiere alle Aktien, damit sie auf ihren Ausgangszustand zurückgesetzt werden
                            foreach (var aktie in alleAktien)
                            {
                                // Zurücksetzen des Aktienzustands ohne die Aktie selbst zu löschen
                                aktie.AktuellerPreis = 0;
                                aktie.Änderung = 0;
                                aktie.ÄnderungProzent = 0;
                                aktie.LetzteAktualisierung = DateTime.Now;

                                // Aktie in der Datenbank aktualisieren
                                await App.DbService.UpdateAktieAsync(aktie);
                            }

                            Debug.WriteLine("Aktien in der Datenbank wurden zurückgesetzt");
                        }
                        catch (Exception aktienResetEx)
                        {
                            Debug.WriteLine($"Fehler beim Zurücksetzen der Aktien: {aktienResetEx.Message}");
                            // Fehler hier behandeln, aber den Gesamtprozess nicht stoppen
                        }

                        // 3. Kontostand auf Startguthaben zurücksetzen
                        var benutzer = mainViewModel.AktuellerBenutzer;
                        benutzer.Kontostand = START_KONTOSTAND;
                        erfolg = await App.DbService.UpdateBenutzerAsync(benutzer);

                        if (erfolg)
                        {
                            // UI aktualisieren
                            mainViewModel.AktualisiereKontostand(START_KONTOSTAND);

                            // HIER DIE ÄNDERUNG: Apple-Aktien nach dem Reset hinzufügen
                            try
                            {
                                // Die neue GetOrCreateAktieBySymbolAsync-Methode verwenden
                                var appleAktie = await App.DbService.GetOrCreateAktieBySymbolAsync("AAPL", "Apple Inc.", 180.00m);

                                if (appleAktie != null)
                                {
                                    // Aktuellen Kurs bestimmen
                                    decimal aktuellerPreis = appleAktie.AktuellerPreis;

                                    if (App.TwelveDataService != null)
                                    {
                                        try
                                        {
                                            var aktienDaten = await App.TwelveDataService.HoleAktienKurse(new List<string> { "AAPL" });
                                            if (aktienDaten != null && aktienDaten.Count > 0 && aktienDaten[0].AktuellerPreis > 0)
                                            {
                                                aktuellerPreis = aktienDaten[0].AktuellerPreis;
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Debug.WriteLine($"Fehler beim Abrufen des aktuellen Apple-Kurses: {ex.Message}");
                                        }
                                    }

                                    // 2 Apple-Aktien zum Portfolio hinzufügen
                                    var portfolioEintrag = new PortfolioEintrag
                                    {
                                        BenutzerID = benutzerId,
                                        AktienID = appleAktie.AktienID,
                                        AktienSymbol = appleAktie.AktienSymbol,
                                        AktienName = appleAktie.AktienName,
                                        Anzahl = 2,
                                        AktuellerKurs = aktuellerPreis,
                                        EinstandsPreis = aktuellerPreis,
                                        LetzteAktualisierung = DateTime.Now
                                    };

                                    // Zum Portfolio hinzufügen
                                    bool success = await App.DbService.AddOrUpdatePortfolioEintragAsync(portfolioEintrag);

                                    if (success)
                                    {
                                        // Kosten vom Startguthaben abziehen
                                        decimal kosten = 2 * aktuellerPreis;
                                        benutzer.Kontostand -= kosten;
                                        mainViewModel.AktualisiereKontostand(benutzer.Kontostand);

                                        // Benutzer in der Datenbank aktualisieren
                                        await App.DbService.UpdateBenutzerAsync(benutzer);

                                        Debug.WriteLine($"2 Apple-Aktien wurden nach dem Portfolio-Reset hinzugefügt für {kosten:F2}€");
                                    }
                                }
                            }
                            catch (Exception appleEx)
                            {
                                Debug.WriteLine($"Fehler beim Hinzufügen der Apple-Aktien nach dem Reset: {appleEx.Message}");
                            }

                            // Daten neu laden, um sicherzustellen, dass alles synchron ist
                            await mainViewModel.PortfolioViewModel.LoadPortfolioDataAsync();

                            // Performance-Chart zurücksetzen (falls vorhanden)
                            if (mainViewModel.PortfolioChartViewModel != null)
                            {
                                // Durch Dispatcher.Invoke auf dem UI-Thread ausführen
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    try
                                    {
                                        // Da wir den Chart nicht direkt zurücksetzen können, triggern wir einen erneuten
                                        // Zeitraum-Wechsel, was den Chart aktualisiert
                                        if (mainViewModel.PortfolioChartViewModel.IsOneDay)
                                        {
                                            mainViewModel.PortfolioChartViewModel.IsOneWeek = true;
                                            mainViewModel.PortfolioChartViewModel.IsOneDay = true;
                                        }
                                        else if (mainViewModel.PortfolioChartViewModel.IsOneWeek)
                                        {
                                            mainViewModel.PortfolioChartViewModel.IsOneMonth = true;
                                            mainViewModel.PortfolioChartViewModel.IsOneWeek = true;
                                        }
                                        else
                                        {
                                            // Standardmäßig zu 1 Woche wechseln
                                            mainViewModel.PortfolioChartViewModel.IsOneWeek = true;
                                        }
                                    }
                                    catch (Exception chartEx)
                                    {
                                        Debug.WriteLine($"Fehler beim Zurücksetzen des Performance-Charts: {chartEx.Message}");
                                        // Fehler beim Chart-Reset beeinträchtigt nicht den gesamten Reset
                                    }
                                });
                            }

                            // 4. WICHTIG: Marktdaten zurücksetzen - dies ist der Hauptteil der Lösung
                            if (mainViewModel.MarktdatenViewModel != null)
                            {
                                try
                                {
                                    // Die ResetMarktdatenAsync-Methode aufrufen
                                    await mainViewModel.MarktdatenViewModel.ResetMarktdatenAsync(true);
                                    Debug.WriteLine("Marktdaten wurden erfolgreich zurückgesetzt");
                                }
                                catch (Exception mdEx)
                                {
                                    Debug.WriteLine($"Fehler beim Zurücksetzen der Marktdaten: {mdEx.Message}");
                                    // Fehler hier behandeln, aber den Gesamtprozess nicht stoppen
                                }
                            }

                            // 5. Benachrichtigen, dass der Benutzer geändert wurde
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                mainViewModel.NotifyAktuellerBenutzerChanged();
                            });

                            MessageBox.Show(
                                $"Portfolio wurde erfolgreich zurückgesetzt.\n" +
                                $"Ihr Kontostand beträgt jetzt wieder {benutzer.Kontostand:N2}€ (inklusive der 2 geschenkten Apple-Aktien).\n" +
                                $"Sie können nun komplett von vorne beginnen.",
                                "Portfolio zurückgesetzt",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);

                            // 6. Optional: Anwendung neu starten, um einen vollständigen Reset zu gewährleisten
                            if (MessageBox.Show(
                                "Möchten Sie die Anwendung neu starten, um alle Änderungen wirksam zu machen?",
                                "Neustart empfohlen",
                                MessageBoxButton.YesNo,
                                MessageBoxImage.Question) == MessageBoxResult.Yes)
                            {
                                // Anwendung neu starten ohne System.Windows.Forms zu verwenden
                                try
                                {
                                    // Aktuellen Anwendungspfad ermitteln
                                    string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;

                                    // Neuen Prozess starten
                                    System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo(exePath);
                                    startInfo.UseShellExecute = true;
                                    System.Diagnostics.Process.Start(startInfo);

                                    // Aktuelle Anwendung beenden
                                    Application.Current.Shutdown();
                                }
                                catch (Exception restartEx)
                                {
                                    Debug.WriteLine($"Fehler beim Neustart der Anwendung: {restartEx.Message}");
                                    MessageBox.Show($"Die Anwendung konnte nicht automatisch neu gestartet werden: {restartEx.Message}",
                                        "Fehler beim Neustart", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }
                        }
                        else
                        {
                            throw new Exception("Fehler beim Aktualisieren des Kontostands");
                        }
                    }
                    else
                    {
                        MessageBox.Show(
                            "Kein Benutzer angemeldet oder MainViewModel nicht gefunden.",
                            "Fehler",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Zurücksetzen des Portfolios: {ex.Message}");
                MessageBox.Show(
                    $"Fehler beim Zurücksetzen des Portfolios: {ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null; // Warte-Cursor zurücksetzen
            }
        }

        /// <summary>
        /// Event-Handler für Konto löschen Button
        /// </summary>
        private async void KontoLöschenButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show(
                    "Möchten Sie wirklich Ihr Konto löschen? Alle Ihre Daten werden unwiderruflich gelöscht " +
                    "und Sie werden ausgeloggt. Diese Aktion kann nicht rückgängig gemacht werden.",
                    "Konto löschen",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    // Passwort zur Bestätigung anfordern mit dem neuen Dialog
                    var passwordDialog = new PasswortBestaetigenDialog
                    {
                        Owner = Window.GetWindow(this)
                    };

                    bool? dialogResult = passwordDialog.ShowDialog();

                    // Restlicher Code bleibt unverändert
                    if (dialogResult == true)
                    {
                        string password = passwordDialog.Password;

                        // Prüfen, ob das Passwort korrekt ist
                        var mainWindow = Window.GetWindow(this) as MainWindow;
                        var mainViewModel = mainWindow?.DataContext as MainViewModel;

                        if (mainViewModel != null && mainViewModel.AktuellerBenutzer != null)
                        {
                            int benutzerId = mainViewModel.AktuellerBenutzer.BenutzerID;
                            string benutzername = mainViewModel.AktuellerBenutzer.Benutzername;

                            // Passwort verifizieren
                            bool isPasswordValid = false;

                            // Für die Demo: Admin und Demo können ihr Konto nicht löschen
                            if (benutzername.Equals("admin", StringComparison.OrdinalIgnoreCase) ||
                                benutzername.Equals("demo", StringComparison.OrdinalIgnoreCase))
                            {
                                MessageBox.Show(
                                    "Die Demo- und Admin-Konten können nicht gelöscht werden.",
                                    "Operation nicht erlaubt",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning);
                                return;
                            }

                            // Passwort prüfen
                            if (benutzername.Equals("demo") && password.Equals("demo") ||
                                benutzername.Equals("admin") && password.Equals("admin"))
                            {
                                isPasswordValid = true;
                            }
                            else
                            {
                                // Normale Passwortprüfung mit AuthService
                                isPasswordValid = await App.AuthService.VerifyPasswordAsync(benutzername, password);
                            }

                            if (isPasswordValid)
                            {
                                Mouse.OverrideCursor = Cursors.Wait;

                                try
                                {
                                    // 1. Portfolio löschen
                                    var dbEntries = await App.DbService.GetPortfolioByBenutzerIdAsync(benutzerId);
                                    foreach (var entry in dbEntries)
                                    {
                                        await App.DbService.RemovePortfolioEintragAsync(benutzerId, entry.AktienID);
                                    }

                                    // 2. Benutzerkonto löschen
                                    bool erfolg = await App.DbService.DeleteBenutzerAsync(benutzerId);

                                    if (erfolg)
                                    {
                                        // Benutzer ausloggen und zum Login zurückkehren
                                        App.AuthService.Logout();

                                        MessageBox.Show(
                                            "Ihr Konto wurde erfolgreich gelöscht. Sie werden nun ausgeloggt.",
                                            "Konto gelöscht",
                                            MessageBoxButton.OK,
                                            MessageBoxImage.Information);

                                        // Zum Login-Fenster zurückkehren
                                        var loginWindow = new LoginWindow();
                                        loginWindow.Show();

                                        // Hauptfenster schließen
                                        mainWindow.Close();
                                    }
                                    else
                                    {
                                        throw new Exception("Fehler beim Löschen des Benutzerkontos");
                                    }
                                }
                                finally
                                {
                                    Mouse.OverrideCursor = null;
                                }
                            }
                            else
                            {
                                MessageBox.Show(
                                    "Das eingegebene Passwort ist nicht korrekt.",
                                    "Ungültiges Passwort",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Löschen des Kontos: {ex.Message}");
                MessageBox.Show(
                    $"Fehler beim Löschen des Kontos: {ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Event-Handler für Änderungen speichern Button
        /// </summary>
        private void ÄnderungenSpeichernButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool settingsChanged = false;

                // Aktualisierungsintervall speichern
                var selectedItem = AktualisierungsIntervallComboBox.SelectedItem as ComboBoxItem;
                if (selectedItem != null && selectedItem.Tag != null)
                {
                    int intervallInSekunden = Convert.ToInt32(selectedItem.Tag);
                    Debug.WriteLine($"Aktualisierungsintervall geändert auf: {intervallInSekunden} Sekunden");

                    // Speichere das Intervall in den Anwendungseinstellungen
                    Properties.Settings.Default.IsDarkModeEnabled = DarkModeToggle.IsChecked ?? false;
                    Properties.Settings.Default.Save();
                    settingsChanged = true;

                    // Aktualisiere das Timer-Intervall im MarktdatenViewModel
                    var mainWindow = Window.GetWindow(this) as MainWindow;
                    var mainViewModel = mainWindow?.DataContext as MainViewModel;
                    if (mainViewModel?.MarktdatenViewModel != null)
                    {
                        // Versuche das _aktualisierungsIntervall direkt zu setzen
                        // Da es privat ist, verwenden wir einen Workaround über die interne Timer-Einstellung
                        var marktdatenViewModel = mainViewModel.MarktdatenViewModel;

                        // Suche den _updateTimer per Reflection
                        System.Reflection.FieldInfo timerField = marktdatenViewModel.GetType()
                            .GetField("_updateTimer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                        if (timerField != null)
                        {
                            var timer = timerField.GetValue(marktdatenViewModel) as DispatcherTimer;
                            if (timer != null)
                            {
                                timer.Interval = TimeSpan.FromSeconds(intervallInSekunden);
                                Debug.WriteLine($"Timer-Intervall auf {intervallInSekunden} Sekunden gesetzt");
                                settingsChanged = true;
                            }
                        }
                    }
                }

                // Dark Mode Einstellung speichern - ist bereits durch die Event-Handler implementiert
                bool darkModeEnabled = DarkModeToggle.IsChecked ?? false;
                Debug.WriteLine($"Dark Mode: {darkModeEnabled}");

                MessageBox.Show("Einstellungen wurden gespeichert.", "Erfolg", MessageBoxButton.OK, MessageBoxImage.Information);

                // Optional: Frage nach Neustart, wenn Einstellungen geändert wurden
                if (settingsChanged && MessageBox.Show(
                    "Möchten Sie die Anwendung neu starten, um alle Änderungen wirksam zu machen?",
                    "Neustart empfohlen",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    // Anwendung neu starten
                    try
                    {
                        // Aktuellen Anwendungspfad ermitteln
                        string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;

                        // Neuen Prozess starten
                        System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo(exePath);
                        startInfo.UseShellExecute = true;
                        System.Diagnostics.Process.Start(startInfo);

                        // Aktuelle Anwendung beenden
                        Application.Current.Shutdown();
                    }
                    catch (Exception restartEx)
                    {
                        Debug.WriteLine($"Fehler beim Neustart der Anwendung: {restartEx.Message}");
                        MessageBox.Show($"Die Anwendung konnte nicht automatisch neu gestartet werden: {restartEx.Message}",
                            "Fehler beim Neustart", MessageBoxButton.OK, MessageBoxImage.Error);

                        // Trotz Fehler zurück zur Hauptansicht wechseln
                        NavigateBackToMainView();
                    }
                }
                else
                {
                    // Zurück zur Hauptansicht wechseln
                    NavigateBackToMainView();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Speichern der Einstellungen: {ex.Message}");
                MessageBox.Show($"Fehler beim Speichern der Einstellungen: {ex.Message}",
                                "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Navigiert zurück zur Hauptansicht
        /// </summary>
        private void NavigateBackToMainView()
        {
            try
            {
                // Elternfenster aus dem visuellen Baum ermitteln
                var window = Window.GetWindow(this) as MainWindow;
                if (window != null)
                {
                    // MainWindow auffordern, zur Hauptansicht zurückzukehren
                    var method = window.GetType().GetMethod("RestoreMainView",
                                System.Reflection.BindingFlags.Instance |
                                System.Reflection.BindingFlags.NonPublic |
                                System.Reflection.BindingFlags.Public);

                    if (method != null)
                    {
                        method.Invoke(window, null);
                        Debug.WriteLine("Zurück zur Hauptansicht navigiert");
                    }
                    else
                    {
                        // Falls die Methode im MainWindow nicht existiert, eine einfachere Lösung
                        window.Close();
                        var newWindow = new MainWindow();
                        newWindow.Show();
                        Debug.WriteLine("MainWindow neu geladen, um zur Hauptansicht zurückzukehren");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler bei der Navigation zurück zur Hauptansicht: {ex.Message}");
            }
        }

    }
}