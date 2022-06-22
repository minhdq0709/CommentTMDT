using CommentTMDT.Helper;
using CommentTMDT.Model;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;

namespace CommentTMDT.Controller
{
    class DiDongThongMinh
    {
        private const string _urlHome = @"https://didongthongminh.vn/";
        private Label _lbError, _lbTotalComment;
        private readonly HttpClient _client = new HttpClient();
        private readonly HtmlAgilityPack.HtmlDocument _document = new HtmlAgilityPack.HtmlDocument();

        public DiDongThongMinh(Label lbError, Label lbTotalComment)
        {
            _lbError = lbError;
            _lbTotalComment = lbTotalComment;
        }

        public async Task CrawlData()
        {
            List<CommentModel> data = await GetCommentProduct(@"https://didongthongminh.vn/xiaomi-redmi-note-11s-8gb-128gb-chinh-hang");
            string jsonObj = System.Text.Json.JsonSerializer.Serialize<List<CommentModel>>(data, Util.opt);
        }

        private async ValueTask<List<CommentModel>> GetCommentProduct(string url)
        {
            List<CommentModel> lstCommentJson = new List<CommentModel>();

            try
            {
                /* Get poruct id */
                string productId = "0";
                string html = await HttpClient_Helper.GetData(_client, url);
                if(html is null)
                {
                    return lstCommentJson;
                }

                _document.LoadHtml(html);
                html = null; 

                productId = _document.DocumentNode.SelectSingleNode(@"//input[@id='product_id_compare']")?.Attributes["value"]?.Value ?? "0";

                /* Build param for get request */
                UriBuilder builder = new UriBuilder(@"https://didongthongminh.vn/index.php");
                NameValueCollection query = HttpUtility.ParseQueryString(builder.Query);

                query["id"] = productId;
                query["task"] = "all_conment";
                query["raw"] = "1";
                query["view"] = "product";
                query["module"] = "products";

                builder.Query = query.ToString();
                html = await HttpClient_Helper.GetData(_client, builder.ToString());

                if(html is null)
                {
                    return lstCommentJson;
                }

                _document.LoadHtml(html);

                HtmlNodeCollection lstDiv = _document.DocumentNode.SelectNodes("//div[@class='cf comment-item comment-item-125  comment_level_0 comment_sub_0']");
                if(lstDiv != null)
                {
                    foreach(HtmlNode item in lstDiv)
                    {
                        DateTime dateComment = GetDate(item.SelectSingleNode(".//span[@class='date']")?.InnerText);

                        if (Util.DatesAreInTheSameWeek(dateComment, DateTime.Now))
                        {
                            CommentModel comment = new CommentModel();

                            comment.UrlProduct = url;
                            comment.Domain = _urlHome;

                            comment.PostDate = DateTime.Now;
                            comment.PostDateTimeStamp = Util.ConvertDateTimeToTimeStamp(comment.PostDate);

                            comment.UserComment = item.SelectSingleNode(".//strong[@class='text_b_cm']")?.InnerText;
                            comment.Comment = item.SelectSingleNode(".//div[@class='comment_content ']")?.InnerText;

                            comment.CommentDate = dateComment;
                            comment.CommentDateTimeStamp = Util.ConvertDateTimeToTimeStamp(dateComment);

                            comment.Id = Util.ConvertStringtoMD5(url + comment.CommentDateTimeStamp.ToString() + "-1");

                            lstCommentJson.Add(comment);
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                return lstCommentJson;
            }
            catch (Exception)
            {
                return lstCommentJson;
            }
        }

        protected DateTime GetDate(string strDate, string pattern = @"\d{2}\/\d{2}\/\d{4}", string formatTypeDate = @"dd/MM/yyyy")
        {
            try
            {
                if (strDate.Contains("giờ trước") || strDate.Contains("phút trước"))
                {
                    return DateTime.Now;
                }
                else if (strDate.Contains("Hôm qua"))
                {
                    return DateTime.Today.AddDays(-1);
                }
                else if (strDate.Contains("ngày trước"))
                {
                    ushort numberDay = (ushort)Util.convertTextToNumber(strDate);
                    return DateTime.Today.AddDays(-numberDay);
                }
                else if (strDate.Contains("tuần trước"))
                {
                    ushort numberDay = (ushort)(Util.convertTextToNumber(strDate) * 7);
                    return DateTime.Today.AddDays(-numberDay);
                }
                else if (strDate.Contains("tháng trước"))
                {
                    ushort numberDay = (ushort)(Util.convertTextToNumber(strDate));
                    return DateTime.Today.AddMonths(-numberDay);
                }
                else if (strDate.Contains("năm trước"))
                {
                    ushort numberDay = (ushort)(Util.convertTextToNumber(strDate));
                    return DateTime.Today.AddYears(-numberDay);
                }
                else
                {
                    Regex rg = new Regex(pattern);
                    MatchCollection matched = rg.Matches(strDate);

                    return DateTime.ParseExact(matched[0].Value, formatTypeDate, CultureInfo.InvariantCulture);
                }
            }
            catch (Exception ex)
            {
                return new DateTime();
            }
        }
    }
}
