using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace NetPulse {
    public static class VisitedSitesHelper {
        // ── Win32 SQLite API (winsqlite3.dll built into Windows 10 & 11) ──
        [DllImport("winsqlite3.dll", EntryPoint = "sqlite3_open", CallingConvention = CallingConvention.Cdecl)]
        private static extern int sqlite3_open(byte[] filename, out IntPtr db);

        [DllImport("winsqlite3.dll", EntryPoint = "sqlite3_prepare_v2", CallingConvention = CallingConvention.Cdecl)]
        private static extern int sqlite3_prepare_v2(IntPtr db, byte[] zSql, int nByte, out IntPtr ppStmt, IntPtr pzTail);

        [DllImport("winsqlite3.dll", EntryPoint = "sqlite3_step", CallingConvention = CallingConvention.Cdecl)]
        private static extern int sqlite3_step(IntPtr stmt);

        [DllImport("winsqlite3.dll", EntryPoint = "sqlite3_column_text", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr sqlite3_column_text(IntPtr stmt, int iCol);

        [DllImport("winsqlite3.dll", EntryPoint = "sqlite3_column_int", CallingConvention = CallingConvention.Cdecl)]
        private static extern int sqlite3_column_int(IntPtr stmt, int iCol);

        [DllImport("winsqlite3.dll", EntryPoint = "sqlite3_column_int64", CallingConvention = CallingConvention.Cdecl)]
        private static extern long sqlite3_column_int64(IntPtr stmt, int iCol);

        [DllImport("winsqlite3.dll", EntryPoint = "sqlite3_finalize", CallingConvention = CallingConvention.Cdecl)]
        private static extern int sqlite3_finalize(IntPtr stmt);

        [DllImport("winsqlite3.dll", EntryPoint = "sqlite3_close_v2", CallingConvention = CallingConvention.Cdecl)]
        private static extern int sqlite3_close_v2(IntPtr db);

        private const int SQLITE_ROW = 100;
        private const int SQLITE_DONE = 101;

        private static readonly DateTime ChromiumEpoch = new DateTime(1601, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime FirefoxEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private struct BrowserSource {
            public string BrowserName;
            public string DatabasePath;
            public bool IsFirefox;
        }

        private static string Utf8PtrToString(IntPtr ptr) {
            if (ptr == IntPtr.Zero) return "";
            int len = 0;
            while (Marshal.ReadByte(ptr, len) != 0) len++;
            if (len == 0) return "";
            byte[] buffer = new byte[len];
            Marshal.Copy(ptr, buffer, 0, len);
            return Encoding.UTF8.GetString(buffer);
        }

        public static string ExtractDomain(string url) {
            if (string.IsNullOrEmpty(url)) return "Unknown";
            try {
                Uri uri = new Uri(url);
                string host = uri.Host;
                if (host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) && host.Length > 4) {
                    host = host.Substring(4);
                }
                return string.IsNullOrEmpty(host) ? "Unknown" : host;
            } catch {
                string clean = url;
                int idx = clean.IndexOf("://");
                if (idx >= 0) clean = clean.Substring(idx + 3);
                idx = clean.IndexOfAny(new char[] { '/', '?', ':', '#' });
                if (idx >= 0) clean = clean.Substring(0, idx);
                if (clean.StartsWith("www.", StringComparison.OrdinalIgnoreCase) && clean.Length > 4) {
                    clean = clean.Substring(4);
                }
                return string.IsNullOrEmpty(clean) ? "Unknown" : clean;
            }
        }

        public static DateTime FromChromiumTimestamp(long timestamp) {
            if (timestamp <= 0) return DateTime.MinValue;
            try {
                return ChromiumEpoch.AddMilliseconds(timestamp / 1000.0).ToLocalTime();
            } catch {
                return DateTime.MinValue;
            }
        }

        public static long ToChromiumTimestamp(DateTime time) {
            try {
                DateTime utc = time.ToUniversalTime();
                if (utc < ChromiumEpoch) return 0;
                return (long)(utc - ChromiumEpoch).TotalMilliseconds * 1000L;
            } catch {
                return 0;
            }
        }

        public static DateTime FromFirefoxTimestamp(long timestamp) {
            if (timestamp <= 0) return DateTime.MinValue;
            try {
                return FirefoxEpoch.AddMilliseconds(timestamp / 1000.0).ToLocalTime();
            } catch {
                return DateTime.MinValue;
            }
        }

        public static long ToFirefoxTimestamp(DateTime time) {
            try {
                DateTime utc = time.ToUniversalTime();
                if (utc < FirefoxEpoch) return 0;
                return (long)(utc - FirefoxEpoch).TotalMilliseconds * 1000L;
            } catch {
                return 0;
            }
        }

        private static bool CopyFileSafely(string sourcePath, string destPath) {
            try {
                using (FileStream src = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                using (FileStream dst = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None)) {
                    byte[] buffer = new byte[65536];
                    int read;
                    while ((read = src.Read(buffer, 0, buffer.Length)) > 0) {
                        dst.Write(buffer, 0, read);
                    }
                }

                // Copy WAL file if it exists
                string walSrc = sourcePath + "-wal";
                string walDst = destPath + "-wal";
                if (File.Exists(walSrc)) {
                    try {
                        using (FileStream src = new FileStream(walSrc, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                        using (FileStream dst = new FileStream(walDst, FileMode.Create, FileAccess.Write, FileShare.None)) {
                            byte[] buffer = new byte[65536];
                            int read;
                            while ((read = src.Read(buffer, 0, buffer.Length)) > 0) {
                                dst.Write(buffer, 0, read);
                            }
                        }
                    } catch {}
                }
                return true;
            } catch {
                return false;
            }
        }

        private static List<BrowserSource> DiscoverBrowserDatabases() {
            List<BrowserSource> sources = new List<BrowserSource>();
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            // Microsoft Edge
            AddChromiumSources(sources, "Edge", Path.Combine(localAppData, @"Microsoft\Edge\User Data"));

            // Google Chrome
            AddChromiumSources(sources, "Chrome", Path.Combine(localAppData, @"Google\Chrome\User Data"));

            // Brave Browser
            AddChromiumSources(sources, "Brave", Path.Combine(localAppData, @"BraveSoftware\Brave-Browser\User Data"));

            // Mozilla Firefox
            string ffProfiles = Path.Combine(appData, @"Mozilla\Firefox\Profiles");
            if (Directory.Exists(ffProfiles)) {
                try {
                    string[] dirs = Directory.GetDirectories(ffProfiles);
                    for (int i = 0; i < dirs.Length; i++) {
                        string p = Path.Combine(dirs[i], "places.sqlite");
                        if (File.Exists(p)) {
                            sources.Add(new BrowserSource { BrowserName = "Firefox", DatabasePath = p, IsFirefox = true });
                        }
                    }
                } catch {}
            }

            // Opera Stable & Opera GX
            string opera = Path.Combine(appData, @"Opera Software\Opera Stable\History");
            if (File.Exists(opera)) {
                sources.Add(new BrowserSource { BrowserName = "Opera", DatabasePath = opera, IsFirefox = false });
            }
            string operaGx = Path.Combine(appData, @"Opera Software\Opera GX Stable\History");
            if (File.Exists(operaGx)) {
                sources.Add(new BrowserSource { BrowserName = "Opera GX", DatabasePath = operaGx, IsFirefox = false });
            }

            // Vivaldi
            AddChromiumSources(sources, "Vivaldi", Path.Combine(localAppData, @"Vivaldi\User Data"));

            return sources;
        }

        private static void AddChromiumSources(List<BrowserSource> list, string browserName, string userDataDir) {
            if (!Directory.Exists(userDataDir)) return;
            try {
                string defaultHist = Path.Combine(userDataDir, @"Default\History");
                if (File.Exists(defaultHist)) {
                    list.Add(new BrowserSource { BrowserName = browserName, DatabasePath = defaultHist, IsFirefox = false });
                }

                string[] profileDirs = Directory.GetDirectories(userDataDir, "Profile *");
                for (int i = 0; i < profileDirs.Length; i++) {
                    string p = Path.Combine(profileDirs[i], "History");
                    if (File.Exists(p)) {
                        list.Add(new BrowserSource { BrowserName = browserName, DatabasePath = p, IsFirefox = false });
                    }
                }
            } catch {}
        }

        private static readonly object _cacheLock = new object();
        private static List<VisitedSiteRecord> _cachedSites = null;
        private static DateTime _lastFetchTime = DateTime.MinValue;
        private static TimeRangePreset _cachedPreset;
        private static DateTime _cachedStartDate;
        private static DateTime _cachedEndDate;

        public static void InvalidateCache() {
            lock (_cacheLock) {
                _cachedSites = null;
                _lastFetchTime = DateTime.MinValue;
            }
        }

        public static List<VisitedSiteRecord> GetVisitedSites(TimeRangePreset preset, DateTime startDate, DateTime endDate, int maxResults) {
            lock (_cacheLock) {
                if (_cachedSites != null &&
                    _cachedPreset == preset &&
                    _cachedStartDate == startDate &&
                    _cachedEndDate == endDate &&
                    (DateTime.Now - _lastFetchTime).TotalSeconds < 60) {
                    return new List<VisitedSiteRecord>(_cachedSites);
                }
            }

            Dictionary<string, VisitedSiteRecord> uniqueByUrl = new Dictionary<string, VisitedSiteRecord>(StringComparer.OrdinalIgnoreCase);

            try {
                List<BrowserSource> sources = DiscoverBrowserDatabases();

                DateTime startBound = new DateTime(startDate.Year, startDate.Month, startDate.Day, 0, 0, 0, DateTimeKind.Local);
                DateTime endBound = new DateTime(endDate.Year, endDate.Month, endDate.Day, 23, 59, 59, 999, DateTimeKind.Local);
                bool isAllTime = (preset == TimeRangePreset.AllTime);

                long chromiumStart = isAllTime ? 0 : ToChromiumTimestamp(startBound);
                long chromiumEnd = isAllTime ? long.MaxValue : ToChromiumTimestamp(endBound);

                long firefoxStart = isAllTime ? 0 : ToFirefoxTimestamp(startBound);
                long firefoxEnd = isAllTime ? long.MaxValue : ToFirefoxTimestamp(endBound);

                for (int i = 0; i < sources.Count; i++) {
                    BrowserSource src = sources[i];
                    string tempDb = Path.Combine(Path.GetTempPath(), "netpulse_hist_" + Guid.NewGuid().ToString("N") + ".tmp");

                    try {
                        if (!CopyFileSafely(src.DatabasePath, tempDb)) {
                            continue;
                        }

                        if (src.IsFirefox) {
                            ReadFirefoxHistory(tempDb, src.BrowserName, firefoxStart, firefoxEnd, isAllTime, uniqueByUrl);
                        } else {
                            ReadChromiumHistory(tempDb, src.BrowserName, chromiumStart, chromiumEnd, isAllTime, uniqueByUrl);
                        }
                    } catch {
                        // Tolerate unreadable sources
                    } finally {
                        try {
                            if (File.Exists(tempDb)) File.Delete(tempDb);
                            string wal = tempDb + "-wal";
                            if (File.Exists(wal)) File.Delete(wal);
                            string shm = tempDb + "-shm";
                            if (File.Exists(shm)) File.Delete(shm);
                        } catch {}
                    }
                }
            } catch {
                // If winsqlite3.dll is absent or general error occurs
            }

            List<VisitedSiteRecord> result = new List<VisitedSiteRecord>(uniqueByUrl.Values);
            result.Sort(delegate(VisitedSiteRecord a, VisitedSiteRecord b) {
                // Sort by LastVisitTime descending
                int cmp = b.LastVisitTime.CompareTo(a.LastVisitTime);
                if (cmp == 0) {
                    return b.VisitCount.CompareTo(a.VisitCount);
                }
                return cmp;
            });

            if (maxResults > 0 && result.Count > maxResults) {
                result = result.GetRange(0, maxResults);
            }

            lock (_cacheLock) {
                _cachedSites = new List<VisitedSiteRecord>(result);
                _cachedPreset = preset;
                _cachedStartDate = startDate;
                _cachedEndDate = endDate;
                _lastFetchTime = DateTime.Now;
            }

            return result;
        }

        private static void ReadChromiumHistory(string dbPath, string browserName, long startMicro, long endMicro, bool isAllTime, Dictionary<string, VisitedSiteRecord> destination) {
            IntPtr db;
            byte[] dbBytes = Encoding.UTF8.GetBytes(dbPath + "\0");
            if (sqlite3_open(dbBytes, out db) != 0) return;

            try {
                string query;
                if (isAllTime) {
                    query = "SELECT url, title, visit_count, last_visit_time FROM urls WHERE url LIKE 'http%' ORDER BY last_visit_time DESC LIMIT 1000";
                } else {
                    query = string.Format("SELECT url, title, visit_count, last_visit_time FROM urls WHERE url LIKE 'http%' AND last_visit_time >= {0} AND last_visit_time <= {1} ORDER BY last_visit_time DESC LIMIT 1000",
                        startMicro, endMicro);
                }

                IntPtr stmt;
                byte[] sqlBytes = Encoding.UTF8.GetBytes(query + "\0");
                if (sqlite3_prepare_v2(db, sqlBytes, -1, out stmt, IntPtr.Zero) != 0) return;

                try {
                    while (sqlite3_step(stmt) == SQLITE_ROW) {
                        string url = Utf8PtrToString(sqlite3_column_text(stmt, 0));
                        if (string.IsNullOrEmpty(url) || (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))) {
                            continue;
                        }

                        string title = Utf8PtrToString(sqlite3_column_text(stmt, 1));
                        int visits = sqlite3_column_int(stmt, 2);
                        if (visits <= 0) visits = 1;

                        long lastTimeRaw = sqlite3_column_int64(stmt, 3);
                        DateTime lastVisit = FromChromiumTimestamp(lastTimeRaw);
                        if (lastVisit == DateTime.MinValue) lastVisit = DateTime.Now;

                        string domain = ExtractDomain(url);
                        if (string.IsNullOrEmpty(title)) {
                            title = domain;
                        }

                        VisitedSiteRecord existing;
                        if (destination.TryGetValue(url, out existing)) {
                            existing.VisitCount += visits;
                            if (lastVisit > existing.LastVisitTime) {
                                existing.LastVisitTime = lastVisit;
                                existing.LastVisitTimeFormatted = lastVisit.ToString("MMM dd, yyyy HH:mm");
                                existing.Date = lastVisit.ToString("yyyy-MM-dd");
                            }
                            if ((string.IsNullOrEmpty(existing.Title) || existing.Title == existing.Domain) && !string.IsNullOrEmpty(title) && title != domain) {
                                existing.Title = title;
                            }
                        } else {
                            VisitedSiteRecord rec = new VisitedSiteRecord {
                                Url = url,
                                Title = title,
                                Domain = domain,
                                Browser = browserName,
                                VisitCount = visits,
                                LastVisitTime = lastVisit,
                                LastVisitTimeFormatted = lastVisit.ToString("MMM dd, yyyy HH:mm"),
                                Date = lastVisit.ToString("yyyy-MM-dd")
                            };
                            destination[url] = rec;
                        }
                    }
                } finally {
                    sqlite3_finalize(stmt);
                }
            } finally {
                sqlite3_close_v2(db);
            }
        }

        private static void ReadFirefoxHistory(string dbPath, string browserName, long startMicro, long endMicro, bool isAllTime, Dictionary<string, VisitedSiteRecord> destination) {
            IntPtr db;
            byte[] dbBytes = Encoding.UTF8.GetBytes(dbPath + "\0");
            if (sqlite3_open(dbBytes, out db) != 0) return;

            try {
                string query;
                if (isAllTime) {
                    query = "SELECT url, title, visit_count, last_visit_date FROM moz_places WHERE url LIKE 'http%' ORDER BY last_visit_date DESC LIMIT 1000";
                } else {
                    query = string.Format("SELECT url, title, visit_count, last_visit_date FROM moz_places WHERE url LIKE 'http%' AND last_visit_date >= {0} AND last_visit_date <= {1} ORDER BY last_visit_date DESC LIMIT 1000",
                        startMicro, endMicro);
                }

                IntPtr stmt;
                byte[] sqlBytes = Encoding.UTF8.GetBytes(query + "\0");
                if (sqlite3_prepare_v2(db, sqlBytes, -1, out stmt, IntPtr.Zero) != 0) return;

                try {
                    while (sqlite3_step(stmt) == SQLITE_ROW) {
                        string url = Utf8PtrToString(sqlite3_column_text(stmt, 0));
                        if (string.IsNullOrEmpty(url) || (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))) {
                            continue;
                        }

                        string title = Utf8PtrToString(sqlite3_column_text(stmt, 1));
                        int visits = sqlite3_column_int(stmt, 2);
                        if (visits <= 0) visits = 1;

                        long lastTimeRaw = sqlite3_column_int64(stmt, 3);
                        DateTime lastVisit = FromFirefoxTimestamp(lastTimeRaw);
                        if (lastVisit == DateTime.MinValue) lastVisit = DateTime.Now;

                        string domain = ExtractDomain(url);
                        if (string.IsNullOrEmpty(title)) {
                            title = domain;
                        }

                        VisitedSiteRecord existing;
                        if (destination.TryGetValue(url, out existing)) {
                            existing.VisitCount += visits;
                            if (lastVisit > existing.LastVisitTime) {
                                existing.LastVisitTime = lastVisit;
                                existing.LastVisitTimeFormatted = lastVisit.ToString("MMM dd, yyyy HH:mm");
                                existing.Date = lastVisit.ToString("yyyy-MM-dd");
                            }
                            if ((string.IsNullOrEmpty(existing.Title) || existing.Title == existing.Domain) && !string.IsNullOrEmpty(title) && title != domain) {
                                existing.Title = title;
                            }
                        } else {
                            VisitedSiteRecord rec = new VisitedSiteRecord {
                                Url = url,
                                Title = title,
                                Domain = domain,
                                Browser = browserName,
                                VisitCount = visits,
                                LastVisitTime = lastVisit,
                                LastVisitTimeFormatted = lastVisit.ToString("MMM dd, yyyy HH:mm"),
                                Date = lastVisit.ToString("yyyy-MM-dd")
                            };
                            destination[url] = rec;
                        }
                    }
                } finally {
                    sqlite3_finalize(stmt);
                }
            } finally {
                sqlite3_close_v2(db);
            }
        }

        public static List<DomainUsageRecord> AggregateDomains(List<VisitedSiteRecord> sites) {
            Dictionary<string, DomainUsageRecord> map = new Dictionary<string, DomainUsageRecord>(StringComparer.OrdinalIgnoreCase);
            int totalVisits = 0;

            if (sites != null) {
                for (int i = 0; i < sites.Count; i++) {
                    VisitedSiteRecord s = sites[i];
                    if (s == null) continue;
                    string domain = s.Domain;
                    if (string.IsNullOrEmpty(domain)) domain = "Unknown";

                    totalVisits += s.VisitCount;

                    DomainUsageRecord rec;
                    if (!map.TryGetValue(domain, out rec)) {
                        rec = new DomainUsageRecord {
                            Domain = domain,
                            TotalVisits = 0,
                            UniqueUrlsCount = 0,
                            LastVisited = s.LastVisitTime,
                            PrimaryBrowser = s.Browser
                        };
                        map[domain] = rec;
                    }

                    rec.TotalVisits += s.VisitCount;
                    rec.UniqueUrlsCount++;
                    if (s.LastVisitTime > rec.LastVisited) {
                        rec.LastVisited = s.LastVisitTime;
                        rec.PrimaryBrowser = s.Browser;
                    }
                }
            }

            List<DomainUsageRecord> list = new List<DomainUsageRecord>(map.Values);
            list.Sort(delegate(DomainUsageRecord a, DomainUsageRecord b) {
                return b.TotalVisits.CompareTo(a.TotalVisits);
            });

            for (int i = 0; i < list.Count; i++) {
                list[i].Percentage = (totalVisits > 0) ? ((double)list[i].TotalVisits / totalVisits * 100.0) : 0;
            }

            return list;
        }
    }
}

