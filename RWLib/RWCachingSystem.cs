using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace RWLib
{
    public class RWCachingSystem
    {
        private Action<CacheEntry> storeCacheEntry;
        private Func<string, CacheEntry?> retrieveCacheEntry;
        private Action<string> purgeCacheEntry;

        public class CacheEntry
        {
            [JsonPropertyName("key")]
            public string Key { get; set; } = "";
            [JsonPropertyName("sourceHash")]
            public long SourceHash { get; set; } = 0;
            [JsonPropertyName("persistentValue")]
            public string PersistentValue { get; set; } = "";

            public bool DidChange(object value)
            {
                return SourceHash != value.GetHashCode();
            }
            public bool DidChange(int hashCode)
            {
                return SourceHash != hashCode;
            }

            public bool IsStillRelevant(object value)
            {
                return !DidChange(value);
            }

            public bool IsStillRelevant(int hashCode)
            {
                return !DidChange(hashCode);
            }
        }

        private RWCachingSystem(Action<CacheEntry> storeCacheEntry, Action<string> purgeEntry, Func<string, CacheEntry?> retrieveCacheEntry)
        {
            this.storeCacheEntry = storeCacheEntry;
            this.retrieveCacheEntry = retrieveCacheEntry;
            this.purgeCacheEntry = purgeEntry;
        }

        public bool DidChange(string key, object value)
        {
            return retrieveCacheEntry(key)?.DidChange(value.GetHashCode()) ?? true;
        }

        public bool DidChange(string key, int hashCode)
        {
            return retrieveCacheEntry(key)?.DidChange(hashCode) ?? true;
        }

        public CacheEntry? GetEntry(string key)
        {
            return retrieveCacheEntry(key);
        }

        public void StoreCacheEntry(string cacheKey, long hashCode, string binFile)
        {
            this.storeCacheEntry(new CacheEntry
            {
                Key = cacheKey,
                SourceHash = hashCode,
                PersistentValue = binFile
            });
        }

        public static RWCachingSystem CreateFileSystemCache(string filename = "RWCache.zip")
        {
            var z = ZipFile.Open(filename, ZipArchiveMode.Update);
            var _lock = new object();

            AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
            {
                z.Dispose();
            };

            return new RWCachingSystem(
                (entry) =>
                {
                    lock (_lock)
                    {
                        var archiveEntry = z.CreateEntry(entry.Key);
                        using (var stream = archiveEntry.Open())
                        {
                            JsonSerializer.Serialize(stream, entry);
                        }
                    }
                },
                (key) =>
                {
                    lock(_lock)
                    {
                        var archiveEntry = z.CreateEntry(key);
                        archiveEntry.Delete();
                    }
                },
                (key) =>
                {
                    lock (_lock)
                    {
                        var entry = z.GetEntry(key);
                        if (entry == null) return null;
                        using (var stream = entry.Open())
                        {
                            return JsonSerializer.Deserialize<CacheEntry>(stream);
                        }
                    }
                });
        }

        public void PurgeEntry(string cacheKey)
        {
            this.purgeCacheEntry(cacheKey);
        }
    }
}
