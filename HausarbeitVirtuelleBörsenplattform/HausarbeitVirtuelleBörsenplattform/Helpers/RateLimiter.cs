using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HausarbeitVirtuelleBörsenplattform.Helpers
{
    /// <summary>
    /// Ein einfacher Token‑Bucket–/Sliding‑Window‑Rate‑Limiter:
    /// maximal _maxRequests innerhalb von _interval.
    /// </summary>
    public class RateLimiter
    {
        private readonly int _maxRequests;
        private readonly TimeSpan _interval;
        private readonly Queue<DateTime> _timestamps = new Queue<DateTime>();
        private readonly object _lock = new object();

        public RateLimiter(int maxRequests, TimeSpan interval)
        {
            _maxRequests = maxRequests;
            _interval = interval;
        }

        /// <summary>
        /// Blockiert asynchron, bis ein neuer Request erlaubt ist.
        /// </summary>
        public async Task ThrottleAsync()
        {
            DateTime now = DateTime.UtcNow;
            TimeSpan wait;

            lock (_lock)
            {
                // Veraltete Einträge entfernen
                while (_timestamps.Count > 0 && now - _timestamps.Peek() > _interval)
                {
                    _timestamps.Dequeue();
                }

                // Freier Slot?
                if (_timestamps.Count < _maxRequests)
                {
                    _timestamps.Enqueue(now);
                    return;
                }

                // Ansonsten bis zum ältesten Zeitstempel + Interval warten
                DateTime earliest = _timestamps.Peek();
                wait = (earliest + _interval) - now;
            }

            if (wait > TimeSpan.Zero)
            {
                await Task.Delay(wait);
            }

            // Nach dem Warten aufräumen und neuen Timestamp setzen
            lock (_lock)
            {
                now = DateTime.UtcNow;
                while (_timestamps.Count > 0 && now - _timestamps.Peek() > _interval)
                {
                    _timestamps.Dequeue();
                }
                _timestamps.Enqueue(now);
            }
        }
    }
}
