using CommentTMDT.Helper;
using CommentTMDT.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace CommentTMDT.Controller
{
    class DienMayChoLon
    {
        private const string _urlHome = @"https://dienmaycholon.vn";
        private ConcurrentQueue<(string, string, DateTime)> _myQueue = new ConcurrentQueue<(string, string, DateTime)>();
        private uint start = 0;
        private readonly Telegram_Helper tgl;
        private readonly HtmlAgilityPack.HtmlDocument _document = new HtmlAgilityPack.HtmlDocument();

        public DienMayChoLon()
        {
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

            Task<uint> task1 = GetComment(4_000);
            Task<uint> task2 = GetComment(5_000);

            await Task.WhenAll(task1, task2);

            uint count = task1.Result + task2.Result;
            if (count > 0)
            {
                MySQL_Helper msql1 = new MySQL_Helper(Config_System.ConnectionToTableReportDaily);
                await msql1.InsertToTableReportDaily(_urlHome, count);
                msql1.Dispose();
            }

            await tgl.SendMessageToChannel($"Done {count} comment of DienMayChoLon", Config_System.ID_TELEGRAM_BOT_GROUP_COMMENT_ECO);
        }

        private async Task<uint> GetComment(ushort delay)
        {
            (string, string, DateTime) obj = ("", "", DateTime.Now);
            const string urlBase = "https://dienmaycholon.vn/api/general/dfquestionproduct/list";
            UriBuilder uriBuilder = new UriBuilder(urlBase);
            NameValueCollection query = HttpUtility.ParseQueryString(uriBuilder.Query);
            MySQL_Helper msql = new MySQL_Helper(Config_System.ConnectionToTableLinkProduct);
            uint count = 0;

            while (_myQueue.TryDequeue(out obj))
            {
                await Task.Delay(delay);

                List<CommentModel> lstData = new List<CommentModel>();
                List<string> lstCheckConflic = new List<string>();
                ushort page = 0;

                string html = await HttpClient_Helper.GetData(obj.Item2);
                if (string.IsNullOrEmpty(html))
                {
                    continue;
                }

                _document.LoadHtml(html);

                html = null;
                string productId = _document.DocumentNode.SelectSingleNode("//input[@id='id_product']")?.Attributes["value"].Value ?? "0";

                while (true)
                {
                    bool checkEndData = false;

                    query["cid_product"] = productId;
                    query["page"] = $"{page++}";
                    uriBuilder.Query = query.ToString();
                    string url = uriBuilder.ToString();

                    string json = await HttpClient_Helper.GetData(url);
                    DienMayChoLonModel.Root data = json.ToObject<DienMayChoLonModel.Root>();

                    if (!data?.data?.data?.Any() ?? true)
                    {
                        break;
                    }

                    foreach (DienMayChoLonModel.Datum item in data.data.data)
                    {
                        DateTime cmtDate = GetDate(item.showTime);
                        if (lstCheckConflic.Contains(item.id) || cmtDate.Date < obj.Item3.Date)
                        {
                            checkEndData = true;
                            break;
                        }

                        #region user
                        CommentModel user = new CommentModel();
                        user.ProductId = productId;
                        user.UrlProduct = obj.Item2;

                        user.CommentDate = cmtDate;
                        user.CommentDateTimeStamp = Util.ConvertDateTimeToTimeStamp(cmtDate);
                        user.Comment = item.content;
                        user.UserComment = Util.ConvertUtf_16ToUnicode(item.name);

                        user.PostDate = DateTime.Now;
                        user.PostDateTimeStamp = Util.ConvertDateTimeToTimeStamp(user.PostDate);

                        user.Id = item.id;
                        lstData.Add(user);
                        lstCheckConflic.Add(user.Id);

                        ++count;
                        #endregion

                        #region QTV
                        if (item.child?.Any() ?? false)
                        {
                            foreach (DienMayChoLonModel.Child item1 in item.child)
                            {
                                CommentModel qtv = new CommentModel();
                                qtv.ProductId = productId;
                                qtv.UrlProduct = obj.Item2;

                                qtv.CommentDate = cmtDate;
                                qtv.CommentDateTimeStamp = user.CommentDateTimeStamp;
                                qtv.Comment = item1.content;
                                qtv.UserComment = "QTV";

                                qtv.PostDate = user.PostDate;
                                qtv.PostDateTimeStamp = user.PostDateTimeStamp;

                                qtv.Id = item1.id_child;
                                lstData.Add(qtv);
                                lstCheckConflic.Add(qtv.Id);

                                ++count;
                            }
                        }
                        #endregion
                    }

                    if (checkEndData)
                    {
                        break;
                    }
                }

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

                await msql.UpdateTimeGetComment(obj.Item1);
                await Task.Delay(1_000);
            }

            msql.Dispose();
            return count;
        }

        private DateTime GetDate(string dateStr)
        {
            dateStr = Util.ConvertUtf_16ToUnicode(dateStr);
            byte numberDay = (byte)Util.GetNumberDay(dateStr);

            if (dateStr.Contains("giờ"))
            {
                return DateTime.Now.AddHours(-numberDay);
            }
            else if (dateStr.Contains("ngày"))
            {
                return DateTime.Now.AddDays(-numberDay);
            }
            else if (dateStr.Contains("tuần"))
            {
                return DateTime.Now.AddDays(-7 * numberDay);
            }
            else if (dateStr.Contains("tháng"))
            {
                return DateTime.Now.AddMonths(-numberDay);
            }
            else if (dateStr.Contains("năm"))
            {
                return DateTime.Now.AddYears(-numberDay);
            }
            else
            {
                return DateTime.Now;
            }
        }
    }
}
