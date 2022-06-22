using CefSharp.WinForms;
using CommentTMDT.Helper;
using CommentTMDT.Model;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CommentTMDT.Controller
{
    class Kidplaza
    {
        private readonly ChromiumWebBrowser _browser = null;
        private HtmlAgilityPack.HtmlDocument _document = new HtmlAgilityPack.HtmlDocument();
        private const string _urlHome = @"https://www.kidsplaza.vn/";
        private readonly Telegram_Helper tgl;
        private uint _start = 0;

        public Kidplaza(ChromiumWebBrowser browser)
        {
            _browser = browser;
            tgl = new Telegram_Helper(Config_System.KEY_BOT);
        }

        public async Task CrawlData()
        {
            uint totalComment = 0;
            MySQL_Helper msql = new MySQL_Helper(Config_System.ConnectionToTableLinkProduct);
            List<(string, string, DateTime)> dataUrl = await msql.GetLinkProductByDomain("https://www.kidsplaza.vn", _start, 100);

            if (!dataUrl.Any())
            {
                _start = 0;
                return;
            }

            /* Next data */
            _start += 100;

            foreach ((string, string, DateTime) item in dataUrl)
            {
                List<CommentModel> data = await GetCommentProduct(item);

                /* Send to kafka */
                if (data.Any())
                {
                    foreach (CommentModel item1 in data)
                    {
                        string json = JsonSerializer.Serialize(item1);
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
            await tgl.SendMessageToChannel($"Done {totalComment} comment of kidsplaza", Config_System.ID_TELEGRAM_BOT_GROUP_COMMENT_ECO);
        }

        private async ValueTask<List<CommentModel>> GetCommentProduct((string, string, DateTime) data)
        {
            List<CommentModel> lstCmtJson = new List<CommentModel>();

            try
            {
                await _browser.LoadUrlAsync(data.Item2);
                await Task.Delay(20_000);

                string html = await Util.GetBrowserSource(_browser);
                if (html is null)
                {
                    return lstCmtJson;
                }

                _document.LoadHtml(html);
                html = null;

                HtmlNodeCollection divComment = _document.DocumentNode.SelectNodes("//ol[@id='list_review']//li[contains(@class, 'item review-item')]//div[@class='review-details']");
                if (divComment == null)
                {
                    return null;
                }

                foreach (HtmlNode item in divComment)
                {
                    DateTime commentTime = GetDate(item.SelectSingleNode(".//time")?.Attributes["datetime"]?.Value);

                    if (commentTime >= data.Item3)
                    {
                        CommentModel cmtJson = new CommentModel();

                        cmtJson.UserComment = Util.RemoveSpecialCharacter(item.SelectSingleNode("./div[@class='review-author']/strong")?.InnerText);
                        cmtJson.Comment = Util.RemoveSpecialCharacter(item.SelectSingleNode("./div[@class='review-content']")?.InnerText);

                        cmtJson.UrlProduct = data.Item2;
                        cmtJson.Domain = _urlHome;

                        cmtJson.PostDate = DateTime.Now;
                        cmtJson.PostDateTimeStamp = Util.ConvertDateTimeToTimeStamp(cmtJson.PostDate);

                        cmtJson.CommentDate = commentTime;
                        cmtJson.CommentDateTimeStamp = Util.ConvertDateTimeToTimeStamp(commentTime);

                        cmtJson.Id = Util.ConvertStringtoMD5(data.Item2 + cmtJson.CommentDateTimeStamp + "-1");
                        cmtJson.IdComment = -1;

                        lstCmtJson.Add(cmtJson);
                    }
                }
            }
            catch (Exception) { }

            return lstCmtJson;
        }

        private DateTime GetDate(string strDate, string pattern = @"\d{2}\/\d{2}\/\d{4}", string formatTypeDate = @"dd/MM/yyyy")
        {
            try
            {
                if (Regex.IsMatch(strDate, pattern))
                {
                    Regex rg = new Regex(pattern);
                    MatchCollection matched = rg.Matches(strDate);

                    return DateTime.ParseExact(matched[0].Value, formatTypeDate, CultureInfo.InvariantCulture);
                }
                else
                {
                    byte numberDay = (byte)(Util.convertTextToNumber(strDate));

                    if (strDate.Contains("Hôm qua"))
                    {
                        return DateTime.Today.AddDays(-1);
                    }
                    else if (strDate.Contains("giờ trước"))
                    {
                        return DateTime.Now.AddHours(-numberDay);
                    }
                    else if (strDate.Contains("ngày trước"))
                    {
                        return DateTime.Today.AddDays(-numberDay);
                    }
                    else if (strDate.Contains("tháng trước"))
                    {
                        return DateTime.Today.AddMonths(-numberDay);
                    }
                    else if (strDate.Contains("năm trước"))
                    {
                        return DateTime.Today.AddYears(-numberDay);
                    }

                    return DateTime.Now;
                }
            }
            catch (Exception)
            {
                return new DateTime();
            }
        }
    }
}
