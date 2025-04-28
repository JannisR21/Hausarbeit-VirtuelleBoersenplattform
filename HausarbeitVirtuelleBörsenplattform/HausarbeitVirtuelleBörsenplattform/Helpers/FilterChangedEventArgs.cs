using System;
using System.Collections.Generic;
using HausarbeitVirtuelleBörsenplattform.Models;

namespace HausarbeitVirtuelleBörsenplattform.Helpers
{
    public class FilterChangedEventArgs : EventArgs
    {
        public IEnumerable<Aktie> GefilterteAktien { get; }
        public string SuchText { get; }
        public bool IstFilterAktiv { get; }

        public FilterChangedEventArgs(IEnumerable<Aktie> gefilterteAktien, string suchText, bool istFilterAktiv)
        {
            GefilterteAktien = gefilterteAktien;
            SuchText = suchText;
            IstFilterAktiv = istFilterAktiv;
        }
    }
}