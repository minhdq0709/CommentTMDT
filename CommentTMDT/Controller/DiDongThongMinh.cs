using CommentTMDT.Helper;
using CommentTMDT.Model;
using HtmlAgilityPack;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace CommentTMDT.Controller
{
    class DiDongThongMinh
    {
        private const string _urlHome = @"https://didongthongminh.vn";
        private readonly HtmlAgilityPack.HtmlDocument _document = new HtmlAgilityPack.HtmlDocument();
        private ConcurrentQueue<(string, string, DateTime)> _myQueue = new ConcurrentQueue<(string, string, DateTime)>();
        private uint start = 0;
        private readonly Telegram_Helper tgl;

        public DiDongThongMinh()
        {
            tgl = new Telegram_Helper(Config_System.KEY_BOT);
        }

        public async Task CrawlData()
        {
            uint count = 0;

            MySQL_Helper msql = new MySQL_Helper(Config_System.ConnectionToTableLinkProduct);
            List<(string, string, DateTime)> dataUrl = await msql.GetLinkProductByDomain(_urlHome, start, 100);
            msql.Dispose();

            if (!dataUrl.Any())
            {
                start = 0;
                return;
            }

            foreach ((string, string, DateTime) item in dataUrl)
            {
                _myQueue.Enqueue(item);
            }

            /* Next data */
            start += 100;

            Task<uint> task1 = GetCommentProduct(4_000);
            Task<uint> task2 = GetCommentProduct(5_000);

            await Task.WhenAll(task1, task2);

            count = task1.Result + task2.Result;

            if (count > 0)
            {
                MySQL_Helper msql1 = new MySQL_Helper(Config_System.ConnectionToTableReportDaily);
                await msql1.InsertToTableReportDaily(_urlHome, count);
                msql1.Dispose();
            }

            await tgl.SendMessageToChannel($"Done {count} comment of DDTM", Config_System.ID_TELEGRAM_BOT_GROUP_COMMENT_ECO);
        }

        private async Task<uint> GetCommentProduct(ushort delay)
        {
            (string, string, DateTime) obj = ("", "", DateTime.Now);
            MySQL_Helper msql = new MySQL_Helper(Config_System.ConnectionToTableLinkProduct);
            uint count = 0;

            while (_myQueue.TryDequeue(out obj))
            {
                await Task.Delay(delay);

                List<CommentModel> lstData = new List<CommentModel>();
                try
                {
                    /* Get poruct id */
                    string productId = "0";
                    string html = await HttpClient_Helper.GetData(obj.Item2);
                    if (html is null)
                    {
                        continue;
                    }

                    _document.LoadHtml(html);
                    html = null;

                    productId = _document.DocumentNode.SelectSingleNode(@"//input[@id='product_id']")?.Attributes["value"]?.Value ?? "0";

                    /* Build param for get request */
                    UriBuilder builder = new UriBuilder(@"https://didongthongminh.vn/index.php");
                    NameValueCollection query = HttpUtility.ParseQueryString(builder.Query);

                    query["id"] = productId;
                    query["task"] = "all_conment";
                    query["raw"] = "1";
                    query["view"] = "product";
                    query["module"] = "products";

                    builder.Query = query.ToString();
                    html = await HttpClient_Helper.GetData(builder.ToString());

                    if (html is null)
                    {
                        continue;
                    }

                    _document.LoadHtml(html);
                    html = null;

                    HtmlNodeCollection lstDiv = _document.DocumentNode.SelectNodes("//div[@class='cf comment-item comment-item-125  comment_level_0 comment_sub_0']");
                    if (lstDiv != null)
                    {
                        foreach (HtmlNode item in lstDiv)
                        {
                            DateTime dateComment = GetDate(item.SelectSingleNode(".//span[@class='date']")?.InnerText);
                            string cmt = item.SelectSingleNode(".//div[@class='comment_content ']")?.InnerText;

                            if (dateComment.Date < obj.Item3.Date || string.IsNullOrEmpty(cmt))
                            {
                                break;
                            }

                            #region user comment
                            CommentModel comment = new CommentModel();

                            comment.UrlProduct = obj.Item2;
                            comment.Domain = _urlHome;

                            comment.PostDate = DateTime.Now;
                            comment.PostDateTimeStamp = Util.ConvertDateTimeToTimeStamp(comment.PostDate);

                            comment.UserComment = item.SelectSingleNode(".//strong[@class='text_b_cm']")?.InnerText;
                            comment.Comment = item.SelectSingleNode(".//div[@class='comment_content ']")?.InnerText;

                            comment.CommentDate = dateComment;
                            comment.CommentDateTimeStamp = Util.ConvertDateTimeToTimeStamp(dateComment);

                            comment.Id = Util.ConvertStringtoMD5(obj.Item2 + comment.CommentDateTimeStamp.ToString() + "-1");

                            lstData.Add(comment);
                            ++count;
                            #endregion

                            #region QTV
                            string cmtAdmin = item.SelectSingleNode(".//div[@class='wrapper-admin-rep']//div[@class='comment_content ']")?.InnerText;
                            if(!String.IsNullOrEmpty(cmtAdmin))
                            {
                                CommentModel qtv = new CommentModel();

                                qtv.UrlProduct = obj.Item2;
                                qtv.Domain = _urlHome;

                                qtv.PostDate = DateTime.Now;
                                qtv.PostDateTimeStamp = Util.ConvertDateTimeToTimeStamp(qtv.PostDate);

                                qtv.UserComment = "QTV";
                                qtv.Comment = cmtAdmin;

                                qtv.CommentDate = dateComment;
                                qtv.CommentDateTimeStamp = Util.ConvertDateTimeToTimeStamp(dateComment);

                                qtv.Id = Util.ConvertStringtoMD5(obj.Item2 + qtv.CommentDateTimeStamp.ToString() + "qtv");

                                lstData.Add(qtv);
                                ++count;
                            }
                            #endregion
                            
                        }
                    }

                }
                catch (Exception) { }

                /* Send to kafka */
                if (lstData.Any())
                {
                    foreach (CommentModel item in lstData)
                    {
                        string json = JsonSerializer.Serialize<CommentModel>(item, Util.opt);
                        Util.InsertPost(json);

                        await Task.Delay(50);
                    }
                }

                lstData.Clear();
                lstData.TrimExcess();

                await msql.UpdateTimeGetComment(obj.Item1);
                await Task.Delay(1_000);
            }

            msql.Dispose();
            return count;
        }

        protected DateTime GetDate(string strDate, string pattern = @"\d{2}\/\d{2}\/\d{4}", string formatTypeDate = @"dd/MM/yyyy")
        {
            try
            {
                Regex rg = new Regex(pattern);
                MatchCollection matched = rg.Matches(strDate);

                return DateTime.ParseExact(matched[0].Value, formatTypeDate, CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                return new DateTime();
            }
        }
    }
}
