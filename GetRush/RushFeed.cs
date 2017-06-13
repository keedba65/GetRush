using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace GetRush
{
    [Serializable]
    [XmlRoot("rss")]
    public class RushFeed
    {
        [XmlElement("channel")]
        public RssChannel Channel { get; set; }
    }

    [Serializable]
    public class RssChannel
    {
        [XmlElement("item")]
        public List<RssItem> Item { get; set; }
    }

    [Serializable]
    public class RssItem
    {
        [XmlElement("title")]
        public string Title { get; set; }
        [XmlElement("enclosure")]
        public RssEnclosure Enclosure { get; set; }
        [XmlElement("pubDate")]
        public string PubDateText { get; set; }
        [XmlIgnore]
        public DateTime PubDate
        {
            get
            {
                CultureInfo enUS = new CultureInfo("en-US");
                DateTime result = DateTime.MinValue;
                string dtFormat = "ddd, d MMM yyyy hh:mm:ss UTC";
                var succeeded = DateTime.TryParseExact(PubDateText, dtFormat, enUS, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out result);
                if (succeeded)
                {
                    return result;
                }
                return DateTime.MinValue;
             }
        }
    }

    [Serializable]
    public class RssEnclosure
    {
        [XmlAttribute("type")]
        public string EncType { get; set; }
        [XmlAttribute("url")]
        public string Url { get; set; }

        [XmlIgnore]
        private Uri _uri;
        [XmlIgnore]
        public Uri Uri
        {
            get
            {
                if(_uri == null)
                {
                    _uri = new Uri(Url, UriKind.Absolute);
                }
                return _uri;
            }
        }
        [XmlIgnore]
        public string filename
        {
            get
            {
                return System.IO.Path.GetFileName(Uri.LocalPath);
            }
        }
    }
}
