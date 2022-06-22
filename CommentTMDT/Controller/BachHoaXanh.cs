using CommentTMDT.Helper;
using CommentTMDT.Model;
using HtmlAgilityPack;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CommentTMDT.Controller
{
    class BachHoaXanh
    {
        private readonly HtmlAgilityPack.HtmlDocument _document = new HtmlAgilityPack.HtmlDocument();
        private const string _urlHome = @"https://www.bachhoaxanh.com";
        private const string _urlApiHome = @"https://www.bachhoaxanh.com/aj/Comment/CommentList";
        private HttpClient _client = new HttpClient();
        private ConcurrentQueue<(string, string, DateTime)> _myQueue = new ConcurrentQueue<(string, string, DateTime)>();
        private uint start = 0;
        private readonly Telegram_Helper tgl;

        public BachHoaXanh()
        {
            _client.DefaultRequestHeaders.Clear();
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json");
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
            Task<uint> task2 = GetCommentProduct(5_000);

            await Task.WhenAll(task1, task2);

            uint count = task1.Result + task2.Result;
            await tgl.SendMessageToChannel($"Done {count} comment of Bach Hoa Xanh", Config_System.ID_TELEGRAM_BOT_GROUP_COMMENT_ECO);
        }

        private async Task<uint> GetCommentProduct(ushort delay)
        {
            (string, string, DateTime) obj = ("", "", DateTime.Now);
            uint count = 0;
            MySQL_Helper msql = new MySQL_Helper(Config_System.ConnectionToTableLinkProduct);

            while (_myQueue.TryDequeue(out obj))
            {
                List<CommentModel> lstComment = new List<CommentModel>();
                bool[] checkEndData = { /*[0]: QA*/ false, /* [1]: comment*/false };

                Dictionary<string, string> parameters = new Dictionary<string, string> {
                        { "objectType", "2" },
                        { "pageSize", "50" }
                    };
                byte indexPage = 0;

                try
                {
                    string html = await HttpClient_Helper.GetData(_client, obj.Item2);
                    if (String.IsNullOrEmpty(html))
                    {
                        continue;
                    }

                    _document.LoadHtml(html);

                    string productId = _document.DocumentNode.SelectSingleNode(@"//div[@class='rowdetail']/aside[@class='slide']")?.Attributes["data-id"]?.Value ?? "0";
                    parameters["objectId"] = productId;

                    while (true)
                    {
                        await Task.Delay(delay);
                        parameters["page"] = $"{indexPage++}";

                        #region Comment
                        if (!checkEndData[1])
                        {
                            parameters["typeOf"] = "1";
                            html = await HttpClient_Helper.GetDataByPostMethod(_client, _urlApiHome, parameters);
                            _document.LoadHtml(html);
                            html = null;

                            HtmlNodeCollection divComment = _document.DocumentNode.SelectNodes(@"//div[@id='comment-list1']/div[@class='item']");
                            if (divComment == null)
                            {
                                checkEndData[1] = true;
                            }
                            else
                            {
                                foreach (HtmlNode item in divComment)
                                {
                                    DateTime dateComment = GetDate(item.SelectSingleNode("./div[not(@*)][last()]//span[@class='comment-time']")?.InnerText).Date;
                                    if (dateComment.Year == 1)
                                    {
                                        continue;
                                    }

                                    if (dateComment.Date < obj.Item3.Date)
                                    {
                                        checkEndData[1] = true;
                                        break;
                                    }

                                    CommentModel cmtJson = new CommentModel();

                                    cmtJson.UrlProduct = obj.Item2;
                                    cmtJson.Domain = _urlHome;
                                    cmtJson.ProductId = productId;
                                    cmtJson.UserComment = Util.RemoveSpecialCharacter(item.SelectSingleNode("./b/text()[normalize-space()]")?.InnerText);
                                    cmtJson.Comment = Util.RemoveSpecialCharacter(item.SelectSingleNode("./div[not(@*)][1]")?.InnerText);
                                    cmtJson.PostDate = DateTime.Now;
                                    cmtJson.PostDateTimeStamp = Util.ConvertDateTimeToTimeStamp(DateTime.Now);
                                    cmtJson.IdComment = ((int)(String.IsNullOrEmpty(item.Attributes["data-id"]?.Value) ? -1 : Util.convertTextToNumber(item.Attributes["data-id"]?.Value)));
                                    cmtJson.Id = Util.ConvertStringtoMD5(obj.Item2 + cmtJson.IdComment.ToString());
                                    cmtJson.CommentDate = dateComment;
                                    cmtJson.CommentDateTimeStamp = Util.ConvertDateTimeToTimeStamp(cmtJson.CommentDate);

                                    lstComment.Add(cmtJson);
                                    count++;
                                }
                            }

                            await Task.Delay(1_000);
                        }
                        #endregion

                        #region Q&A
                        if (!checkEndData[0])
                        {
                            parameters["typeOf"] = "0";
                            html = await HttpClient_Helper.GetDataByPostMethod(_client, _urlApiHome, parameters);
                            _document.LoadHtml(html);
                            html = null;

                            HtmlNodeCollection divQnA = _document.DocumentNode.SelectNodes(@"//div[@id='comment-list0']/div[contains(@class, 'item') or @class='cmt-child']");
                            if (divQnA == null)
                            {
                                checkEndData[0] = true;
                            }
                            else
                            {
                                foreach (HtmlNode item in divQnA)
                                {
                                    #region user
                                    DateTime dateQuestion = GetDate(item.SelectSingleNode("./div[not(@*)][last()]//span[@class='comment-time']")?.InnerText);

                                    if (dateQuestion.Year == 1)
                                    {
                                        goto next;
                                    }

                                    if (dateQuestion.Date < obj.Item3.Date)
                                    {
                                        checkEndData[0] = true;
                                        break;
                                    }

                                    CommentModel user = new CommentModel();

                                    user.UrlProduct = obj.Item2;
                                    user.Domain = _urlHome;
                                    user.ProductId = productId;
                                    user.UserComment = Util.RemoveSpecialCharacter(item.SelectSingleNode("./b/text()[normalize-space()]")?.InnerText);
                                    user.Comment = Util.RemoveSpecialCharacter(item.SelectSingleNode("./div[not(@*)][1]")?.InnerText);
                                    user.PostDate = DateTime.Now;
                                    user.PostDateTimeStamp = Util.ConvertDateTimeToTimeStamp(user.PostDate);
                                    user.IdComment = (int)(String.IsNullOrEmpty(item.Attributes["data-id"]?.Value) ? -1 : Util.convertTextToNumber(item.Attributes["data-id"]?.Value));
                                    user.Id = Util.ConvertStringtoMD5(obj.Item2 + user.IdComment.ToString());
                                    user.CommentDate = dateQuestion;
                                    user.CommentDateTimeStamp = Util.ConvertDateTimeToTimeStamp((DateTime)user.CommentDate);

                                    lstComment.Add(user);
                                    count++;
                                    #endregion

                                #region reply's admin
                                next:
                                    DateTime dateReply = GetDate(item.SelectSingleNode(".//div[@class='child item']/div[not(@*)][last()]//span[@class='comment-time']")?.InnerText);
                                    if (dateReply.Year == 1)
                                    {
                                        continue;
                                    }

                                    if (dateReply.Date < obj.Item3.Date)
                                    {
                                        checkEndData[0] = true;
                                        break;
                                    }

                                    CommentModel admin = new CommentModel();

                                    admin.UrlProduct = obj.Item2;
                                    admin.Domain = _urlHome;
                                    admin.ProductId = productId;
                                    admin.UserComment = Util.RemoveSpecialCharacter(item.SelectSingleNode(".//div[@class='child item']/b")?.InnerText);
                                    admin.Comment = Util.RemoveSpecialCharacter(item.SelectSingleNode(".//div[@class='child item']/div[not(@*)][1][text()[preceding-sibling::br] or /span/span/text()[preceding-sibling::br]]")?.InnerText);
                                    admin.PostDate = DateTime.Now;
                                    admin.PostDateTimeStamp = Util.ConvertDateTimeToTimeStamp(admin.PostDate);
                                    admin.IdComment = (int)(String.IsNullOrEmpty(item.Attributes["data-id"]?.Value) ? -1 : Util.convertTextToNumber(item.Attributes["data-id"]?.Value));
                                    admin.Id = Util.ConvertStringtoMD5(obj.Item2 + admin.IdComment.ToString());
                                    admin.CommentDate = dateReply;
                                    admin.CommentDateTimeStamp = Util.ConvertDateTimeToTimeStamp(admin.CommentDate);

                                    lstComment.Add(admin);
                                    count++;

                                    #endregion
                                }
                            }

                            await Task.Delay(1_000);
                        }
                        #endregion

                        if (checkEndData[0] && checkEndData[1])
                        {
                            break;
                        }
                    }
                }
                catch (Exception) { }

                /* Send to kafka */
                if (lstComment.Any())
                {
                    foreach (CommentModel item in lstComment)
                    {
                        string json = JsonSerializer.Serialize(item);
                        Util.InsertPost(json);

                        await Task.Delay(50);
                    }
                }

                lstComment.Clear();
                lstComment.TrimExcess();

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
                if (String.IsNullOrEmpty(strDate))
                {
                    return new DateTime();
                }

                Regex rg = new Regex(pattern);

                if (strDate.Contains("qua"))
                {
                    return DateTime.Today.AddDays(-1);
                }
                else if (strDate.Contains("ngày trước") || strDate.Contains("ng&#224;y"))
                {
                    ushort numberDay = (ushort)Util.GetNumberDay(strDate);
                    return DateTime.Today.AddDays(-numberDay);
                }
                else if (strDate.Contains("tháng trước") || strDate.Contains("th&#225;ng"))
                {
                    ushort numberDay = (ushort)(Util.GetNumberDay(strDate) * 30);
                    return DateTime.Today.AddDays(-numberDay);
                }
                else if (strDate.Contains("năm trước") || strDate.Contains("nam"))
                {
                    ushort numberDay = (ushort)(Util.GetNumberDay(strDate) * 365);
                    return DateTime.Today.AddDays(-numberDay);
                }
                else if (rg.IsMatch(strDate))
                {
                    MatchCollection matched = rg.Matches(strDate);

                    return DateTime.ParseExact(matched[0].Value, formatTypeDate, CultureInfo.InvariantCulture);
                }

                return DateTime.Now;
            }
            catch (Exception)
            {
                return new DateTime();
            }
        }

    }
}
