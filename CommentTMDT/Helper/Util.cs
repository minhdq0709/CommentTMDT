using CefSharp;
using CefSharp.WinForms;
using Confluent.Kafka;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CommentTMDT.Helper
{
    public static class Util
    {
        public static readonly JsonSerializerOptions opt = new JsonSerializerOptions()
        {
            Encoder = JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All),
            WriteIndented = true
        };

        private static string _topicTableName = "ecommerce-crawler-comment";
        private static string SERVER_LINK = "10.3.48.81:9092,10.3.48.90:9092,10.3.48.91:9092";

        public static bool DatesAreInTheSameWeek(DateTime startDate, DateTime endDate)
        {
            if (startDate.Year == 1 || endDate.Year == 1)
            {
                return false;
            }

            byte weekOfStartDate = (byte)(startDate.DayOfYear / 7);
            byte weekOfEndDate = (byte)(endDate.DayOfYear / 7);

            return (startDate.Year == endDate.Year) && (weekOfStartDate == weekOfEndDate);
        }

        public static async Task<string> EvaluateJavaScriptSync(string jsScript, ChromiumWebBrowser browser)
        {
            string jsonFromJS = null;
            if (browser.CanExecuteJavascriptInMainFrame && !String.IsNullOrEmpty(jsScript))
            {
                JavascriptResponse result = await browser.EvaluateScriptAsync(jsScript);

                if (result.Success)
                {
                    jsonFromJS = "";

                    if (result.Result != null)
                    {
                        jsonFromJS = result.Result.ToString();
                    }
                }
            }
            return jsonFromJS;
        }

        public static long convertTextToNumber(string text)
        {
            if (String.IsNullOrEmpty(text))
            {
                return 0;
            }

            const string regexRemoveText = @"\D";
            return long.Parse(Regex.Replace(text, regexRemoveText, ""));
        }

        public static long GetNumberDay(string text)
        {
            if (String.IsNullOrEmpty(text))
            {
                return 0;
            }

            Regex regex = new Regex(@"(\d+)(?= )");
            return long.Parse(regex.Match(text).Value);
        }

        public static async Task<string> GetBrowserSource(ChromiumWebBrowser browser)
        {
            return await browser.GetMainFrame().GetSourceAsync();
        }

        public static int InsertPost(string messagejson)
        {
            try
            {
                var config = new ProducerConfig
                {
                    BootstrapServers = SERVER_LINK,
                    ClientId = Dns.GetHostName(),
                    Partitioner = Confluent.Kafka.Partitioner.Random
                };

                using (var producer = new ProducerBuilder<string, string>(config).Build())
                {
                    producer.Produce(_topicTableName, new Message<string, string> { Value = messagejson });
                    return 1;
                }
            }
            catch (Exception ex) { string mes = ex.Message; }

            return 0;
        }

        public static string ConvertStringtoMD5(string strword)
        {
            if (String.IsNullOrEmpty(strword))
            {
                return "";
            }

            MD5 md5 = MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(strword);
            byte[] hash = md5.ComputeHash(inputBytes);
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("x2"));
            }

            return sb.ToString();
        }

        public static double ConvertDateTimeToTimeStamp(DateTime value)
        {
            if(value.Year == 1)
            {
                return 0;
            }

            long epoch = (value.Ticks - 621355968000000000) / 10000000;
            return epoch;
        }

        public static string ConvertNumberToTypeMoney(ulong data)
        {
            return data.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("de"));
        }

        public static string GetDomainFromurl(string url)
        {
            return Regex.Match(url, @"https://(.+).(vn|com)").Value;
        }

        /// <summary>
        /// Delete old data and write new data to file
        /// </summary>
        public static bool WriteFile(string pathFile, ref List<string> urlMd5)
        {
            try
            {
                File.WriteAllLines(pathFile, urlMd5.ToArray());
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public static bool WriteFileNotDeleteOldData(string pathFile, ref List<string> data)
        {
            try
            {
                File.AppendAllLines(pathFile, data.ToArray());
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public static List<string> ReadFileToList(string path)
        {
            List<string> allLinesText = new List<string>();
            if (File.Exists(path))
            {
                allLinesText = File.ReadAllLines(path).ToList();
            }

            return allLinesText;
        }

        public static Dictionary<string, string> ReadFileToHashMap(string path)
        {
            Dictionary<string, string> myTable = new Dictionary<string, string>();
            List<string> lstUrl = ReadFileToList(path);

            if (lstUrl.Count > 0)
            {
                myTable = lstUrl.ToDictionary(x => x, x => "");
            }

            return myTable;
        }

        public static string RemoveSpecialCharacter(string text)
        {
            if (String.IsNullOrEmpty(text))
            {
                return "";
            }

            return Regex.Replace(text, @"\t|\n|\r|\s+", " ");
        }

        public static uint GetValueParam(ref string url, string regex)
        {
            int data = 0;
            Regex rg = new Regex(regex);
            MatchCollection matchedAuthors = rg.Matches(url);

            Int32.TryParse(matchedAuthors[0].Value, out data);
            return (uint)data;
        }

        public static List<string> GetPathFileInFolder(string pathFolder)
        {
            List<string> lstPath = new List<string>();

            try
            {
                lstPath = Directory.GetFiles(pathFolder, "*.txt").ToList();
                return lstPath;
            }
            catch (Exception ex)
            {
                return lstPath;
            }
        }

        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }
    }
}
