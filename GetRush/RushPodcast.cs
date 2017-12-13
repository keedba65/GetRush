using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace GetRush
{
    class RushPodcast
    {
        private string _sUsername;
        private string _sPassword;
        private Logger _mLogger;

        public RushPodcast()
        {
            _mLogger = LogManager.GetLogger("RushPodcast");
        }

        private void GetCredentials()
        {
            if (string.IsNullOrWhiteSpace(_sUsername))
            {
                _sUsername = Settings.Username;
            }

            if (string.IsNullOrWhiteSpace(_sPassword))
            {
                _sPassword = Settings.Password;
            }
        }

        public async Task<string> GetPodcast()
        {
            GetCredentials();
            if (string.IsNullOrWhiteSpace(_sUsername) || string.IsNullOrWhiteSpace(_sPassword)) return "";

            HttpClientHandler handler = new HttpClientHandler();
            HttpClient client = new HttpClient();
            var byteArray = Encoding.ASCII.GetBytes($"{_sUsername}:{_sPassword}");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

            HttpResponseMessage response = await client.GetAsync(Settings.FeedURL);
            HttpContent content = response.Content;

            // ... Check Status Code                                
            _mLogger.Info("Response StatusCode: " + (int)response.StatusCode);

            // ... Read the string.
            string result = await content.ReadAsStringAsync();

            // ... Display the result.
            if (result != null &&
            result.Length >= 50)
            {
                _mLogger.Info(result.Substring(0, 50) + "...");
            }
            return result;
        }

        private void UpdateLastDownloadTimestamp(RssItem item)
        {
            if(item.PubDate > Settings.LastDownloadTimestamp)
            {
                Settings.LastDownloadTimestamp = item.PubDate;
            }
        }

        public async Task<bool> DownloadItem(RssItem item)
        {
            string targetDir = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
            string targetPath = Path.Combine(targetDir, item.Enclosure.filename);
            _mLogger.Info($"Downloading from {item.Enclosure.Url} to {targetPath}");
            HttpClientHandler handler = new HttpClientHandler();
            HttpClient client = new HttpClient();
            try
            {
                HttpResponseMessage response = await client.GetAsync(item.Enclosure.Uri);
                if (response.IsSuccessStatusCode)
                {
                    using (var reader = await response.Content.ReadAsStreamAsync())
                    {
                        using (var writer = File.Open(targetPath, FileMode.Create))
                        {
                            byte[] buffer = new byte[64 * 1024];
                            int read = 0;
                            do
                            {
                                read = await reader.ReadAsync(buffer, 0, buffer.Length);
                                if (read > 0)
                                {
                                    await writer.WriteAsync(buffer, 0, read);
                                }
                            } while (read > 0);
                        }
                    }
                    UpdateLastDownloadTimestamp(item);
                    return true;
                }
            } catch(Exception ex)
            {
                _mLogger.Error(ex);
            }
            return false;
        }
    }
}
