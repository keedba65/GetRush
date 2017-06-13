using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace GetRush
{
    class Program
    {
        static async void Doit()
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
            foreach(var item in feed.Channel.Item)
            {
                if (item.PubDate <= dtLast) break;
                feedStack.Push(item);
            }
            while(feedStack.Count > 0)
            {
                var item = feedStack.Pop();
                await podcast.DownloadItem(item);
            }
            Console.WriteLine(result);
        }
        static void Main(string[] args)
        {
            Doit();
            Console.ReadLine();
        }
    }
}
