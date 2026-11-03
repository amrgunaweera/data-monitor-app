using System;
using System.Collections.Generic;

namespace NetPulse {
    public class InterfaceInfo {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string InterfaceType { get; set; } // "Wi-Fi" or "Ethernet"
        public bool IsConnected { get; set; }
        public string IPv4Address { get; set; }
        public string MacAddress { get; set; }
        public string LinkSpeed { get; set; }
        public string ConnectedNetworkName { get; set; }

        public double CurrentDownloadSpeed { get; set; } // Bytes/sec
        public double CurrentUploadSpeed { get; set; }   // Bytes/sec
        public double PeakDownloadSpeed { get; set; }
        public double PeakUploadSpeed { get; set; }

        public double SessionDownloaded { get; set; }    // Total bytes
        public double SessionUploaded { get; set; }      // Total bytes

        // Rolling 60-second speed history
        public double[] DownloadHistory { get; private set; }
        public double[] UploadHistory { get; private set; }

        // Internal tracking
        public long LastBytesReceived { get; set; }
        public long LastBytesSent { get; set; }
        public DateTime LastUpdated { get; set; }
        public bool HasBaseline { get; set; }

        public const int HistoryLength = 60;

        public InterfaceInfo() {
            DownloadHistory = new double[HistoryLength];
            UploadHistory = new double[HistoryLength];
            IPv4Address = "Not Assigned";
            MacAddress = "Unknown";
            LinkSpeed = "Unknown";
        }

        public void PushSpeed(double dlSpeed, double ulSpeed) {
            CurrentDownloadSpeed = dlSpeed;
            CurrentUploadSpeed = ulSpeed;

            if (dlSpeed > PeakDownloadSpeed) PeakDownloadSpeed = dlSpeed;
            if (ulSpeed > PeakUploadSpeed) PeakUploadSpeed = ulSpeed;

            // Shift left and append
            for (int i = 0; i < HistoryLength - 1; i++) {
                DownloadHistory[i] = DownloadHistory[i + 1];
                UploadHistory[i] = UploadHistory[i + 1];
            }
            DownloadHistory[HistoryLength - 1] = dlSpeed;
            UploadHistory[HistoryLength - 1] = ulSpeed;
        }

        public void ResetSession() {
            SessionDownloaded = 0;
            SessionUploaded = 0;
            PeakDownloadSpeed = 0;
            PeakUploadSpeed = 0;
            Array.Clear(DownloadHistory, 0, HistoryLength);
            Array.Clear(UploadHistory, 0, HistoryLength);
        }
    }

    public class GlobalStats {
        public double TotalDownloadSpeed { get; set; }
        public double TotalUploadSpeed { get; set; }
        public double PeakDownloadSpeed { get; set; }
        public double PeakUploadSpeed { get; set; }
        public double TotalSessionDownloaded { get; set; }
        public double TotalSessionUploaded { get; set; }

        public double[] DownloadHistory { get; private set; }
        public double[] UploadHistory { get; private set; }

        public const int HistoryLength = 60;

        public GlobalStats() {
            DownloadHistory = new double[HistoryLength];
            UploadHistory = new double[HistoryLength];
        }

        public void PushSpeed(double dlSpeed, double ulSpeed) {
            TotalDownloadSpeed = dlSpeed;
            TotalUploadSpeed = ulSpeed;

            if (dlSpeed > PeakDownloadSpeed) PeakDownloadSpeed = dlSpeed;
            if (ulSpeed > PeakUploadSpeed) PeakUploadSpeed = ulSpeed;

            for (int i = 0; i < HistoryLength - 1; i++) {
                DownloadHistory[i] = DownloadHistory[i + 1];
                UploadHistory[i] = UploadHistory[i + 1];
            }
            DownloadHistory[HistoryLength - 1] = dlSpeed;
            UploadHistory[HistoryLength - 1] = ulSpeed;
        }

        public void ResetSession() {
            TotalSessionDownloaded = 0;
            TotalSessionUploaded = 0;
            PeakDownloadSpeed = 0;
            PeakUploadSpeed = 0;
            Array.Clear(DownloadHistory, 0, HistoryLength);
            Array.Clear(UploadHistory, 0, HistoryLength);
        }
    }
}

