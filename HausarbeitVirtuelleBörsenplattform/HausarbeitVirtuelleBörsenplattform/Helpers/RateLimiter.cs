using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;

namespace HausarbeitVirtuelleBörsenplattform.Helpers
{
    /// <summary>
    /// Ein verbesserter Rate-Limiter für API-Anfragen, der das Basic 8 Plan-Limit berücksichtigt
    /// </summary>
    public class RateLimiter
    {
        private readonly int _maxRequests;
        private readonly TimeSpan _interval;
        private readonly Queue<DateTime> _timestamps = new Queue<DateTime>();
        private readonly object _lock = new object();
        private int _currentMinuteRequests = 0;
        private DateTime _currentMinuteStart = DateTime.UtcNow;

        /// <summary>
        /// Initialisiert einen neuen RateLimiter
        /// </summary>
        /// <param name="maxRequests">Maximale Anzahl von Anfragen pro Intervall</param>
        /// <param name="interval">Zeitintervall für die Begrenzung</param>
        public RateLimiter(int maxRequests, TimeSpan interval)
        {
            _maxRequests = maxRequests;
            _interval = interval;
            Debug.WriteLine($"RateLimiter initialisiert: {maxRequests} Anfragen pro {interval.TotalMinutes} Minuten");
        }

        /// <summary>
        /// Blockiert asynchron, bis ein neuer Request erlaubt ist.
        /// Verbesserte Version für den Basic 8 Plan
        /// </summary>
        public async Task ThrottleAsync()
        {
            DateTime now = DateTime.UtcNow;
            TimeSpan wait = TimeSpan.Zero; // Hier initialisieren wir wait mit einem Standardwert
            bool needsToWait = false;

            lock (_lock)
            {
                // Prüfen, ob wir in eine neue Minute gewechselt sind
                if ((now - _currentMinuteStart).TotalMinutes >= 1)
                {
                    Debug.WriteLine("RateLimiter: Neue Minute begonnen, setze Zähler zurück");
                    _currentMinuteStart = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, DateTimeKind.Utc);
                    _currentMinuteRequests = 0;
                }

                // Veraltete Einträge aus der Queue entfernen
                while (_timestamps.Count > 0 && now - _timestamps.Peek() > _interval)
                {
                    _timestamps.Dequeue();
                }

                // Prüfen, ob wir das Limit für die aktuelle Minute erreicht haben
                if (_currentMinuteRequests >= _maxRequests)
                {
                    Debug.WriteLine($"RateLimiter: Limit erreicht ({_currentMinuteRequests}/{_maxRequests}), warte auf nächste Minute");

                    // Berechne, wie lange bis zur nächsten Minute gewartet werden muss
                    var nextMinute = _currentMinuteStart.AddMinutes(1);
                    wait = nextMinute - now;
                    needsToWait = true;
                }
                // Dann prüfen, ob wir das generelle Limit erreicht haben
                else if (_timestamps.Count >= _maxRequests)
                {
                    // Wenn alle Slots belegt sind, warten wir, bis der älteste Slot frei wird
                    DateTime earliest = _timestamps.Peek();
                    wait = (earliest + _interval) - now;

                    if (wait > TimeSpan.Zero)
                    {
                        Debug.WriteLine($"RateLimiter: Alle Slots belegt, warte {wait.TotalSeconds:F1} Sekunden");
                        needsToWait = true;
                    }
                    else
                    {
                        // Wartezeit ist abgelaufen, wir können fortfahren
                        _timestamps.Dequeue(); // Entferne den ältesten Eintrag
                        _timestamps.Enqueue(now); // Füge den neuen Zeitstempel hinzu
                        _currentMinuteRequests++;
                        Debug.WriteLine($"RateLimiter: Request erlaubt ({_currentMinuteRequests}/{_maxRequests})");
                    }
                }
                else
                {
                    // Es ist noch Platz im Limit, wir können fortfahren
                    _timestamps.Enqueue(now);
                    _currentMinuteRequests++;
                    Debug.WriteLine($"RateLimiter: Request erlaubt ({_currentMinuteRequests}/{_maxRequests})");
                }
            }

            // Wenn wir warten müssen, tun wir das außerhalb des Locks
            if (needsToWait && wait > TimeSpan.Zero)
            {
                // Zusätzlich kleine Sicherheitsmarge hinzufügen
                var waitWithMargin = wait.Add(TimeSpan.FromMilliseconds(100));
                Debug.WriteLine($"RateLimiter: Warte {waitWithMargin.TotalSeconds:F1} Sekunden");
                await Task.Delay(waitWithMargin);

                // Nach dem Warten rekursiv nochmal prüfen
                await ThrottleAsync();
            }
        }
    }
}