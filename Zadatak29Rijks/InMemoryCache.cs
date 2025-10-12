using System;
using System.Collections.Concurrent;

namespace Zadatak29Rijks
{
    public class InMemoryCache
    {
        private sealed class CachedItem
        {
            public string Value { get; set; } = string.Empty;
            public DateTime Expiry { get; set; }
        }

        private readonly ConcurrentDictionary<string, CachedItem> _cache =
        new ConcurrentDictionary<string, CachedItem>();
        private readonly TimeSpan _ttl = TimeSpan.FromMinutes(2);

        public string Get(string key)
        {
            if (_cache.TryGetValue(key, out CachedItem item))
            {
                if (DateTime.Now < item.Expiry)
                    return item.Value;

                _cache.TryRemove(key, out _);
            }
            return null;
        }

        public void Set(string key, string value)
        {
            var item = new CachedItem
            {
                Value = value,
                Expiry = DateTime.Now.Add(_ttl)
            };
            _cache[key] = item;
        }

        public void CleanExpired()
        {
            int removed = 0;
            foreach (var key in _cache.Keys)
            {
                if (_cache.TryGetValue(key, out var item) && DateTime.Now >= item.Expiry)
                {
                    if (_cache.TryRemove(key, out _))
                        removed++;
                }
            }
            if (removed > 0)
                Console.WriteLine($"[Cache] Uklonjeno {removed} isteklih unosa ({DateTime.Now:T})");
        }
    }
}
