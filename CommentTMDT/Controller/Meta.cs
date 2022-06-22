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
using System.Threading;
using System.Threading.Tasks;

namespace CommentTMDT.Controller
{
    class Meta
    {
        private readonly HttpClient _client = new HttpClient();
        private const string _urlHome = @"https://meta.vn";
        private const string _urlApiHome = @"https://meta.vn/ajx/loader.aspx";
        private readonly HtmlAgilityPack.HtmlDocument _document = new HtmlAgilityPack.HtmlDocument();
        private ConcurrentQueue<(string, string, DateTime)> _myQueue = new ConcurrentQueue<(string, string, DateTime)>();
        private uint start = 0;
        private readonly Telegram_Helper tgl;

        public async Task CrawData()
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

            Task<uint> task1 = GetCommentProduct(5_000);
            Task<uint> task2 = GetCommentProduct(4_000);

            await Task.WhenAll(task1, task2);

            count = task1.Result + task2.Result;
            await tgl.SendMessageToChannel($"Done {count} comment of tiki", Config_System.ID_TELEGRAM_BOT_GROUP_COMMENT_ECO);
        }

        private async Task<uint> GetCommentProduct(ushort delay)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string> {
                { "request", "details.rateReviews" },
                { "mod", "ajax" },
                { "act", "list" },
                { "sort", "1" },
                { "show_full", "0" },
                { "productId", "0" },
                { "p", "0" }
            };

            /* Total comment's product */
            uint totalComment = 0;

            try
            {
                (string, string, DateTime) obj = ("", "", DateTime.Now);
                MySQL_Helper msql = new MySQL_Helper(Config_System.ConnectionToTableLinkProduct);

                while (_myQueue.TryDequeue(out obj))
                {
                    List<CommentModel> lstCmtJson = new List<CommentModel>();
                    string productId = SplitIdParamToUrl(obj.Item2) ?? "0";
                    parameters["productId"] = productId;
                    ushort indexPage = 1;
                    ushort indexLastComment = 0;
                    bool checkEndData = false;

                    while (true)
                    {
                        string html = null;
                        parameters["p"] = $"{indexPage++}";

                        HttpResponseMessage result = new HttpResponseMessage();
                        FormUrlEncodedContent encodedContent = new FormUrlEncodedContent(parameters);
                        using (CancellationTokenSource cts = new CancellationTokenSource(20_000))
                        {
                            try
                            {
                                result = await _client.PostAsync(new Uri(_urlApiHome), encodedContent);
                            }
                            catch (Exception)
                            {
                                return totalComment;
                            }
                        }

                        html = await result.Content.ReadAsStringAsync();

                        if (String.IsNullOrEmpty(html))
                        {
                            break;
                        }

                        await Task.Delay(delay);

                        _document.LoadHtml(html);
                        HtmlNodeCollection divComment = _document.DocumentNode.SelectNodes($@"//div[contains(@class, 'comment-row-item view-comments-item')][position()>{indexLastComment++}]");

                        if (divComment == null)
                        {
                            break;
                        }

                        foreach (HtmlNode item in divComment)
                        {
                            #region User
                            CommentModel cmtUser = new CommentModel();
                            string strDate = item.SelectSingleNode(@".//div[@class='comment-ask-box level1']//div[@class='time-comment block']/span")?.Attributes["title"]?.Value;

                            if (!String.IsNullOrEmpty(strDate))
                            {
                                cmtUser.CommentDate = GetDate(strDate);
                                if(cmtUser.CommentDate.Date < obj.Item3.Date)
                                {
                                    checkEndData = true;
                                    break;
                                }

                                string idComment = item.SelectSingleNode(".//input[@class='rep-comment relate-com-item']")?.Attributes["value"]?.Value;
                                cmtUser.UrlProduct = obj.Item1;
                                cmtUser.Domain = _urlHome;
                                cmtUser.UserComment = item.SelectSingleNode(@".//span[@class='full-name-cm ava-name user-normal']")?.InnerText;
                                cmtUser.Comment = Util.RemoveSpecialCharacter(item.SelectSingleNode(@".//div[@class='comment-ask']")?.InnerText);
                                cmtUser.ProductId = productId;
                                cmtUser.PostDate = DateTime.Now;
                                cmtUser.PostDateTimeStamp = Util.ConvertDateTimeToTimeStamp(cmtUser.PostDate);
                                cmtUser.IdComment = (int)(String.IsNullOrEmpty(idComment) ? -1 : Util.convertTextToNumber(idComment));
                                cmtUser.Id = Util.ConvertStringtoMD5(obj.Item1 + cmtUser.PostDateTimeStamp.ToString() + cmtUser.IdComment.ToString());

                                cmtUser.CommentDateTimeStamp = Util.ConvertDateTimeToTimeStamp((DateTime)cmtUser.CommentDate);

                                lstCmtJson.Add(cmtUser);
                                totalComment++;
                            }
                            #endregion

                            #region Admin
                            CommentModel cmtAdmin = new CommentModel();
                            strDate = item.SelectSingleNode(@".//div[contains(@class, 'comment-ask-box member-rep')]//div[@class='time-comment block']/span")?.Attributes["title"]?.Value;

                            if (!String.IsNullOrEmpty(strDate))
                            {
                                cmtAdmin.CommentDate = GetDate(strDate);
                                if(cmtAdmin.CommentDate.Date < obj.Item3.Date)
                                {
                                    checkEndData = true;
                                    break;
                                }

                                string idComment = item.SelectSingleNode(".//div[@class='comment-ask-box member-rep level2']")?.Attributes["data-id"]?.Value;
                                cmtAdmin.PostDate = DateTime.Now;
                                cmtAdmin.PostDateTimeStamp = Util.ConvertDateTimeToTimeStamp(cmtAdmin.PostDate);
                                cmtAdmin.UrlProduct = obj.Item1;
                                cmtAdmin.Domain = _urlHome;
                                cmtAdmin.UserComment = "META";
                                cmtAdmin.Comment = Util.RemoveSpecialCharacter(item.SelectSingleNode(@".//div[@class='comment-replied']//div[@class='show-replied']")?.InnerText);
                                cmtAdmin.ProductId = productId;
                                cmtAdmin.IdComment = (int)(String.IsNullOrEmpty(idComment) ? -1 : Util.convertTextToNumber(idComment));
                                cmtAdmin.Id = Util.ConvertStringtoMD5(obj.Item1 + cmtAdmin.PostDateTimeStamp.ToString() + cmtAdmin.IdComment.ToString());

                                cmtAdmin.CommentDateTimeStamp = Util.ConvertDateTimeToTimeStamp((DateTime)cmtAdmin.CommentDate);

                                lstCmtJson.Add(cmtAdmin);
                                totalComment++;
                            }
                            #endregion
                        }

                        if(checkEndData)
                        {
                            break;
                        }
                    }

                    /* Send to kafka */
                    if (lstCmtJson.Any())
                    {
                        foreach (CommentModel item in lstCmtJson)
                        {
                            string json = JsonSerializer.Serialize(item);
                            Util.InsertPost(json);

                            await Task.Delay(500);
                        }
                    }

                    lstCmtJson.Clear();
                    lstCmtJson.TrimExcess();

                    await msql.UpdateTimeGetComment(obj.Item1);
                    await Task.Delay(1_000);
                }

                msql.Dispose();
            }
            catch (Exception) { }

            return totalComment;
        }

        private DateTime GetDate(string strDate, string pattern = @"\d{2}-\d{2}-\d{4}", string formatTypeDate = @"dd-MM-yyyy")
        {
            try
            {
                Regex rg = new Regex(pattern);
                Match matched = rg.Match(strDate);

                return DateTime.ParseExact(matched.Value, formatTypeDate, CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                return new DateTime();
            }
        }

        private string SplitIdParamToUrl(string url)
        {
            try
            {
                MatchCollection matches = Regex.Matches(url, @"(?<=-p)\d+");
                return matches[0].Value;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public Meta()
        {
            tgl = new Telegram_Helper(Config_System.KEY_BOT);
        }
    }
}
