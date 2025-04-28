using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using HausarbeitVirtuelleBörsenplattform.Helpers;
using HausarbeitVirtuelleBörsenplattform.ViewModels;

namespace HausarbeitVirtuelleBörsenplattform.Views
{
    /// <summary>
    /// Interaktionslogik für AktienFilterUserControl.xaml
    /// </summary>
    public partial class AktienFilterUserControl : UserControl
    {
        /// <summary>
        /// Event, das ausgelöst wird, wenn sich die Filter ändern
        /// </summary>
        public event EventHandler<FilterChangedEventArgs> FilterChanged;

        public AktienFilterUserControl()
        {
            InitializeComponent();
            Debug.WriteLine("AktienFilterUserControl initialisiert");

            // Event-Handler für DataContext-Änderungen hinzufügen
            this.DataContextChanged += AktienFilterUserControl_DataContextChanged;
            this.Loaded += AktienFilterUserControl_Loaded;
            this.Unloaded += AktienFilterUserControl_Unloaded;
        }

        /// <summary>
        /// Event-Handler für das Laden des Controls
        /// </summary>
        private void AktienFilterUserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("AktienFilterUserControl geladen");

            // DataContext-Änderungen überwachen und FilterChanged Event ggf. weiterleiten
            ConnectToViewModel();
        }

        /// <summary>
        /// Event-Handler für das Entladen des Controls
        /// </summary>
        private void AktienFilterUserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("AktienFilterUserControl entladen");

            // Event-Handler entfernen, um Memory Leaks zu vermeiden
            DisconnectFromViewModel();
        }

        /// <summary>
        /// Event-Handler für DataContext-Änderungen
        /// </summary>
        private void AktienFilterUserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Debug.WriteLine("AktienFilterUserControl DataContext geändert");

            // Alten Event-Handler entfernen
            if (e.OldValue is AktienFilterViewModel oldViewModel)
            {
                oldViewModel.PropertyChanged -= ViewModel_PropertyChanged;
                oldViewModel.FilterChanged -= ViewModel_FilterChanged;
            }

            // Mit neuem ViewModel verbinden
            ConnectToViewModel();
        }

        /// <summary>
        /// Verbindet das Control mit dem ViewModel
        /// </summary>
        private void ConnectToViewModel()
        {
            if (DataContext is AktienFilterViewModel viewModel)
            {
                // Event-Handler für PropertyChanged-Events des ViewModels hinzufügen
                viewModel.PropertyChanged -= ViewModel_PropertyChanged;
                viewModel.PropertyChanged += ViewModel_PropertyChanged;

                // Event-Handler für FilterChanged-Events des ViewModels hinzufügen
                viewModel.FilterChanged -= ViewModel_FilterChanged;
                viewModel.FilterChanged += ViewModel_FilterChanged;

                Debug.WriteLine("Mit AktienFilterViewModel verbunden");
            }
        }

        /// <summary>
        /// Trennt das Control vom ViewModel
        /// </summary>
        private void DisconnectFromViewModel()
        {
            if (DataContext is AktienFilterViewModel viewModel)
            {
                // Event-Handler entfernen
                viewModel.PropertyChanged -= ViewModel_PropertyChanged;
                viewModel.FilterChanged -= ViewModel_FilterChanged;

                Debug.WriteLine("Von AktienFilterViewModel getrennt");
            }
        }

        /// <summary>
        /// Event-Handler für PropertyChanged-Events des ViewModels
        /// </summary>
        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AktienFilterViewModel.GefilterteAktien) ||
                e.PropertyName == nameof(AktienFilterViewModel.SuchText) ||
                e.PropertyName == nameof(AktienFilterViewModel.IstFilterAktiv))
            {
                // FilterChanged-Event auslösen, wenn sich relevante Properties geändert haben
                OnFilterChanged();
            }
        }

        /// <summary>
        /// Event-Handler für FilterChanged-Events des ViewModels
        /// </summary>
        private void ViewModel_FilterChanged(object sender, FilterChangedEventArgs e)
        {
            // FilterChanged-Event weiterleiten
            OnFilterChanged(e);
        }

        /// <summary>
        /// Löst das FilterChanged-Event aus
        /// </summary>
        protected virtual void OnFilterChanged()
        {
            if (DataContext is AktienFilterViewModel viewModel)
            {
                // FilterChanged-Event mit Daten aus dem ViewModel auslösen
                var args = new FilterChangedEventArgs(viewModel.GefilterteAktien, viewModel.SuchText, viewModel.IstFilterAktiv);
                OnFilterChanged(args);
            }
        }

        /// <summary>
        /// Löst das FilterChanged-Event mit den übergebenen Argumenten aus
        /// </summary>
        protected virtual void OnFilterChanged(FilterChangedEventArgs e)
        {
            try
            {
                Debug.WriteLine($"AktienFilterUserControl: Löse FilterChanged-Event aus ({e.GefilterteAktien.Count()} Aktien, SuchText='{e.SuchText}')");
                FilterChanged?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Auslösen des FilterChanged-Events: {ex.Message}");
            }
        }
    }
}