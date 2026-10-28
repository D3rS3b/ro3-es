using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Ro3Es
{
    /// <summary>
    /// Diccionario original -> español, con captura de cadenas desconocidas
    /// y recarga en caliente.
    /// </summary>
    internal sealed class TranslationStore : IDisposable
    {
        private sealed class Entry
        {
            [JsonProperty("src")] public string Src;
            [JsonProperty("dst")] public string Dst;
            [JsonProperty("status")] public string Status;
            [JsonProperty("ctx")] public string Ctx;
        }

        private readonly string _dictPath;
        private readonly string _capturePath;
        private readonly Func<bool> _enabled;
        private readonly Func<bool> _capture;
        private readonly Func<bool> _withContext;

        private readonly object _mapLock = new object();
        private Dictionary<string, string> _map = new Dictionary<string, string>(StringComparer.Ordinal);

        private readonly object _seenLock = new object();
        private readonly HashSet<string> _seen = new HashSet<string>(StringComparer.Ordinal);

        private readonly BlockingCollection<Entry> _queue = new BlockingCollection<Entry>(4096);
        private readonly Thread _writer;
        private volatile bool _disposed;

        private FileSystemWatcher _watcher;
        private Timer _debounce;

        public int Count
        {
            get { lock (_mapLock) { return _map.Count; } }
        }

        public TranslationStore(string dictPath, string capturePath,
                                Func<bool> enabled, Func<bool> capture, Func<bool> withContext)
        {
            _dictPath = dictPath;
            _capturePath = capturePath;
            _enabled = enabled;
            _capture = capture;
            _withContext = withContext;

            LoadDictionary();
            LoadSeenCapture();

            _writer = new Thread(WriterLoop) { IsBackground = true, Name = "ro3-es-capture" };
            _writer.Start();
        }

        public string Translate(string value, string context)
        {
            if (string.IsNullOrEmpty(value)) return value;
            if (_enabled != null && !_enabled()) return value;

            if (_map.TryGetValue(value, out string dst) && !string.IsNullOrEmpty(dst))
            {
                return dst;
            }

            // Segundo intento: ignorando espacios exteriores.
            string trimmed = value.Trim();
            if (trimmed.Length != value.Length && _map.TryGetValue(trimmed, out dst) && !string.IsNullOrEmpty(dst))
            {
                return value.Replace(trimmed, dst);
            }

            if (_capture != null && _capture() && HasLetters(value))
            {
                Record(value, context);
            }

            return value;
        }

        public void Record(string source, string context)
        {
            if (string.IsNullOrEmpty(source) || source.Length > 4000) return;

            lock (_seenLock)
            {
                if (!_seen.Add(source)) return;
            }

            var entry = new Entry
            {
                Src = source,
                Dst = string.Empty,
                Status = "new",
                Ctx = (_withContext != null && _withContext()) ? context : null
            };

            if (!_queue.TryAdd(entry))
            {
                lock (_seenLock) { _seen.Remove(source); }
            }
        }

        private void WriterLoop()
        {
            var sb = new StringBuilder();
            foreach (Entry entry in _queue.GetConsumingEnumerable())
            {
                try
                {
                    sb.Clear();
                    sb.Append(JsonConvert.SerializeObject(entry, Formatting.None));
                    sb.Append('\n');
                    File.AppendAllText(_capturePath, sb.ToString(), Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    Ro3EsPlugin.Log?.LogWarning($"No se pudo escribir la captura: {ex.Message}");
                }
            }
        }

        private void LoadDictionary()
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);

            if (!File.Exists(_dictPath))
            {
                Ro3EsPlugin.Log?.LogWarning($"No existe el diccionario: {_dictPath}");
                lock (_mapLock) { _map = map; }
                return;
            }

            int lineNo = 0;
            foreach (string line in File.ReadLines(_dictPath, Encoding.UTF8))
            {
                lineNo++;
                if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#")) continue;

                try
                {
                    var entry = JsonConvert.DeserializeObject<Entry>(line);
                    if (entry == null || string.IsNullOrEmpty(entry.Src)) continue;
                    if (string.IsNullOrEmpty(entry.Dst)) continue;
                    if (string.Equals(entry.Status, "keep", StringComparison.OrdinalIgnoreCase)) continue;

                    map[entry.Src] = entry.Dst;
                }
                catch (Exception ex)
                {
                    Ro3EsPlugin.Log?.LogWarning($"es.jsonl línea {lineNo}: {ex.Message}");
                }
            }

            lock (_mapLock) { _map = map; }
        }

        private void LoadSeenCapture()
        {
            if (!File.Exists(_capturePath)) return;

            try
            {
                foreach (string line in File.ReadLines(_capturePath, Encoding.UTF8))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    try
                    {
                        var obj = JObject.Parse(line);
                        string src = (string)obj["src"];
                        if (!string.IsNullOrEmpty(src))
                        {
                            lock (_seenLock) { _seen.Add(src); }
                        }
                    }
                    catch
                    {
                        // Línea corrupta: se ignora.
                    }
                }
            }
            catch (Exception ex)
            {
                Ro3EsPlugin.Log?.LogWarning($"No se pudo leer la captura previa: {ex.Message}");
            }
        }

        public void Watch()
        {
            try
            {
                string dir = Path.GetDirectoryName(_dictPath);
                string file = Path.GetFileName(_dictPath);
                if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return;

                _watcher = new FileSystemWatcher(dir, file)
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName
                };

                FileSystemEventHandler onChange = (s, e) => ScheduleReload();
                _watcher.Changed += onChange;
                _watcher.Created += onChange;
                _watcher.Renamed += (s, e) => ScheduleReload();
                _watcher.EnableRaisingEvents = true;

                _debounce = new Timer(_ => ReloadNow(), null, Timeout.Infinite, Timeout.Infinite);
            }
            catch (Exception ex)
            {
                Ro3EsPlugin.Log?.LogWarning($"Hot reload no disponible: {ex.Message}");
            }
        }

        private void ScheduleReload()
        {
            try { _debounce?.Change(400, Timeout.Infinite); } catch { }
        }

        private void ReloadNow()
        {
            LoadDictionary();
            Ro3EsPlugin.Log?.LogInfo($"Diccionario recargado: {Count} traducciones.");
        }

        private static bool HasLetters(string value)
        {
            foreach (char c in value)
            {
                if (char.IsLetter(c)) return true;
            }
            return false;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try { _watcher?.Dispose(); } catch { }
            try { _debounce?.Dispose(); } catch { }
            try { _queue.CompleteAdding(); } catch { }
        }
    }
}
