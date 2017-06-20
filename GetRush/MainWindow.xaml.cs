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

        private async void GetFeedButton_Click(object sender, RoutedEventArgs e)
        {
            RushPodcast podcast = new RushPodcast();
            string result = await podcast.GetPodcast();
            XmlSerializer serializer = new XmlSerializer(typeof(RushFeed));

            RushFeed feed = null;
            using (StringReader reader = new StringReader(result))
            {
                feed = (RushFeed)serializer.Deserialize(reader);
            }
            DateTime dtLast = podcast.GetLastDownloadTimestamp();
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
                await podcast.DownloadItem(item);
            }
            MessageBox.Show(sb.ToString(), "Download complete");
        }
    }
}
