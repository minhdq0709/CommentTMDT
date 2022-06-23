using CommentTMDT.Helper;
using CommentTMDT.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace CommentTMDT.Controller
{
    class Tiki
    {
        private HttpClient _client = new HttpClient();
        private const string _urlHome = @"https://tiki.vn";
        private const string _urlApiHome = @"https://tiki.vn/api/v2/reviews";
        private ConcurrentQueue<(string, string, DateTime)> _myQueue = new ConcurrentQueue<(string, string, DateTime)>();
        private uint start = 0;
        private readonly Telegram_Helper tgl;

        public Tiki()
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

            Task<uint> task1 = GetCommentProduct(1_000);
            Task<uint> task2 = GetCommentProduct(2_000);
            Task<uint> task3 = GetCommentProduct(3_000);
            Task<uint> task4 = GetCommentProduct(4_000);

            await Task.WhenAll(task1, task2, task3, task4);

            count = task1.Result + task2.Result + task3.Result + task4.Result;
            if (count > 0)
            {
                MySQL_Helper msql1 = new MySQL_Helper(Config_System.ConnectionToTableReportDaily);
                await msql1.InsertToTableReportDaily(_urlHome, count);
                msql1.Dispose();
            }

            await tgl.SendMessageToChannel($"Done {count} comment of tiki", Config_System.ID_TELEGRAM_BOT_GROUP_COMMENT_ECO);
        }

        private async Task<uint> GetCommentProduct(ushort delay)
        {
            (string, string, DateTime) obj = ("", "", DateTime.Now);
            UriBuilder builder = new UriBuilder(_urlApiHome);
            NameValueCollection query = HttpUtility.ParseQueryString(builder.Query);
            MySQL_Helper msql = new MySQL_Helper(Config_System.ConnectionToTableLinkProduct);
            uint count = 0;

            query["limit"] = "100";
            query["include"] = "comments,contribute_info";
            query["seller_id"] = "1";
            query["sort"] = "id|desc";

            while (_myQueue.TryDequeue(out obj))
            {
                List<CommentModel> data = new List<CommentModel>();
                ushort indexPage = 0;
                (string idProduct, string spid) dataId = SplitIdParamToUrl(obj.Item2);

                query["spid"] = dataId.spid ?? "0";
                query["product_id"] = dataId.idProduct ?? "0";

                while (true)
                {
                    query["page"] = $"{++indexPage}";

                    builder.Query = query.ToString();
                    string url1 = builder.ToString();

                    string json = null;
                    using (CancellationTokenSource cts = new CancellationTokenSource(20_000))
                    {
                        try
                        {
                            json = await _client.GetStringAsync(url1).ConfigureAwait(false);
                        }
                        catch (Exception)
                        {
                            break;
                        }
                    }

                    if (String.IsNullOrEmpty(json))
                    {
                        break;
                    }

                    await Task.Delay(delay);

                    TikiModel.Root tiki = null;
                    try
                    {
                        tiki = JsonSerializer.Deserialize<TikiModel.Root>(json);
                    }
                    catch (Exception ex) { }

                    if (tiki?.data == null || !tiki.data.Any())
                    {
                        break;
                    }

                    foreach (TikiModel.Datum item in tiki.data)
                    {
                        if(!string.IsNullOrEmpty(item.timeline?.review_created_date))
                        {
                            DateTime createComment = GetDate(item.timeline.review_created_date).Date;
                            if (createComment >= obj.Item3.Date && !String.IsNullOrEmpty(item.content))
                            {
                                CommentModel temp = new CommentModel();
                                temp.IdComment = item.id ?? 0;

                                temp.ProductId = dataId.idProduct;
                                temp.Domain = "https://tiki.vn/";
                                temp.UrlProduct = obj.Item2;

                                temp.UserComment = item.created_by?.full_name;
                                temp.Comment = item.content;

                                temp.PostDate = DateTime.Now;
                                temp.PostDateTimeStamp = Util.ConvertDateTimeToTimeStamp(temp.PostDate);
                                temp.CommentDate = createComment;
                                temp.CommentDateTimeStamp = Util.ConvertDateTimeToTimeStamp(createComment);

                                temp.Id = Util.ConvertStringtoMD5(temp.UrlProduct + temp.IdComment);
                                ++count;

                                data.Add(temp);
                            }
                        }
                    }
                }

                /* Send to kafka */
                if (data.Any())
                {
                    foreach (CommentModel item in data)
                    {
                        string json = JsonSerializer.Serialize<CommentModel>(item, Util.opt);
                        Util.InsertPost(json);

                        await Task.Delay(50);
                    }
                }

                data.Clear();
                data.TrimExcess();

                await msql.UpdateTimeGetComment(obj.Item1);
                await Task.Delay(1_000);
            }

            msql.Dispose();

            return count;
        }

        private (string idProduct, string spid) SplitIdParamToUrl(string url)
        {
            try
            {
                MatchCollection matches = Regex.Matches(url, @"\d+(?=.html)|(?<=spid=)\d+");
                if (matches.Count == 2)
                {
                    /*[0]: product_id, [1]: spid */
                    return (matches[0].Value, matches[1].Value);
                }
            }
            catch (Exception) { }

            return (null, null);
        }

        private DateTime GetDate(string strDate)
        {
            return DateTime.ParseExact(strDate, "yyyy-MM-dd HH:mm:ss", new CultureInfo("en-US"), DateTimeStyles.None);
        }
    }
}
