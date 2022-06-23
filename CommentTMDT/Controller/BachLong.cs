using CefSharp.WinForms;
using CommentTMDT.Helper;
using CommentTMDT.Model;
using HtmlAgilityPack;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CommentTMDT.Controller
{
    public class BachLong
    {
        private ChromiumWebBrowser _browser = null;
        private readonly HtmlAgilityPack.HtmlDocument _document = new HtmlAgilityPack.HtmlDocument();
        private const string _urlHome = "https://bachlongmobile.com";
        private uint _start = 0;
        private readonly Telegram_Helper tgl;

        public BachLong(ChromiumWebBrowser browser)
        {
            _browser = browser;
            tgl = new Telegram_Helper(Config_System.KEY_BOT);
        }

        public async Task CrawlData()
        {
            uint totalComment = 0;
            MySQL_Helper msql = new MySQL_Helper(Config_System.ConnectionToTableLinkProduct);
            List<(string, string, DateTime)> dataUrl = await msql.GetLinkProductByDomain("https://bachlongmobile.com", _start, 100);

            if (!dataUrl.Any())
            {
                _start = 0;
                return;
            }

            /* Next data */
            _start += 100;

            foreach ((string, string, DateTime) item in dataUrl)
            {
                List<CommentModel> data = await GetDetailComment(item);

                /* Send to kafka */
                if (data.Any())
                {
                    foreach (CommentModel item1 in data)
                    {
                        string json = JsonSerializer.Serialize<CommentModel>(item1, Util.opt);
                        Util.InsertPost(json);

                        await Task.Delay(50);
                    }
                }

                data.Clear();
                data.TrimExcess();

                await msql.UpdateTimeGetComment(item.Item1);
                totalComment += (uint)data.Count();
            }

            msql.Dispose();
            await tgl.SendMessageToChannel($"Done {totalComment} comment of bachlongmobile", Config_System.ID_TELEGRAM_BOT_GROUP_COMMENT_ECO);
        }

        private async Task<List<CommentModel>> GetDetailComment((string, string, DateTime) data)
        {
            List<CommentModel> lstComment = new List<CommentModel>();

            await _browser.LoadUrlAsync(data.Item2);
            await Task.Delay(20_000);

            string html = await Util.GetBrowserSource(_browser);
            _document.LoadHtml(html);

            HtmlNodeCollection divComment = _document.DocumentNode.SelectNodes("//div[@class='list-content-cmt']/div[@class='content-cmt']/div[not(@class='avatar') and not(@id)]");
            if (divComment != null)
            {
                foreach (HtmlNode item in divComment)
                {
                    CommentModel cmt = new CommentModel();

                    cmt.CommentDate = GetDate(item.SelectSingleNode(".//div[@class='username']/p[@class='time-cmt']")?.InnerText);

                    if (cmt.CommentDate.Year == 1 || data.Item3 < cmt.CommentDate)
                    {
                        continue;
                    }

                    cmt.PostDate = DateTime.Now;
                    cmt.PostDateTimeStamp = Util.ConvertDateTimeToTimeStamp(cmt.PostDate);
                    cmt.Domain = _urlHome;
                    cmt.UrlProduct = data.Item2;
                    cmt.IdComment = -1;
                    cmt.UserComment = item.SelectSingleNode(".//div[@class='username']/label")?.InnerText;
                    cmt.Comment = Util.RemoveSpecialCharacter(item.SelectSingleNode(".//div[@class='content']")?.InnerText);
                    cmt.Id = Util.ConvertStringtoMD5(data.Item2 + cmt.CommentDateTimeStamp + cmt.IdComment);
                    cmt.CommentDateTimeStamp = Util.ConvertDateTimeToTimeStamp(cmt.CommentDate);

                    lstComment.Add(cmt);
                }
            }

            return lstComment;
        }

        private DateTime GetDate(string date, string format = "dd-MM-yyyy")
        {
            try
            {
                if (date.Contains("ngày"))
                {
                    sbyte timeLine = (sbyte)(Util.convertTextToNumber(date) * (-1));
                    return DateTime.Now.AddDays(timeLine);
                }
                else
                {
                    return DateTime.ParseExact(date, format, CultureInfo.InvariantCulture);
                }
            }
            catch (Exception)
            {
                return new DateTime();
            }
        }
    }
}
