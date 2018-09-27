using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Xml.Serialization;
using NLog;

namespace GetRush
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Logger _mLogger;
        public MainWindow()
        {
            _mLogger = LogManager.GetLogger("MainWindow");
            InitializeComponent();
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            UpdateLastUpdateTextBlock();
            var dt = new DispatcherTimer() {Interval = TimeSpan.FromMilliseconds(1000), IsEnabled = true};
            dt.Tick += (o, args) =>
            {
                dt.Stop();
                GetFeedMethod();
            };
            dt.Start();
        }

        private void UpdateLastUpdateTextBlock()
        {
            var dtLast = Settings.LastDownloadTimestamp.ToLocalTime();
            LastUpdateTextBlock.Text = $"Last Podcast Update: {dtLast:f}";
            _mLogger.Info($"Setting Last Podcast Update to {dtLast:f}");
        }

        private void ClearUpdateStatusTextBox()
        {
            _mLogger.Info("Clearing update status text");
            UpdateStatusTextBox.Text = "";
        }

        private void AppendToUpdateStatusTextBox(string text)
        {
            _mLogger.Info(text);
            UpdateStatusTextBox.Text += text + "\r\n";
            UpdateStatusTextBox.ScrollToEnd();
        }

        private async void GetFeedButton_Click(object sender, RoutedEventArgs e)
        {
            await GetTheFeed();
        }

        private async void GetFeedMethod()
        {
            await GetTheFeed();
        }

        private async Task SavePodcastRss(string rssFeed)
        {
            var path =
                $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\KeedbaSoft\\logs\\Feeds";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            var currentTime = DateTime.Now;

            const string dtFormat = "yyyy-MM-ddTHH.mm.ss";
            var filename = $"{currentTime.ToString(dtFormat)}RushFeed.xml";
            using (TextWriter writer = new StreamWriter(Path.Combine(path, filename)))
            {
                await writer.WriteAsync(rssFeed);
                _mLogger.Debug($"Saved RSS feed XML to {filename}");
            }
        }

        private async Task GetTheFeed()
        { 
            _mLogger.Trace("Entering GetTheFeed");
            GetFeedButton.IsEnabled = false;
            ClearUpdateStatusTextBox();
            var podcast = new RushPodcast();
            AppendToUpdateStatusTextBox("Downloading Rush Podcast RSS Feed");
            var result = await podcast.GetPodcast();
            try
            {
                var serializer = new XmlSerializer(typeof(RushFeed));

                RushFeed feed = null;
                using (var reader = new StringReader(result))
                {
                    feed = (RushFeed) serializer.Deserialize(reader);
                }
                AppendToUpdateStatusTextBox($"Got Rush Podcast RSS Feed \"{feed.Channel.Title}\"");
                await SavePodcastRss(result);
                var dtLast = Settings.LastDownloadTimestamp;
                var feedList = new List<RssItem>();
                foreach (var item in feed.Channel.Item)
                {
                    if (item.PubDate <= dtLast) break;
                    feedList.Add(item);
                }

                if (feedList.Count > 0)
                {
                    feedList.Sort((f1,f2)=>string.Compare(f1.Title, f2.Title, StringComparison.InvariantCulture));

                    feedList.Sort((f1, f2) =>
                    {
                        var f1d = DateTime.Parse(f1.PubDate.ToShortDateString());
                        var f2d = DateTime.Parse(f2.PubDate.ToShortDateString());
                        if (f1d == f2d) return 0;
                        if (f1d < f2d) return -1;
                        return 1;
                    });
                    feedList.Reverse();
                    var feedStack = new Stack<RssItem>();
                    foreach (var item in feedList)
                    {
                        feedStack.Push(item);
                    }
                    var sb = new StringBuilder();
                    var downloadFailed = false;
                    while (feedStack.Count > 0 && !downloadFailed)
                    {
                        var item = feedStack.Pop();
                        sb.AppendLine($"Got {item.Title}");
                        var retries = 3;
                        do
                        {
                            --retries;
                            //AppendToUpdateStatusTextBox(
                            //    $"Downloading {System.IO.Path.GetFileName(System.Net.WebUtility.UrlDecode(item.Enclosure.Url))}");
                            var targetPath = $"Rush Limbaugh - {item.Title}{Path.GetExtension(item.Enclosure.Filename)}".Replace(",", "");
                            AppendToUpdateStatusTextBox($"Downloading {targetPath}");
                            var success = await podcast.DownloadItem(item);
                            if (success)
                            {
                                break;
                            }
                            if (retries > 0)
                            {
                                AppendToUpdateStatusTextBox($"Download failed.  retrying in 60 seconds...");
                                await Task.Delay(60 * 1000);
                            }
                            else
                            {
                                downloadFailed = true;
                            }
                        } while (retries > 0);
                    }
                    if (!downloadFailed)
                    {
                        UpdateLastUpdateTextBlock();
                        sb.AppendLine();
                        sb.AppendLine("Open 'Music' Folder?");
                        var bOpenMusic = MessageBoxEx.Show(this, sb.ToString(), "Download complete", MessageBoxButton.YesNo);
                        if (bOpenMusic == MessageBoxResult.Yes)
                        {
                            var path = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
                            OpenFolderInExplorer(path);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _mLogger.Error($"podcast.GetPodcast returned:\n {result}");

                _mLogger.Error(ex);
            }
            GetFeedButton.IsEnabled = true;
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // Instantiate the dialog box
            var dlg = new SettingsDialog {Owner = this};

            // Configure the dialog box
            //dlg.DocumentMargin = this.documentTextBox.Margin;

            // Open the dialog box modally 
            dlg.ShowDialog();
        }

        void OpenFolderInExplorer(string path)
        {
            try
            {
                Win32Calls.ShellExecute(IntPtr.Zero, "open", path, null, null, ShowCommands.SW_SHOWDEFAULT);
            }
            catch (Exception e)
            {
                _mLogger.Warn($"OpenFolderInExplorer : {e}");
            }
        }

    }

    internal class Win32Calls
    {
        [DllImport("shell32.dll")]
        public static extern IntPtr ShellExecute(IntPtr hwnd, string lpOperation, string lpFile, string lpParameters, string lpDirectory, ShowCommands nShowCmd);
    }

    public enum ShowCommands : int
    {
        SW_HIDE = 0,
        SW_SHOWNORMAL = 1,
        SW_NORMAL = 1,
        SW_SHOWMINIMIZED = 2,
        SW_SHOWMAXIMIZED = 3,
        SW_MAXIMIZE = 3,
        SW_SHOWNOACTIVATE = 4,
        SW_SHOW = 5,
        SW_MINIMIZE = 6,
        SW_SHOWMINNOACTIVE = 7,
        SW_SHOWNA = 8,
        SW_RESTORE = 9,
        SW_SHOWDEFAULT = 10,
        SW_FORCEMINIMIZE = 11,
        SW_MAX = 11
    }

}
