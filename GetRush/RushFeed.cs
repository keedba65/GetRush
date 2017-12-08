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
        [XmlElement("title")]
        public string Title { get; set; }
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
                DateTime result = DateTime.MinValue;
                var bHasUtc = PubDateText.Contains("UTC");
                var bHasPzzzz = PubDateText.Contains("+0000");
                if (bHasUtc) result = ConvertPubDateUtcFormat();
                else if (bHasPzzzz) result = ConvertPubDatePzzzzFormat();
                return result;
             }
        }

        private DateTime ConvertPubDatePzzzzFormat()
        {
            string dateText = PubDateText;
            var index = dateText.IndexOf("+0000", StringComparison.Ordinal);
            dateText = dateText.Substring(0, index - 1);
            // Time already in UTC!!!
            DateTime.TryParse(dateText, out var result);
            return result;
        }
        private DateTime ConvertPubDateUtcFormat()
        {
            CultureInfo enUs = new CultureInfo("en-US");
            const string dtFormat = "ddd, d MMM yyyy hh:mm:ss UTC";
            var succeeded = DateTime.TryParseExact(PubDateText, dtFormat, enUs, DateTimeStyles.AllowWhiteSpaces ,out var result);
            if (succeeded)
            {
                // Rush timestamp is actually 1 hour after the end of the show (e.g. 3:00 pm is given as 4:00)
                // Adjust to end of the show (Rush shows are noon to 3pm EST (12:00:00-15:00:00))
                result -= TimeSpan.FromHours(1);
                // Rush UTC is actually US Eastern Local time PM without designater
                // First update to 24 hour clock (e.g. 3:00:00 should be 15:00:00)
                result += TimeSpan.FromHours(12);
                // Next adjust to UTC, from ET (where Rush broadcasts from)
                var etZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                result = TimeZoneInfo.ConvertTime(result, etZone, TimeZoneInfo.Utc);
                return result;
            }
            return DateTime.MinValue;
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
