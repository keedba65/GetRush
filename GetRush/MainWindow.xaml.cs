using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Serialization;

namespace GetRush
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
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
        }

        private void ClearUpdateStatusTextBox()
        {
            UpdateStatusTextBox.Text = "";
        }

        private void AppendToUpdateStatusTextBox(string text)
        {
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

        private async Task GetTheFeed()
        { 
            GetFeedButton.IsEnabled = false;
            ClearUpdateStatusTextBox();
            var podcast = new RushPodcast();
            AppendToUpdateStatusTextBox("Downloading Rush Podcast RSS Feed");
            var result = await podcast.GetPodcast();
            var serializer = new XmlSerializer(typeof(RushFeed));

            RushFeed feed = null;
            using (var reader = new StringReader(result))
            {
                feed = (RushFeed)serializer.Deserialize(reader);
            }
            AppendToUpdateStatusTextBox($"Got Rush Podcast RSS Feed \"{feed.Channel.Title}\"");
            var dtLast = Settings.LastDownloadTimestamp;
            var feedStack = new Stack<RssItem>();
            foreach (var item in feed.Channel.Item)
            {
                if (item.PubDate <= dtLast) break;
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
                    AppendToUpdateStatusTextBox($"Downloading {System.IO.Path.GetFileName(System.Net.WebUtility.UrlDecode(item.Enclosure.Url))}");
                    var success = await podcast.DownloadItem(item);
                    if (success) { break; }
                    if (retries > 0)
                    {
                        AppendToUpdateStatusTextBox($"Download failed.  retrying in 60 seconds...");
                        await Task.Delay(60 * 1000);
                    } else
                    {
                        downloadFailed = true;
                    }
                } while (retries > 0);
            }
            if (!downloadFailed)
            {
                UpdateLastUpdateTextBlock();
                MessageBoxEx.Show(this, sb.ToString(), "Download complete");
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
    }
}
