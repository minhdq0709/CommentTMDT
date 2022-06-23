using CommentTMDT.Helper;
using CommentTMDT.Model;
using HtmlAgilityPack;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CommentTMDT.Controller
{
    class HoangHa
    {
        private readonly HtmlAgilityPack.HtmlDocument _document = new HtmlAgilityPack.HtmlDocument();
        private const string _urlHome = @"https://hoanghamobile.com";
        private const string _urlApiComment = @"https://hoanghamobile.com/ajax/comment";
        private const string _urlApiReview = @"https://hoanghamobile.com/ajax/review";
        private readonly HttpClient _client = new HttpClient();
        private ConcurrentQueue<(string, string, DateTime)> _myQueue = new ConcurrentQueue<(string, string, DateTime)>();
        private uint start = 0;
        private readonly Telegram_Helper tgl;

        public HoangHa()
        {
            _client.DefaultRequestHeaders.Add("x-requested-with", "XMLHttpRequest");
            tgl = new Telegram_Helper(Config_System.KEY_BOT);
        }

        public async Task CrawlData()
        {
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

            Task<uint> task1 = GetCommentProduct(5_000);
            Task<uint> task2 = GetCommentProduct(4_000);

            await Task.WhenAll(task1, task2);

            uint count = task1.Result + task2.Result;
            if (count > 0)
            {
                MySQL_Helper msql1 = new MySQL_Helper(Config_System.ConnectionToTableReportDaily);
                await msql1.InsertToTableReportDaily(_urlHome, count);
                msql1.Dispose();
            }

            await tgl.SendMessageToChannel($"Done {count} comment of HoangHa", Config_System.ID_TELEGRAM_BOT_GROUP_COMMENT_ECO);
        }

        public async Task<uint> GetCommentProduct(ushort delay)
        {
            (string, string, DateTime) obj = ("", "", DateTime.Now);
            MySQL_Helper msql = new MySQL_Helper(Config_System.ConnectionToTableLinkProduct);
            uint count = 0;

            while (_myQueue.TryDequeue(out obj))
            {
                List<CommentModel> lstCmtJson = new List<CommentModel>();
                ushort indexPage = 0;
                /* 
                 * [0]: comment
                 * [1]: review
                 */
                bool[] checkDataEndPage = { false, false };
                Dictionary<string, string> parameters = new Dictionary<string, string> {
                    { "parent", "0" },
                    { "child", "true" }
                };

                try
                {
                    string productId = "0";
                    string html = await HttpClient_Helper.GetData(_client, obj.Item2);
                    if (html is null)
                    {
                        continue;
                    }

                    _document.LoadHtml(html);

                    productId = _document.DocumentNode.SelectSingleNode(@"//section[@id='comments']//input[@name='ModelID']")?.Attributes["value"]?.Value ?? "0";
                    parameters["systemTypeId"] = _document.DocumentNode.SelectSingleNode(@"//section[@id='comments']//input[@name='SystemTypeID']")?.Attributes["value"]?.Value ?? "0";
                    parameters["modelId"] = productId;

                    while (true)
                    {
                        await Task.Delay(delay);
                        parameters["p"] = $"{indexPage++}";

                        #region Comment
                        if (!checkDataEndPage[0])
                        {
                            html = await HttpClient_Helper.GetDataByPostMethod(_client, _urlApiComment, parameters);
                            if (html is null)
                            {
                                break;
                            }

                            _document.LoadHtml(html);
                            html = null;

                            HtmlNodeCollection divComment = _document.DocumentNode.SelectNodes(@"//div[@class='item']");
                            if (divComment == null)
                            {
                                checkDataEndPage[0] = true;
                            }
                            else
                            {
                                foreach (HtmlNode item in divComment)
                                {
                                    DateTime dateComment = GetDate(item.SelectSingleNode(@"./div[@class='info']/p[2]/label/i")?.InnerText);
                                    if (dateComment.Date >= obj.Item3.Date)
                                    {
                                        CommentModel cmtJson = new CommentModel();

                                        cmtJson.UrlProduct = obj.Item2;
                                        cmtJson.Domain = _urlHome;
                                        cmtJson.ProductId = productId;

                                        cmtJson.UserComment = item.SelectSingleNode(@"./div[@class='info']/p/strong")?.InnerText;
                                        cmtJson.Comment = Util.RemoveSpecialCharacter(item.SelectSingleNode(@"./div[@class='info']/div[@class='content']")?.InnerText);

                                        cmtJson.PostDate = DateTime.Now;
                                        cmtJson.PostDateTimeStamp = Util.ConvertDateTimeToTimeStamp(cmtJson.PostDate);

                                        cmtJson.Id = Util.ConvertStringtoMD5(obj.Item2 + cmtJson.PostDateTimeStamp.ToString() + "-1");
                                        cmtJson.IdComment = -1;

                                        cmtJson.CommentDate = dateComment;
                                        cmtJson.CommentDateTimeStamp = Util.ConvertDateTimeToTimeStamp(dateComment);

                                        lstCmtJson.Add(cmtJson);
                                        count++;
                                    }
                                    else
                                    {
                                        checkDataEndPage[0] = true;
                                        break;
                                    }
                                }
                            }
                        }
                        #endregion

                        #region Review
                        if (!checkDataEndPage[1])
                        {
                            html = await HttpClient_Helper.GetDataByPostMethod(_client, _urlApiReview, parameters);
                            if (html is null)
                            {
                                continue;
                            }

                            _document.LoadHtml(html);
                            html = null;

                            HtmlNodeCollection divReview = _document.DocumentNode.SelectNodes(@"//div[@class='item']");
                            if (divReview == null)
                            {
                                checkDataEndPage[1] = true;
                            }
                            else
                            {
                                foreach (HtmlNode item in divReview)
                                {
                                    DateTime dateReview = GetDate(item.SelectSingleNode(@"./div[@class='info']/p[2]/label/i")?.InnerText);
                                    if (dateReview.Date >= obj.Item3.Date)
                                    {
                                        CommentModel cmtJson = new CommentModel();

                                        cmtJson.UrlProduct = obj.Item2;
                                        cmtJson.Domain = _urlHome;
                                        cmtJson.ProductId = productId;

                                        cmtJson.UserComment = item.SelectSingleNode(@"./div[@class='info']/p/strong")?.InnerText;
                                        cmtJson.Comment = Util.RemoveSpecialCharacter(item.SelectSingleNode(@"./div[@class='info']/div[@class='content']")?.InnerText);

                                        cmtJson.PostDate = DateTime.Now;
                                        cmtJson.PostDateTimeStamp = Util.ConvertDateTimeToTimeStamp(cmtJson.PostDate);

                                        cmtJson.Id = Util.ConvertStringtoMD5(obj.Item2 + cmtJson.PostDateTimeStamp.ToString() + "-1");
                                        cmtJson.IdComment = -1;

                                        cmtJson.CommentDate = dateReview;
                                        cmtJson.CommentDateTimeStamp = Util.ConvertDateTimeToTimeStamp((DateTime)cmtJson.CommentDate);

                                        lstCmtJson.Add(cmtJson);
                                        count++;
                                    }
                                    else
                                    {
                                        checkDataEndPage[1] = true;
                                        break;
                                    }
                                }
                            }
                        }
                        #endregion

                        if (checkDataEndPage[0] && checkDataEndPage[1])
                        {
                            break;
                        }
                    }
                }
                catch (Exception) { }

                /* Send to kafka */
                if (lstCmtJson.Any())
                {
                    foreach (CommentModel item in lstCmtJson)
                    {
                        string json = JsonSerializer.Serialize<CommentModel>(item, Util.opt);
                        Util.InsertPost(json);

                        await Task.Delay(50);
                    }
                }

                lstCmtJson.Clear();
                lstCmtJson.TrimExcess();

                await msql.UpdateTimeGetComment(obj.Item1);
                await Task.Delay(1_000);
            }

            msql.Dispose();
            return count;
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
                    ushort numberDay = (ushort)Util.convertTextToNumber(strDate);

                    if (strDate.Contains("giờ trước"))
                    {
                        return DateTime.Today.AddHours(-numberDay);
                    }
                    else if (strDate.Contains("Hôm qua"))
                    {
                        return DateTime.Today.AddDays(-1);
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
