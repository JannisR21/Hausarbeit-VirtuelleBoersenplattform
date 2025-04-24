using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using System.Diagnostics;
using System.Windows.Controls.Primitives;

namespace HausarbeitVirtuelleBörsenplattform.Helpers
{
    /// <summary>
    /// Hilfsklasse zum Aktualisieren der Benutzeroberfläche
    /// </summary>
    public static class DataUIExtensions
    {
        /// <summary>
        /// Aktualisiert ein DataGrid nach Datenänderungen
        /// </summary>
        /// <param name="dataGrid">Das zu aktualisierende DataGrid</param>
        /// <param name="refreshItems">Optional: True, um Items neu zu laden</param>
        public static void RefreshDataGrid(this DataGrid dataGrid, bool refreshItems = false)
        {
            if (dataGrid == null) return;

            try
            {
                Debug.WriteLine("Aktualisierung des DataGrid wird durchgeführt...");
                var dispatcher = dataGrid.Dispatcher;

                // Auf dem UI-Thread ausführen
                if (dispatcher.CheckAccess())
                {
                    if (refreshItems)
                    {
                        var currentItemsSource = dataGrid.ItemsSource;
                        dataGrid.ItemsSource = null;
                        dataGrid.ItemsSource = currentItemsSource;
                        dataGrid.Items.Refresh();
                    }
                    else
                    {
                        dataGrid.Items.Refresh();
                    }

                    // Spalten aktualisieren
                    foreach (var column in dataGrid.Columns)
                    {
                        if (column is DataGridBoundColumn boundColumn)
                        {
                            // Statt direkt auf binding.UpdateTarget() zuzugreifen (was nicht existiert),
                            // verwenden wir BindingOperations.GetBindingExpression
                            if (boundColumn.Binding != null && boundColumn.Header != null)
                            {
                                // Suche nach dem ersten Element, das diese Spalte verwendet
                                foreach (var item in dataGrid.Items)
                                {
                                    var container = dataGrid.ItemContainerGenerator.ContainerFromItem(item) as DataGridRow;
                                    if (container != null)
                                    {
                                        var cell = GetCell(dataGrid, container, dataGrid.Columns.IndexOf(column));
                                        if (cell != null)
                                        {
                                            // Aktualisieren der Bindungen in der Zelle
                                            BindingOperations.GetBindingExpression(cell, DataGridCell.ContentProperty)?.UpdateTarget();
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    Debug.WriteLine("DataGrid wurde erfolgreich aktualisiert");
                }
                else
                {
                    // Auf dem UI-Thread aufrufen
                    dispatcher.BeginInvoke(new Action(() => RefreshDataGrid(dataGrid, refreshItems)));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Aktualisieren des DataGrid: {ex.Message}");
            }
        }

        /// <summary>
        /// Hilfsmethode zum Abrufen einer DataGridCell aus einer Zeile und einem Spaltenindex
        /// </summary>
        private static DataGridCell GetCell(DataGrid dataGrid, DataGridRow row, int columnIndex)
        {
            if (row != null)
            {
                DataGridCellsPresenter presenter = FindVisualChild<DataGridCellsPresenter>(row);
                if (presenter != null)
                {
                    // Try to get the cell, but handle the error if it occurs
                    try
                    {
                        return presenter.ItemContainerGenerator.ContainerFromIndex(columnIndex) as DataGridCell;
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Aktualisiert die Benutzeroberfläche eines UserControl
        /// </summary>
        /// <param name="control">Das zu aktualisierende UserControl</param>
        public static void RefreshUIElements(this UserControl control)
        {
            if (control == null) return;

            try
            {
                var dispatcher = control.Dispatcher;

                // Auf dem UI-Thread ausführen
                if (dispatcher.CheckAccess())
                {
                    // DataGrid finden und aktualisieren
                    foreach (var grid in FindVisualChildren<DataGrid>(control))
                    {
                        RefreshDataGrid(grid, true);
                    }

                    // TextBlock aktualisieren
                    foreach (var textBlock in FindVisualChildren<TextBlock>(control))
                    {
                        if (textBlock.DataContext != null && textBlock.GetBindingExpression(TextBlock.TextProperty) != null)
                        {
                            textBlock.GetBindingExpression(TextBlock.TextProperty).UpdateTarget();
                        }
                    }

                    Debug.WriteLine("UI-Elemente wurden erfolgreich aktualisiert");
                }
                else
                {
                    // Auf dem UI-Thread aufrufen
                    dispatcher.BeginInvoke(new Action(() => RefreshUIElements(control)));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Aktualisieren der UI-Elemente: {ex.Message}");
            }
        }

        /// <summary>
        /// Hilfsmethode zum Finden des ersten visuellen Kindes eines bestimmten Typs
        /// </summary>
        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child != null && child is T)
                {
                    return (T)child;
                }
                else
                {
                    T childOfChild = FindVisualChild<T>(child);
                    if (childOfChild != null)
                    {
                        return childOfChild;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Sucht nach allen Kindern eines bestimmten Typs in einem DependencyObject
        /// </summary>
        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = System.Windows.Media.VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    // Rekursiv in den Kindern suchen
                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }
    }
}