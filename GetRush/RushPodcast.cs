using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace GetRush
{
    internal class RushPodcast
    {
        private string _sUsername;
        private string _sPassword;
        private readonly Logger _mLogger;

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

            using (var client = new HttpClient())
            {
                var byteArray = Encoding.ASCII.GetBytes($"{_sUsername}:{_sPassword}");
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                var response = await client.GetAsync(Settings.FeedUrl);
                var content = response.Content;

                // ... Check Status Code                                
                _mLogger.Info("Response StatusCode: " + (int) response.StatusCode);

                // ... Read the string.
                string result = await content.ReadAsStringAsync();

                // ... Display the result.
                if (result != null && result.Length >= 50)
                {
                    _mLogger.Info(result.Substring(0, 50) + "...");
                }
                return result;
            }
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
            var targetDir = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
            var targetPath = Path.Combine(targetDir, item.Enclosure.Filename);
            _mLogger.Info($"Downloading from {item.Enclosure.Url} to {targetPath}");
            using (var client = new HttpClient())
            {
                try
                {
                    var response = await client.GetAsync(item.Enclosure.Uri);
                    if (response.IsSuccessStatusCode)
                    {
                        using (var reader = await response.Content.ReadAsStreamAsync())
                        {
                            using (var writer = File.Open(targetPath, FileMode.Create))
                            {
                                var buffer = new byte[64 * 1024];
                                var read = 0;
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
                }
                catch (Exception ex)
                {
                    _mLogger.Error(ex);
                }
                return false;
            }
        }
    }
}
