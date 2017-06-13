using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GetRush
{
    class RushPodcast
    {
        const string url = "http://rss.premiereradio.net/podcast/rushlimb.xml";
        const string regKey = @"HKEY_CURRENT_USER\SOFTWARE\KeedbaSoft\GetRush";
        string username;
        string password;

        void GetCredentials()
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                username = Registry.GetValue(regKey, "Username", "") as string;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                password = Registry.GetValue(regKey, "Password", "") as string;
            }
        }

        public async Task<string> GetPodcast()
        {
            GetCredentials();
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password)) return "";

            HttpClientHandler handler = new HttpClientHandler();
            HttpClient client = new HttpClient();
            var byteArray = Encoding.ASCII.GetBytes($"{username}:{password}");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

            HttpResponseMessage response = await client.GetAsync(url);
            HttpContent content = response.Content;

            // ... Check Status Code                                
            System.Diagnostics.Debug.WriteLine("Response StatusCode: " + (int)response.StatusCode);

            // ... Read the string.
            string result = await content.ReadAsStringAsync();

            // ... Display the result.
            if (result != null &&
            result.Length >= 50)
            {
                System.Diagnostics.Debug.WriteLine(result.Substring(0, 50) + "...");
            }
            return result;
        }

        public DateTime GetLastDownloadTimestamp()
        {
            string sCurrentTimestamp = Registry.GetValue(regKey, "LastDownloadTimestamp", "") as string;
            DateTime dtLast = DateTime.MinValue;
            if (!string.IsNullOrWhiteSpace(sCurrentTimestamp) &&
                DateTime.TryParse(sCurrentTimestamp, out dtLast))
            {
                // Nothing, just want to parse dtLast
            }
            return dtLast;
        }

        private void UpdateLastDownloadTimestamp(RssItem item)
        {
            var dtLast = GetLastDownloadTimestamp();
            if(item.PubDate > dtLast)
            {
                var enUS = CultureInfo.CreateSpecificCulture("en-US");
                string sCurrentTimestamp = item.PubDate.ToString("s", enUS);
                Registry.SetValue(regKey, "LastDownloadTimestamp", sCurrentTimestamp);
            }
        }
        public async Task DownloadItem(RssItem item)
        {
            string targetDir = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
            string targetPath = Path.Combine(targetDir, item.Enclosure.filename);
            System.Diagnostics.Debug.WriteLine($"Downloading from {item.Enclosure.Url} to {targetPath}");
            HttpClientHandler handler = new HttpClientHandler();
            HttpClient client = new HttpClient();
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
            }
        }
    }
}
