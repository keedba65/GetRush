using Microsoft.Win32;
using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace GetRush
{
    internal class Settings
    {
        private const string Url = "https://rss.premiereradio.net/podcast/rushlimb.xml";
        private const string RegKey = @"HKEY_CURRENT_USER\SOFTWARE\KeedbaSoft\GetRush";

        // abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ
        //           111111111122222222223333333333444444444455
        // 0123456789012345678901234567890123456789012345678901
        // RushLimbaugh
        // 421 3 1  2  
        // 308778210067
        // 43, 20, 18, 7, 37, 8, 12, 1, 0, 20, 6, 7
        private static readonly byte[] SAditionalEntropy = { 43, 20, 18, 7, 37, 8, 12, 1, 0, 20, 6, 7 };

        private static string Protect(string strData)
        {
            var data = Encoding.UTF8.GetBytes(strData);
            try
            {
                var encodedData = ProtectedData.Protect(data, SAditionalEntropy, DataProtectionScope.CurrentUser);
                return Convert.ToBase64String(encodedData);
            }
            catch
            {
                return strData;
            }
        }

        private static string UnProtect(string encodedStringData)
        {
            var encodedData = Convert.FromBase64String(encodedStringData);
            try
            {
                var data = ProtectedData.Unprotect(encodedData, SAditionalEntropy, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(data);
            }
            catch
            {
                return encodedStringData;
            }
        }

        public static string FeedUrl => Url;

        public static string Username
        {
            get => Registry.GetValue(RegKey, "Username", "") as string;
            set => Registry.SetValue(RegKey, "Username", value);
        }

        public static string Password
        {
            get
            {
                var pwd = Registry.GetValue(RegKey, "Password", "") as string;
                if(!string.IsNullOrEmpty(pwd))
                {
                    pwd = UnProtect(pwd);
                }
                return pwd;
            }
            set
            {
                var pwd = value;
                if(!string.IsNullOrEmpty(pwd))
                {
                    pwd = Protect(pwd);
                }
                Registry.SetValue(RegKey, "Password", pwd ?? "");
            }
        }

        public static DateTime LastDownloadTimestamp
        {
            get
            {
                var sCurrentTimestamp = Registry.GetValue(RegKey, "LastDownloadTimestamp", "") as string;
                var dtLast = DateTime.MinValue;
                if (!string.IsNullOrWhiteSpace(sCurrentTimestamp) &&
                    DateTime.TryParse(sCurrentTimestamp, out dtLast))
                {
                    // Nothing, just want to parse dtLast
                }
                return dtLast;
            }
            set
            {
                var enUs = CultureInfo.CreateSpecificCulture("en-US");
                var sCurrentTimestamp = value.ToString("s", enUs);
                Registry.SetValue(RegKey, "LastDownloadTimestamp", sCurrentTimestamp);
            }
        }
    }
}
