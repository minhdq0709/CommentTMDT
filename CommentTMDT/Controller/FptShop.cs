using CefSharp.WinForms;
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
    class FptShop
    {
        private readonly HtmlAgilityPack.HtmlDocument _document = new HtmlAgilityPack.HtmlDocument();
        private const string _urlHome = @"https://fptshop.com.vn";
        private Label _lbError, _lbTotalComment;
        private readonly HttpClient _client = new HttpClient();
        private readonly ChromiumWebBrowser _browser;
        private const string _jsNextComment = @"document.getElementsByClassName('pagination-item to-top')[0].click()";

        public FptShop(ChromiumWebBrowser browser, Label lbError, Label lbTotalComment)
        {
            _browser = browser;
            _lbError = lbError;
            _lbTotalComment = lbTotalComment;
        }

        public async Task CrawlData()
        {
            List<CommentModel> data = await GetCommentProduct("https://fptshop.com.vn/dien-thoai/oppo-reno7-z");
        }

        private async ValueTask<List<CommentModel>> GetCommentProduct(string url)
        {
            List<CommentModel> lstData = new List<CommentModel>();
            bool checkEndDataInWeek = false;

            await _browser.LoadUrlAsync(url);
            await Task.Delay(20_000);

            while(true)
            {
                string html = await Util.GetBrowserSource(_browser);
                _document.LoadHtml(html);
                html = null;

                HtmlNodeCollection lstDiv = _document.DocumentNode.SelectNodes("//div[@class='c-user-comment']//div[@class='c-comment-box']");
                if(lstDiv == null)
                {
                    break;
                }

                foreach (HtmlNode item in lstDiv)
                {
                    DateTime commentDate = GetDate(item.SelectSingleNode(".//div[@class='list-star']/ul/span")?.InnerText);

                    if (Util.DatesAreInTheSameWeek(commentDate, DateTime.Now))
                    {
                        CommentModel data = new CommentModel();

                        data.UserComment = item.SelectSingleNode("./div[@class='c-comment-box__content']/div")?.InnerText;
                        data.Comment = item.SelectSingleNode("./div[@class='c-comment-box__content']/div[@class='c-comment-text']")?.InnerText;
                        data.UrlProduct = url;

                        data.PostDate = DateTime.Now;
                        data.PostDateTimeStamp = Util.ConvertDateTimeToTimeStamp(data.PostDate);

                        data.CommentDate = commentDate;
                        data.CommentDateTimeStamp = Util.ConvertDateTimeToTimeStamp(commentDate);

                        data.ProductId = _document.DocumentNode.SelectSingleNode("//input[@id='id-product']")?.Attributes["value"]?.Value;
                        data.Id = Util.ConvertStringtoMD5(url + data.CommentDateTimeStamp + "-1");
                        data.IdComment = 0;

                        lstData.Add(data);
                    }
                    else
                    {
                        checkEndDataInWeek = true;
                        break;
                    }
                }

                if(checkEndDataInWeek)
                {
                    break;
                }    

                if (_document.DocumentNode.SelectSingleNode("//div[@class='c-user-comment']/div[@class='c-comment-pagination margin-bottom-10 reviewdetail']//li[@title='Tiến 1 trang']") != null)
                {
                    string js = await Util.EvaluateJavaScriptSync(_jsNextComment, _browser);
                    await Task.Delay(10_000);

                    /* Error */
                    if (js == null)
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            return lstData;
        }

        private DateTime GetDate(string date, string format = "dd/MM/yyyy")
        {
            try
            {
                if(Regex.IsMatch(date, format))
                {
                    date = Regex.Match(date, @"\d+/\d+/\d+").Value;
                    return DateTime.ParseExact(date, format, CultureInfo.InvariantCulture);
                }    
                else
                {
                    sbyte timeLine = (sbyte)(Util.convertTextToNumber(date) * (-1));

                    if (date.Contains("phút"))
                    {
                        return DateTime.Now.AddMinutes(timeLine);
                    }
                    else if(date.Contains("giờ"))
                    {
                        return DateTime.Now.AddHours(timeLine);
                    }
                    else if (date.Contains("ngày"))
                    {
                        return DateTime.Now.AddDays(timeLine);
                    }
                }

                return new DateTime();
            }
            catch (Exception)
            {
                return new DateTime();
            }
        }
    }
}
