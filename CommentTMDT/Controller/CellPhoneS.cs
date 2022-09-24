using CefSharp.WinForms;
using CommentTMDT.Helper;
using CommentTMDT.Model;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CommentTMDT.Controller
{
    class CellPhoneS
    {
        private ChromiumWebBrowser _browser = null;
        private readonly HtmlAgilityPack.HtmlDocument _document = new HtmlAgilityPack.HtmlDocument();
        private const string _urlHome = @"https://cellphones.com.vn/";
        private const string jsClickShowMoreReview = @"document.getElementById('cmt_loadmore').click()";
        private readonly Label _lbTotalComment, _lbError;

        public CellPhoneS(ChromiumWebBrowser browser, Label lbTotalComment, Label lbError)
        {
            _browser = browser;
            _lbTotalComment = lbTotalComment;
            _lbError = lbError;
        }

        public async Task CrawlData()
        {
            List<CommentModel> data = await GetCommentProduct("https://cellphones.com.vn/samsung-galaxy-s22-ultra.html?itm=hotsale");
            string jsonObj = System.Text.Json.JsonSerializer.Serialize<List<CommentModel>>(data, Util.opt);

            //if (Util.InsertPost(jsonObj) == 1)
            //{
            //    _lb1.Invoke((MethodInvoker)(() => _lb1.Text = Util.ConvertNumberToTypeMoney(totalComment1)));
            //}
        }

        private async Task<List<CommentModel>> GetCommentProduct(string url)
        {
            CommentModel obj = new CommentModel();
            ushort indexLastComment = 0;
            bool[] checkEndDataInWeek = { false, false };
            List<CommentModel> listCommentJson = new List<CommentModel>();

            try
            {
                await _browser.LoadUrlAsync(url);
                await Task.Delay(15_000);

                while (true)
                {
                    string html = await Util.GetBrowserSource(_browser).ConfigureAwait(false);
                    _document.LoadHtml(html);
                    html = null;

                    string productId = _document.DocumentNode.SelectSingleNode("//div[@id='page-review-product']")?.Attributes["data-productid"]?.Value;

                    HtmlNodeCollection divComment = _document.DocumentNode.SelectNodes($"//div[@class='list-comment']/div[@class='item-comment'][position()>{indexLastComment}]");
                    if (divComment != null)
                    {
                        foreach (HtmlNode item in divComment)
                        {
                            #region Custommer's comment
                            CommentModel custommer = new CommentModel();
                            custommer.UserComment = item.SelectSingleNode("./div[@class='item-comment__box-cmt']//div[@class='box-info']/p")?.InnerText;
                            custommer.Comment = Util.RemoveSpecialCharacter(item.SelectSingleNode(".//div[@class='box-cmt__box-question']/p")?.InnerText);

                            if (!String.IsNullOrEmpty(custommer.UserComment) && !String.IsNullOrEmpty(custommer.Comment))
                            {
                                custommer.UrlProduct = url;
                                custommer.Domain = _urlHome;
                                custommer.ProductId = productId;

                                custommer.PostDate = DateTime.Now;
                                custommer.PostDateTimeStamp = Util.ConvertDateTimeToTimeStamp(custommer.PostDate);

                                string dateComment = item.SelectSingleNode("./div[@class='item-comment__box-cmt']//p[@class='box-time-cmt']")?.InnerText;
                                custommer.CommentDate = GetDate(dateComment);
                                if(!Util.DatesAreInTheSameWeek(custommer.CommentDate, DateTime.Now))
                                {
                                    checkEndDataInWeek[0] = true;
                                    goto DataAdmin;
                                }

                                if (custommer.CommentDate != null)
                                {
                                    custommer.CommentDateTimeStamp = Util.ConvertDateTimeToTimeStamp(custommer.CommentDate);
                                }

                                custommer.Id = Util.ConvertStringtoMD5($"{url}{custommer.CommentDateTimeStamp}-1");
                                custommer.IdComment = 0;

                                listCommentJson.Add(custommer);
                            }

                            #endregion

                            #region Admin's reply
                            DataAdmin:
                            {
                                CommentModel admin = new CommentModel();
                                admin.UserComment = item.SelectSingleNode(".//div[@class='item-comment__box-rep-comment']//div[@class='box-info']")?.InnerText;
                                admin.Comment = Util.RemoveSpecialCharacter(item.SelectSingleNode(".//div[@class='item-comment__box-rep-comment']//div[@class='box-cmt__box-question']")?.InnerText);

                                if (!String.IsNullOrEmpty(admin.UserComment) && !String.IsNullOrEmpty(admin.Comment))
                                {
                                    admin.UrlProduct = url;
                                    admin.Domain = _urlHome;
                                    admin.ProductId = productId;

                                    admin.PostDate = DateTime.Now;
                                    admin.PostDateTimeStamp = Util.ConvertDateTimeToTimeStamp(admin.PostDate);

                                    string dateComment = item.SelectSingleNode(".//div[@class='item-comment__box-rep-comment']//p[@class='box-time-cmt']")?.InnerText;
                                    admin.CommentDate = GetDate(dateComment);

                                    if (!Util.DatesAreInTheSameWeek(admin.CommentDate, DateTime.Now))
                                    {
                                        checkEndDataInWeek[1] = true;
                                        continue;
                                    }

                                    if (admin.CommentDate != null)
                                    {
                                        admin.CommentDateTimeStamp = Util.ConvertDateTimeToTimeStamp(admin.CommentDate);
                                    }

                                    admin.Id = Util.ConvertStringtoMD5($"{url}{admin.CommentDateTimeStamp}-1");
                                    admin.IdComment = 0;

                                    listCommentJson.Add(admin);
                                }
                            }
                            #endregion
                        }
                    }

                    /* Only get data in week */
                    if(checkEndDataInWeek[0] && checkEndDataInWeek[1])
                    {
                        break;
                    }

                    HtmlNode checkExitButtonLoadMore = _document.DocumentNode.SelectSingleNode("//a[@id='cmt_loadmore' and not(@style='display: none;')]");
                    if (checkExitButtonLoadMore == null)
                    {
                        break;
                    }

                    /* Check end page(if end page => data return = null because not found div button show more) and check error js */
                    string checkJs = await Util.EvaluateJavaScriptSync(jsClickShowMoreReview, _browser).ConfigureAwait(false);
                    if (checkJs == null)
                    {
                        break;
                    }

                    await Task.Delay(10_000);
                }

                return listCommentJson;
            }
            catch (Exception ex)
            {
                return listCommentJson;
            }
        }

        private DateTime GetDate(string strDate, string pattern = @"\d{2}\/\d{2}\/\d{4}", string formatTypeDate = @"dd/MM/yyyy")
        {
            try
            {
                if (strDate.Contains("giờ") || strDate.Contains("phút") || strDate.Contains("tiếng"))
                {
                    return DateTime.Now;
                }
                else if (strDate.Contains("Hôm qua"))
                {
                    return DateTime.Today.AddDays(-1);
                }
                else if (strDate.Contains("ngày"))
                {
                    ushort numberDay = (ushort)Util.convertTextToNumber(strDate);
                    return DateTime.Today.AddDays(-numberDay);
                }
                else if (strDate.Contains("tuần"))
                {
                    ushort numberDay = (ushort)(Util.convertTextToNumber(strDate) * 7);
                    return DateTime.Today.AddDays(-numberDay);
                }
                else if (strDate.Contains("tháng"))
                {
                    ushort numberDay = (ushort)(Util.convertTextToNumber(strDate) * 30);
                    return DateTime.Today.AddDays(-numberDay);
                }
                else if (strDate.Contains("năm"))
                {
                    ushort numberDay = (ushort)(Util.convertTextToNumber(strDate) * 365);
                    return DateTime.Today.AddDays(-numberDay);
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
