using CommentTMDT.Helper;
using CommentTMDT.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CommentTMDT.Controller
{
    class ChoLon
    {
        private HttpClient _client = new HttpClient();
        private readonly HtmlAgilityPack.HtmlDocument _document = new HtmlAgilityPack.HtmlDocument();
        private const string _urlHome = "https://dienmaycholon.vn";
        private readonly Label _lbTotalComment;
        private readonly Label _lbError;

        public ChoLon(Label lbTotalComment, Label lbError)
        {
            _lbError = lbError;
            _lbTotalComment = lbTotalComment;
        }

        public async Task CrawlData()
        {
            List<CommentModel> data = await GetCommentPage("https://dienmaycholon.vn/tivi/smart-tivi-samsung-qled-4k-43-inch-qa43q60b");
            if (data != null)
            {
                string jsonObj = System.Text.Json.JsonSerializer.Serialize<List<CommentModel>>(data, Util.opt);
                if (Util.InsertPost(jsonObj) == 1)
                {
                }
            }
        }

        private async ValueTask<List<CommentModel>> GetCommentPage(string urlProduct)
        {
            List<CommentModel> lstCommentDetail = new List<CommentModel>();

            string html = await HttpClient_Helper.GetData(_client, urlProduct);
            _document.LoadHtml(html);
            string idProduct = _document.DocumentNode.SelectSingleNode("//input[@id='id_product']")?.Attributes["value"]?.Value;

            if (!String.IsNullOrEmpty(idProduct))
            {
                byte totalPage = 0;
                byte pageCurrent = 1;
                bool checkEndDataInWeek = false;

                do
                {
                    string linkApi = @"https://dienmaycholon.vn/api/general/dfquestionproduct/list?cid_product=" + idProduct + $"&page={pageCurrent}";
                    html = await HttpClient_Helper.GetData(_client, linkApi);

                    ChoLonModel.Root jsonObj = JsonConvert.DeserializeObject<ChoLonModel.Root>(html);

                    List<ChoLonModel.Datum> lstDataComment = jsonObj.data.data;
                    if (lstDataComment != null && lstDataComment.Count > 0)
                    {
                        foreach (ChoLonModel.Datum item in lstDataComment)
                        {
                            /* Only get data in week */
                            if(Util.DatesAreInTheSameWeek( GetDate(item.showTime), DateTime.Now) )
                            {
                                #region comment
                                CommentModel cmt = new CommentModel();

                                cmt.ProductId = idProduct;
                                cmt.Domain = _urlHome;
                                cmt.UrlProduct = urlProduct;
                                cmt.Id = Util.ConvertStringtoMD5(cmt.UrlProduct + item.id);

                                cmt.Comment = item.content;
                                cmt.UserComment = item.name;

                                cmt.PostDate = DateTime.Now;
                                cmt.PostDateTimeStamp = Util.ConvertDateTimeToTimeStamp(cmt.PostDate);

                                cmt.CommentDate = GetDate(item.showTime);
                                cmt.CommentDateTimeStamp = Util.ConvertDateTimeToTimeStamp((DateTime)cmt.CommentDate);

                                lstCommentDetail.Add(cmt);
                                #endregion

                                #region reply
                                if(item.child != null && item.child.Any())
                                {
                                    List<ChoLonModel.Child> listReply = item.child;
                                    foreach (ChoLonModel.Child item1 in listReply)
                                    {
                                        if (Util.DatesAreInTheSameWeek(GetDate(item1.showTime), DateTime.Now))
                                        {
                                            CommentModel reply = new CommentModel();

                                            reply.ProductId = idProduct;
                                            reply.Domain = _urlHome;
                                            reply.UrlProduct = urlProduct;
                                            reply.Id = Util.ConvertStringtoMD5(reply.UrlProduct + item1.id_child);

                                            reply.Comment = item1.content;
                                            reply.UserComment = item1.name;

                                            reply.PostDate = DateTime.Now;
                                            reply.PostDateTimeStamp = Util.ConvertDateTimeToTimeStamp(reply.PostDate);

                                            reply.CommentDate = GetDate(item1.showTime);
                                            reply.CommentDateTimeStamp = Util.ConvertDateTimeToTimeStamp(reply.CommentDate);

                                            lstCommentDetail.Add(reply);
                                        }
                                        else
                                        {
                                            checkEndDataInWeek = true;
                                            break;
                                        }
                                    }
                                }
                                #endregion
                            }
                            else
                            {
                                checkEndDataInWeek = true;
                                break;
                            }
                        }
                    }

                    if(checkEndDataInWeek)
                    {
                        break;
                    }

                    totalPage = (byte)jsonObj.data.totalpage;
                } while (pageCurrent++ <= totalPage);
            }

            return lstCommentDetail;
        }

        private DateTime GetDate(string date)
        {
            DateTime data = DateTime.Now;

            if (date.Contains("giờ"))
            {
                sbyte timeLine = (sbyte)(sbyte.Parse(Regex.Match(date, @"(\d+)(?=.giờ)").Value) * (-1));
                data = data.AddHours(timeLine);
            }

            if (date.Contains("ngày"))
            {
                sbyte timeLine = (sbyte)(sbyte.Parse(Regex.Match(date, @"(\d+)(?=.ngày)").Value) * (-1));
                data = data.AddDays(timeLine);
            }

            if (date.Contains("tháng"))
            {
                sbyte timeLine = (sbyte)(sbyte.Parse(Regex.Match(date, @"(\d+)(?=.tháng)").Value) * (-1));
                data = data.AddMonths(timeLine);
            }

            if (date.Contains("năm"))
            {
                sbyte timeLine = (sbyte)(sbyte.Parse(Regex.Match(date, @"(\d+)(?=.năm)").Value) * (-1));
                data = data.AddYears(timeLine);
            }

            return data;
        }
    }
}
