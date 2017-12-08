using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace GetRush
{
    class Settings
    {
        const string url = "https://rss.premiereradio.net/podcast/rushlimb.xml";
        const string regKey = @"HKEY_CURRENT_USER\SOFTWARE\KeedbaSoft\GetRush";

        // abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ
        //           111111111122222222223333333333444444444455
        // 0123456789012345678901234567890123456789012345678901
        // RushLimbaugh
        // 421 3 1  2  
        // 308778210067
        // 43, 20, 18, 7, 37, 8, 12, 1, 0, 20, 6, 7
        static byte[] s_aditionalEntropy = { 43, 20, 18, 7, 37, 8, 12, 1, 0, 20, 6, 7 };

        private static string Protect(string strData)
        {
            byte[] data = Encoding.UTF8.GetBytes(strData);
            try
            {
                var encodedData = ProtectedData.Protect(data, s_aditionalEntropy, DataProtectionScope.CurrentUser);
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
                var data = ProtectedData.Unprotect(encodedData, s_aditionalEntropy, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(data);
            }
            catch
            {
                return encodedStringData;
            }
        }

        public static string FeedURL { get { return url; } }
        public static string Username
        {
            get
            {
                return Registry.GetValue(regKey, "Username", "") as string;
            }
            set
            {
                Registry.SetValue(regKey, "Username", value);
            }
        }

        public static string Password
        {
            get
            {
                var pwd = Registry.GetValue(regKey, "Password", "") as string;
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
                Registry.SetValue(regKey, "Password", pwd);
            }
        }

        public static DateTime LastDownloadTimestamp
        {
            get
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
            set
            {
                var enUS = CultureInfo.CreateSpecificCulture("en-US");
                string sCurrentTimestamp = value.ToString("s", enUS);
                Registry.SetValue(regKey, "LastDownloadTimestamp", sCurrentTimestamp);
            }
        }
    }
}
