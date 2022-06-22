using CommentTMDT.Helper;
using CommentTMDT.Model;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;

namespace CommentTMDT.Controller
{
    class Lazada
    {
        private const string _urlHome = @"https://www.lazada.vn/";
        private const string _urlAPICommentHome = @"https://my.lazada.vn/pdp/review/getReviewList";
        private readonly static HttpClient _client = new HttpClient();
        private Label _lbError, _lbTotalComment;

        public Lazada(Label lbError, Label lbTotalComment)
        {
            _lbError = lbError;
            _lbTotalComment = lbTotalComment;
        }

        public async Task CrawlData()
        {
            await GetCommentProduct(@"https://www.lazada.vn/products/dau-goi-clear-thao-duoc-botanique-630g-i1766940264-s7914981909.html?search=1&spm=a2o4n.searchlist.list.i40.72cd63bbh1jMom");
        }

        private async Task<List<CommentModel>> GetCommentProduct(string url)
        {
            byte indexPage = 1;
            List<CommentModel> lstCmtJson = new List<CommentModel>();

            try
            {
                UriBuilder builder = new UriBuilder(_urlAPICommentHome);
                NameValueCollection query = HttpUtility.ParseQueryString(builder.Query);

                query["itemId"] = SplitIdParamToUrl(url) ?? "0";

                while (true)
                {
                    query["pageSize"] = "10";
                    query["pageNo"] = $"{indexPage++}";

                    builder.Query = query.ToString();
                    string url1 = builder.ToString();

                    HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, url1);
                    HttpResponseMessage httpResponseMessage = await _client.SendAsync(httpRequestMessage);

                    HttpContent httpContent = httpResponseMessage.Content;
                    string result = httpResponseMessage.Content.ReadAsStringAsync().Result;

                    LazadaModel.Root data = result.ToObject<LazadaModel.Root>();
                    if (data is null || data.httpStatusCode != 200)
                    {
                        break;
                    }

                    if (data.model?.items != null && data.model.items.Any())
                    {
                        foreach (LazadaModel.Item item in data.model.items)
                        {

                        }
                    }
                }
            }
            catch (Exception) { }

            return lstCmtJson;
        }

        private DateTime GetDate(string strDate, string formatTypeDate = @"dd thg MM yyyy")
        {
            try
            {
                if (strDate.Contains("giờ trước") || strDate.Contains("phút trước") || strDate.Contains("hours ago"))
                {
                    return DateTime.Now;
                }
                else if (strDate.Contains("Hôm qua"))
                {
                    return DateTime.Today.AddDays(-1);
                }
                else if (strDate.Contains("ngày trước") || strDate.Contains("day ago"))
                {
                    ushort numberDay = (ushort)Util.convertTextToNumber(strDate);
                    return DateTime.Today.AddDays(-numberDay);
                }
                else if (strDate.Contains("weeks ago"))
                {
                    ushort numberDay = (ushort)(Util.convertTextToNumber(strDate) * 7);
                    return DateTime.Today.AddDays(-numberDay);
                }
                else if (strDate.Contains("tháng trước"))
                {
                    ushort numberDay = (ushort)(Util.convertTextToNumber(strDate) * 30);
                    return DateTime.Today.AddDays(-numberDay);
                }
                else
                {
                    return DateTime.Parse(strDate);
                }
            }
            catch (Exception ex)
            {
                return new DateTime();
            }
        }

        private string SplitIdParamToUrl(string url)
        {
            try
            {
                MatchCollection matches = Regex.Matches(url, @"(?<=-i)\d+");
                return matches[0].Value;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
