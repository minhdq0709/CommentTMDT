using CommentTMDT.Helper;
using CommentTMDT.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace CommentTMDT.Controller
{
    class Sendo
    {
        private readonly string _urlHome = @"https://www.sendo.vn";
        private readonly HttpClient _client = new HttpClient();
        private const string _urlApiHome = @"https://ratingapi.sendo.vn/product/";
        private ConcurrentQueue<(string, string, DateTime)> _myQueue = new ConcurrentQueue<(string, string, DateTime)>();
        private uint start = 0;
        private readonly Telegram_Helper tgl;

        public Sendo()
        {
            tgl = new Telegram_Helper(Config_System.KEY_BOT);
        }

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

            Task<uint> task1 = GetCommentProduct(4_000);
            Task<uint> task2 = GetCommentProduct(5_000);
            Task<uint> task3 = GetCommentProduct(3_000);
            Task<uint> task4 = GetCommentProduct(2_000);

            await Task.WhenAll(task1, task2, task3, task4);

            count = task1.Result + task2.Result + task3.Result + task4.Result;
            await tgl.SendMessageToChannel($"Done {count} comment of Sendo", Config_System.ID_TELEGRAM_BOT_GROUP_COMMENT_ECO);
        }

        private async Task<uint> GetCommentProduct(ushort delay)
        {
            uint count = 0;
            MySQL_Helper msql = new MySQL_Helper(Config_System.ConnectionToTableLinkProduct);

            (string, string, DateTime) obj = ("", "", DateTime.Now);
            while (_myQueue.TryDequeue(out obj))
            {
                byte indexPage = 0;
                List<CommentModel> lstData = new List<CommentModel>();

                /* Config http */
                string urlApi = $"{_urlApiHome}/{SplitIdParamToUrl(obj.Item2)}/rating";
                UriBuilder builder = new UriBuilder(urlApi);
                NameValueCollection query = HttpUtility.ParseQueryString(builder.Query);

                query["limit"] = "200";
                query["v"] = "2";
                query["star"] = "all";

                try
                {
                    while (true)
                    {
                        query["page"] = $"{indexPage++}";
                        builder.Query = query.ToString();
                        string url = builder.ToString();

                        string json = await _client.GetStringAsync(url).ConfigureAwait(false);
                        SendoModel.Root data = null;
                        try
                        {
                            data = JsonSerializer.Deserialize<SendoModel.Root>(json);
                        }
                        catch(Exception ex) { }

                        if (data?.data == null || !data.data.Any())
                        {
                            break;
                        }

                        foreach (SendoModel.Datum1 item in data.data)
                        {
                            DateTime commentDate = Util.UnixTimeStampToDateTime(item.update_time);
                            if(commentDate.Date >= obj.Item3.Date && !string.IsNullOrEmpty(item.comment))
                            {
                                CommentModel cmtJson = new CommentModel();
                                cmtJson.UrlProduct = url;
                                cmtJson.Domain = _urlHome;

                                cmtJson.UserComment = item.user_name;
                                cmtJson.Comment = item.comment;

                                cmtJson.PostDateTimeStamp = item.update_time;
                                cmtJson.PostDate = commentDate;

                                cmtJson.IdComment = item.rating_id;
                                cmtJson.Id = Util.ConvertStringtoMD5(url + cmtJson.PostDateTimeStamp.ToString() + cmtJson.IdComment);

                                lstData.Add(cmtJson);
                                ++count;
                            }
                        }

                        await Task.Delay(delay);
                    }
                }
                catch (Exception) { }

                /* Send to kafka */
                if(lstData.Any())
                {
                    foreach(CommentModel item in lstData)
                    {
                        string json = JsonSerializer.Serialize(item);
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

        private string SplitIdParamToUrl(string url)
        {
            try
            {
                MatchCollection matches = Regex.Matches(url, @"\d+(?=.html)");
                return matches[0].Value;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
