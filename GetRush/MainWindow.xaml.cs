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
        }

        void UpdateLastUpdateTextBlock()
        {
            DateTime dtLast = Settings.LastDownloadTimestamp.ToLocalTime();
            LastUpdateTextBlock.Text = $"Last Podcast Update: {dtLast.ToString("f")}";
        }

        void ClearUpdateStatusTextBox()
        {
            UpdateStatusTextBox.Text = "";
        }

        void AppendToUpdateStatusTextBox(string text)
        {
            UpdateStatusTextBox.Text += text + "\r\n";
            UpdateStatusTextBox.ScrollToEnd();
        }

        private async void GetFeedButton_Click(object sender, RoutedEventArgs e)
        {
            GetFeedButton.IsEnabled = false;
            ClearUpdateStatusTextBox();
            RushPodcast podcast = new RushPodcast();
            AppendToUpdateStatusTextBox("Downloading Rush Podcast RSS Feed");
            string result = await podcast.GetPodcast();
            XmlSerializer serializer = new XmlSerializer(typeof(RushFeed));

            RushFeed feed = null;
            using (StringReader reader = new StringReader(result))
            {
                feed = (RushFeed)serializer.Deserialize(reader);
            }
            AppendToUpdateStatusTextBox($"Got Rush Podcast RSS Feed \"{feed.Channel.Title}\"");
            DateTime dtLast = Settings.LastDownloadTimestamp;
            Stack<RssItem> feedStack = new Stack<RssItem>();
            foreach (var item in feed.Channel.Item)
            {
                if (item.PubDate <= dtLast) break;
                feedStack.Push(item);
            }
            StringBuilder sb = new StringBuilder();
            while (feedStack.Count > 0)
            {
                var item = feedStack.Pop();
                sb.AppendLine($"Got {item.Title}");
                AppendToUpdateStatusTextBox($"Downloading {System.IO.Path.GetFileName(System.Net.WebUtility.UrlDecode(item.Enclosure.Url))}");
                await podcast.DownloadItem(item);
            }
            UpdateLastUpdateTextBlock();
            MessageBoxEx.Show(this, sb.ToString(), "Download complete");
            GetFeedButton.IsEnabled = true;
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // Instantiate the dialog box
            SettingsDialog dlg = new SettingsDialog();

            // Configure the dialog box
            dlg.Owner = this;
            //dlg.DocumentMargin = this.documentTextBox.Margin;

            // Open the dialog box modally 
            dlg.ShowDialog();
        }
    }
}
